using System.Text.Json.Serialization;
using F24.Middleware;
using F24.Repositories;
using F24.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

LoadEnvironmentFile(Path.Combine(AppContext.BaseDirectory, ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The request is invalid." : error.ErrorMessage)
            .FirstOrDefault() ?? "The request is invalid.";
        return new BadRequestObjectResult(new { error = new { code = "INVALID_REQUEST", message } });
    })
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddScoped<FileSystemService>();
builder.Services.AddScoped<IFileSystemRepository, FileSystemRepository>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(BuildConnectionString(builder.Configuration)));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var root = await db.Folders.AsNoTracking().SingleAsync(x => x.ParentId == null, cancellationToken);
    return Results.Redirect($"/folders/{root.Id}");
});
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/db", async (AppDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.Json(new { status = "unhealthy", database = "disconnected" }, statusCode: 503));
app.MapControllers();
app.Run();

static string BuildConnectionString(IConfiguration configuration)
{
    var host = configuration["POSTGRES_HOST"] ?? "localhost";
    var port = configuration["POSTGRES_PORT"] ?? "5432";

    return
        $"Host={host};Port={port};Database={configuration["POSTGRES_DB"]};Username={configuration["POSTGRES_USER"]};Password={configuration["POSTGRES_PASSWORD"]}";
}

static void LoadEnvironmentFile(string path)
{
    if (!File.Exists(path)) return;

    foreach (var line in File.ReadLines(path))
    {
        var value = line.Trim();
        if (value.Length == 0 || value.StartsWith('#')) continue;

        var separator = value.IndexOf('=');
        if (separator <= 0) continue;

        Environment.SetEnvironmentVariable(value[..separator].Trim(), value[(separator + 1)..].Trim().Trim('"'));
    }
}

public partial class Program
{
}
