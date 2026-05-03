using PiKoRe.Core.Models;

namespace PiKoRe.Core.Abstractions;

/// <summary>Persistent job queue backed by SQLite.</summary>
public interface IJobQueue
{
    Task EnqueueAsync(Job job, CancellationToken ct);
    Task<Job?> DequeueAsync(CancellationToken ct);
    Task MarkCompletedAsync(Guid jobId, CancellationToken ct);
    Task MarkFailedAsync(Guid jobId, string error, CancellationToken ct);
    Task<IReadOnlyList<string>> GetCompletedCapabilitiesForFileAsync(Guid fileId, CancellationToken ct);
    Task<bool> JobExistsForFileAndCapabilityAsync(Guid fileId, string capability, CancellationToken ct);
    Task<string?> GetPipelineConfigDagJsonAsync(CancellationToken ct);
}
