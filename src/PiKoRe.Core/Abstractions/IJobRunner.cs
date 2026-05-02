using PiKoRe.Core.Models;

namespace PiKoRe.Core.Abstractions;

/// <summary>Executes a single job against the appropriate plugin. Default implementation: LocalSequentialRunner.</summary>
public interface IJobRunner
{
    Task<JobResult> RunAsync(Job job, CancellationToken ct);
}
