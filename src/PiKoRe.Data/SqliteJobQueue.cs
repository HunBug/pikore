using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using PiKoRe.Core.Abstractions;
using PiKoRe.Core.Models;
using Serilog;

namespace PiKoRe.Data;

public sealed class SqliteJobQueue : IJobQueue
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    public SqliteJobQueue(IConfiguration config, ILogger logger)
    {
        _connectionString = config["ConnectionStrings:SQLite"]
            ?? throw new InvalidOperationException("ConnectionStrings:SQLite not configured");
        _logger = logger.ForContext<SqliteJobQueue>();
    }

    public async Task EnqueueAsync(Job job, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await conn.ExecuteAsync("""
            INSERT INTO job_queue (id, file_id, capability, status, plugin_id, priority, created_at, updated_at)
            VALUES (@Id, @FileId, @Capability, 'queued', @PluginId, @Priority, @CreatedAt, @UpdatedAt)
            """,
            new
            {
                Id        = job.Id.ToString(),
                FileId    = job.FileId.ToString(),
                job.Capability,
                PluginId  = job.PluginId?.ToString(),
                job.Priority,
                CreatedAt = job.Created.ToString("O"),
                UpdatedAt = job.Updated.ToString("O"),
            });

        _logger.ForContext("job_id", job.Id)
               .ForContext("capability", job.Capability)
               .Debug("Job enqueued");
    }

    public async Task<Job?> DequeueAsync(CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);

        // BEGIN IMMEDIATE prevents two workers from dequeuing the same row.
        await conn.ExecuteAsync("BEGIN IMMEDIATE");
        try
        {
            var row = await conn.QuerySingleOrDefaultAsync<JobRow>("""
                SELECT j.id         AS Id,
                       j.file_id    AS FileId,
                       j.capability AS Capability,
                       j.plugin_id  AS PluginId,
                       j.priority   AS Priority,
                       j.created_at AS CreatedAt,
                       j.updated_at AS UpdatedAt,
                       j.error      AS Error,
                       fi.path      AS FilePath,
                       fi.media_type AS MediaType
                FROM job_queue j
                JOIN file_index fi ON fi.id = j.file_id
                WHERE j.status = 'queued'
                ORDER BY j.priority DESC, j.created_at ASC
                LIMIT 1
                """);

            if (row is null)
            {
                await conn.ExecuteAsync("ROLLBACK");
                return null;
            }

            var now = DateTimeOffset.UtcNow.ToString("O");
            await conn.ExecuteAsync(
                "UPDATE job_queue SET status = 'running', updated_at = @Now WHERE id = @Id",
                new { Now = now, Id = row.Id });

            await conn.ExecuteAsync("COMMIT");

            return new Job(
                Guid.Parse(row.Id),
                Guid.Parse(row.FileId),
                row.Capability,
                JobStatus.Running,
                row.PluginId is null ? null : Guid.Parse(row.PluginId),
                row.Priority,
                DateTimeOffset.Parse(row.CreatedAt),
                DateTimeOffset.Parse(now),
                row.Error,
                row.FilePath,
                row.MediaType);
        }
        catch
        {
            await conn.ExecuteAsync("ROLLBACK");
            throw;
        }
    }

    public async Task MarkCompletedAsync(Guid jobId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE job_queue SET status = 'completed', updated_at = @Now WHERE id = @Id",
            new { Now = DateTimeOffset.UtcNow.ToString("O"), Id = jobId.ToString() });

        _logger.ForContext("job_id", jobId).Debug("Job marked completed");
    }

    public async Task MarkFailedAsync(Guid jobId, string error, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE job_queue SET status = 'failed', updated_at = @Now, error = @Error WHERE id = @Id",
            new { Now = DateTimeOffset.UtcNow.ToString("O"), Error = error, Id = jobId.ToString() });

        _logger.ForContext("job_id", jobId).Warning("Job marked failed: {Error}", error);
    }

    public async Task<IReadOnlyList<string>> GetCompletedCapabilitiesForFileAsync(Guid fileId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        var results = await conn.QueryAsync<string>(
            "SELECT capability FROM job_queue WHERE file_id = @FileId AND status = 'completed'",
            new { FileId = fileId.ToString() });
        return results.AsList();
    }

    public async Task<bool> JobExistsForFileAndCapabilityAsync(Guid fileId, string capability, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM job_queue
            WHERE file_id = @FileId AND capability = @Capability AND status NOT IN ('failed')
            """,
            new { FileId = fileId.ToString(), Capability = capability });
        return count > 0;
    }

    public async Task<string?> GetPipelineConfigDagJsonAsync(CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT dag_json FROM pipeline_config ORDER BY updated_at DESC LIMIT 1");
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync("PRAGMA journal_mode=WAL;");
        return conn;
    }

    private sealed record JobRow(
        string Id,
        string FileId,
        string Capability,
        string? PluginId,
        int Priority,
        string CreatedAt,
        string UpdatedAt,
        string? Error,
        string FilePath,
        string MediaType);
}
