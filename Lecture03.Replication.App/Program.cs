using Lecture03.Replication.App.BackgroundServices;
using Lecture03.Replication.App.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")!;

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<LoadGeneratorOptions>(builder.Configuration.GetSection("LoadGenerator"));

builder.Services.AddDbContextFactory<CatsDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHostedService<LoadGenerator>();

var host = builder.Build();

// Postgres only ever creates the "postgres" administrative database on
// its own - our "cats" database has to be created explicitly before EF
// Core can migrate it.
await EnsureDatabaseExistsAsync(connectionString);

await using (var context = await host.Services.GetRequiredService<IDbContextFactory<CatsDbContext>>()
    .CreateDbContextAsync())
{
    await context.Database.MigrateAsync();
}

host.Run();

static async Task EnsureDatabaseExistsAsync(string connectionString)
{
    var targetBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    var databaseName = targetBuilder.Database!;
    targetBuilder.Database = "postgres";

    await using var adminConnection = new NpgsqlConnection(targetBuilder.ConnectionString);
    await adminConnection.OpenAsync();

    await using var checkCommand = adminConnection.CreateCommand();
    checkCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = $1";
    checkCommand.Parameters.Add(new NpgsqlParameter { Value = databaseName });
    var exists = await checkCommand.ExecuteScalarAsync() is not null;

    if (!exists)
    {
        await using var createCommand = adminConnection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await createCommand.ExecuteNonQueryAsync();
    }
}
