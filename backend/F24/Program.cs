using F24.Repositories;
using F24.Services;
using Microsoft.EntityFrameworkCore;

LoadEnvironmentFile(Path.Combine(AppContext.BaseDirectory, ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<FileSystemService>();
builder.Services.AddScoped<IFileSystemRepository, FileSystemRepository>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(BuildConnectionString(builder.Configuration)));

var app = builder.Build();
app.MapControllers();
app.Run();

static string BuildConnectionString(IConfiguration configuration) =>
    $"Host=localhost;Port={configuration["POSTGRES_PORT"]};Database={configuration["POSTGRES_DB"]};Username={configuration["POSTGRES_USER"]};Password={configuration["POSTGRES_PASSWORD"]}";

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

public partial class Program { }
