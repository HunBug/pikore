using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace PiKoRe.Data.Tests;

public sealed class DatabaseMigratorTests : IAsyncLifetime
{
    // pgvector image required — the standard postgres image does not include the vector extension.
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .Build();

    public Task InitializeAsync() => _pg.StartAsync();
    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();

    [Fact]
    public async Task MigratePostgres_AppliesInitialSchema_WithoutError()
    {
        DatabaseMigrator.MigratePostgres(_pg.GetConnectionString(), NullLogger.Instance);

        await using var conn = new NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT to_regclass('public.embeddings')::TEXT";
        var result = await cmd.ExecuteScalarAsync();

        Assert.Equal("embeddings", result);
    }

    [Fact]
    public async Task MigratePostgres_IsIdempotent()
    {
        DatabaseMigrator.MigratePostgres(_pg.GetConnectionString(), NullLogger.Instance);
        // Running twice must not throw — DbUp tracks applied scripts.
        DatabaseMigrator.MigratePostgres(_pg.GetConnectionString(), NullLogger.Instance);

        await Task.CompletedTask;
    }
}
