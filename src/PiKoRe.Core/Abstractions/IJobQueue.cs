using PiKoRe.Core.Models;

namespace PiKoRe.Core.Abstractions;

/// <summary>Persistent job queue backed by SQLite.</summary>
public interface IJobQueue
{
    Task EnqueueAsync(Job job, CancellationToken ct);
    Task<Job?> DequeueAsync(CancellationToken ct);
    Task MarkCompletedAsync(Guid jobId, CancellationToken ct);
    Task MarkFailedAsync(Guid jobId, string error, CancellationToken ct);
}
