using System.Reflection;
using DbUp;
using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;

namespace PiKoRe.Data;

/// <summary>Applies numbered SQL migrations via DbUp. Throws on failure.</summary>
public static class DatabaseMigrator
{
    public static void MigrateSqlite(string connectionString, ILogger logger)
    {
        var upgrader = DeployChanges.To
            .SqliteDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                name => name.Contains(".SQLite."))
            .LogTo(new MicrosoftLogAdapter(logger))
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
            throw new InvalidOperationException("SQLite migration failed.", result.Error);
    }

    public static void MigratePostgres(string connectionString, ILogger logger)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                name => name.Contains(".PostgreSQL."))
            .LogTo(new MicrosoftLogAdapter(logger))
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
            throw new InvalidOperationException("PostgreSQL migration failed.", result.Error);
    }

    private sealed class MicrosoftLogAdapter : IUpgradeLog
    {
        private readonly ILogger _inner;
        internal MicrosoftLogAdapter(ILogger logger) => _inner = logger;

        public void LogTrace(string format, params object[] args) =>
            _inner.LogTrace(format, args);

        public void LogDebug(string format, params object[] args) =>
            _inner.LogDebug(format, args);

        public void LogInformation(string format, params object[] args) =>
            _inner.LogInformation(format, args);

        public void LogWarning(string format, params object[] args) =>
            _inner.LogWarning(format, args);

        public void LogError(string format, params object[] args) =>
            _inner.LogError(format, args);

        public void LogError(Exception ex, string format, params object[] args) =>
            _inner.LogError(ex, format, args);
    }
}
