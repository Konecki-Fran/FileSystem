using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace F24.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class ApiContractTests(PostgresFixture fixture)
{
    private static readonly Guid RootId = Guid.Empty;

    [PostgresFact]
    public async Task Create_and_delete_file_returns_201_and_204()
    {
        await fixture.ResetAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync($"/folders/{RootId}", new { name = "api-file.txt", type = "File" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreatedEntry>();
        Assert.NotNull(created);
        Assert.Equal("api-file.txt", created.Name);
        Assert.Equal("file", created.Type);

        var delete = await client.DeleteAsync($"/files/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [PostgresFact]
    public async Task Concurrent_duplicate_conflict_uses_the_documented_409_error_contract()
    {
        await fixture.ResetAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var requests = new[]
        {
            client.PostAsJsonAsync($"/folders/{RootId}", new { name = "duplicate", type = "File" }),
            client.PostAsJsonAsync($"/folders/{RootId}", new { name = "DUPLICATE", type = "Folder" })
        };
        var responses = await Task.WhenAll(requests);

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        using var body = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
        Assert.Equal("NAME_ALREADY_EXISTS", body.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [PostgresFact]
    public async Task Root_and_missing_resource_errors_use_the_documented_status_codes()
    {
        await fixture.ResetAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var rootDelete = await client.DeleteAsync($"/folders/{RootId}");
        var missingDelete = await client.DeleteAsync($"/files/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Conflict, rootDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
    }

    [PostgresFact]
    public async Task Search_validates_mode_and_exact_search_folder()
    {
        await fixture.ResetAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var invalidMode = await client.GetAsync("/search?prefix=file&mode=999");
        var missingFolder = await client.GetAsync(
            $"/search?prefix=file.txt&mode=ExactCurrent&folder={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.BadRequest, invalidMode.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingFolder.StatusCode);
    }

    private sealed record CreatedEntry(Guid Id, string Name, string Type);

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.ClearProviders()));
}
