using F24.Models.Entities;
using F24.Models.Enums;
using F24.Models.Requests;
using F24.Repositories;
using F24.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using File = F24.Models.Entities.File;

namespace F24.IntegrationTests;

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgresFixture>
{
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_POSTGRES_INTEGRATION_TESTS"), "true",
                StringComparison.OrdinalIgnoreCase))
            Skip = "Set RUN_POSTGRES_INTEGRATION_TESTS=true and configure a disposable PostgreSQL database.";
    }
}

public sealed class PostgresFixture
{
    public bool Enabled => string.Equals(
        Environment.GetEnvironmentVariable("RUN_POSTGRES_INTEGRATION_TESTS"), "true",
        StringComparison.OrdinalIgnoreCase);

    public string ConnectionString => new NpgsqlConnectionStringBuilder
    {
        Host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost",
        Port = int.Parse(Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432"),
        Database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "f24",
        Username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "f24",
        Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "f24-test-password",
        Pooling = false
    }.ConnectionString;

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async Task ResetAsync()
    {
        if (!Enabled) return;

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using (var reset = new NpgsqlCommand("DROP SCHEMA public CASCADE; CREATE SCHEMA public;", connection))
            await reset.ExecuteNonQueryAsync();

        var schema = await System.IO.File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
        await using var initialize = new NpgsqlCommand(schema, connection);
        await initialize.ExecuteNonQueryAsync();
    }
}

[Collection("PostgreSQL")]
public sealed class PostgresFileSystemTests(PostgresFixture fixture)
{
    private static readonly Guid RootId = Guid.Empty;

    [PostgresFact]
    public async Task Concurrent_file_and_folder_creation_preserves_one_sibling_namespace()
    {
        await fixture.ResetAsync();

        static async Task<Exception?> CreateAsync(PostgresFixture fixture, EntryType type)
        {
            await using var context = fixture.CreateContext();
            var service = new FileSystemService(new FileSystemRepository(context));
            try
            {
                await service.CreateAsync(RootId,
                    new CreateEntryRequest { Name = "SharedName", Type = type }, CancellationToken.None);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        var results = await Task.WhenAll(
            CreateAsync(fixture, EntryType.File),
            CreateAsync(fixture, EntryType.Folder));

        Assert.Single(results, result => result is null);
        Assert.Single(results, result => result is DbUpdateException);

        await using var verification = fixture.CreateContext();
        var count = await verification.Files.CountAsync(file => file.ParentId == RootId && file.Name == "SharedName")
                    + await verification.Folders.CountAsync(folder => folder.ParentId == RootId && folder.Name == "SharedName");
        Assert.Equal(1, count);
    }

    [PostgresFact]
    public async Task Deleting_a_folder_cascades_through_its_complete_subtree()
    {
        await fixture.ResetAsync();

        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        await using (var setup = fixture.CreateContext())
        {
            setup.Folders.AddRange(
                new Folder { Id = parentId, ParentId = RootId, Name = "parent" },
                new Folder { Id = childId, ParentId = parentId, Name = "child" });
            setup.Files.Add(new File { Id = Guid.NewGuid(), ParentId = childId, Name = "nested.txt" });
            await setup.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            var service = new FileSystemService(new FileSystemRepository(context));
            await service.DeleteFolderAsync(parentId, CancellationToken.None);
        }

        await using var verification = fixture.CreateContext();
        Assert.False(await verification.Folders.AnyAsync(folder => folder.Id == parentId || folder.Id == childId));
        Assert.False(await verification.Files.AnyAsync(file => file.ParentId == childId));
    }

    [PostgresFact]
    public async Task Search_orders_prefix_results_and_supports_literal_wildcards_and_exact_current_folder()
    {
        await fixture.ResetAsync();

        var documentsId = Guid.NewGuid();
        var nestedId = Guid.NewGuid();
        await using (var setup = fixture.CreateContext())
        {
            setup.Folders.AddRange(
                new Folder { Id = documentsId, ParentId = RootId, Name = "documents" },
                new Folder { Id = nestedId, ParentId = documentsId, Name = "nested" });
            setup.Files.AddRange(
                new File { Id = Guid.NewGuid(), ParentId = documentsId, Name = "Alpha.txt" },
                new File { Id = Guid.NewGuid(), ParentId = nestedId, Name = "alphabet.txt" },
                new File { Id = Guid.NewGuid(), ParentId = documentsId, Name = "100%ready.txt" },
                new File { Id = Guid.NewGuid(), ParentId = nestedId, Name = "Alpha.txt" });
            await setup.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var service = new FileSystemService(new FileSystemRepository(context));

        var ordered = await service.SearchAsync("alpha", SearchMode.PrefixAll, null, 10, CancellationToken.None);
        Assert.Equal(["Alpha.txt", "Alpha.txt", "alphabet.txt"], ordered.Select(result => result.Name));

        var wildcard = await service.SearchAsync("100%", SearchMode.PrefixAll, null, 10, CancellationToken.None);
        Assert.Single(wildcard);
        Assert.Equal("100%ready.txt", wildcard[0].Name);

        var exact = await service.SearchAsync("alpha.txt", SearchMode.ExactCurrent, documentsId, 10,
            CancellationToken.None);
        Assert.Single(exact);
        Assert.Equal(documentsId, exact[0].ParentId);
        Assert.Equal("home/documents/Alpha.txt", exact[0].Path);
    }

    [PostgresFact]
    public async Task Rolled_back_mutation_is_not_persisted()
    {
        await fixture.ResetAsync();

        var id = Guid.NewGuid();
        await using (var context = fixture.CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.Files.Add(new File { Id = id, ParentId = RootId, Name = "rollback.txt" });
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verification = fixture.CreateContext();
        Assert.False(await verification.Files.AnyAsync(file => file.Id == id));
    }

    [PostgresFact]
    public async Task Derived_folder_path_is_bounded_without_persisting_denormalized_path_data()
    {
        await fixture.ResetAsync();

        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var longName = new string('a', 250);
        await using (var setup = fixture.CreateContext())
        {
            setup.Folders.AddRange(
                new Folder { Id = parentId, ParentId = RootId, Name = longName },
                new Folder { Id = childId, ParentId = parentId, Name = "final-part" });
            await setup.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var service = new FileSystemService(new FileSystemRepository(context));
        var folder = await service.GetFolderAsync(childId, CancellationToken.None);

        Assert.Equal(255, folder.Path.Length);
        Assert.StartsWith("...", folder.Path);
        Assert.EndsWith("/final-part", folder.Path);
    }
}
