using System.Text.Json;
using MediatR;
using PiKoRe.Core.Abstractions;
using PiKoRe.Core.Constants;
using PiKoRe.Core.Events;
using PiKoRe.Core.Models;
using Serilog;

namespace PiKoRe.Core.Pipeline;

public sealed class DagEngine : INotificationHandler<JobCompletedEvent>
{
    private readonly IReadOnlyList<IInProcessPlugin> _plugins;
    private readonly IJobQueue _jobQueue;
    private readonly ILogger _logger;

    public DagEngine(IEnumerable<IInProcessPlugin> plugins, IJobQueue jobQueue, ILogger logger)
    {
        _plugins  = plugins.ToList();
        _jobQueue = jobQueue;
        _logger   = logger.ForContext<DagEngine>();
    }

    public async Task Handle(JobCompletedEvent notification, CancellationToken ct)
    {
        var dagJson = await _jobQueue.GetPipelineConfigDagJsonAsync(ct);
        if (dagJson is null) return;

        List<string> enabledCapabilities;
        try
        {
            enabledCapabilities = JsonSerializer.Deserialize<List<string>>(dagJson) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.Warning(ex, "Failed to deserialize pipeline_config dag_json");
            return;
        }

        var fileId    = notification.Result.FileId;
        var mediaType = notification.Result.MediaType ?? MediaTypes.All;

        // Include the just-completed capability in the set: the DB job row is still
        // 'running' at this point (PipelineWorker calls MarkCompletedAsync after the
        // runner returns, which is after this handler runs inline inside Publish).
        var completedCaps = new HashSet<string>(
            await _jobQueue.GetCompletedCapabilitiesForFileAsync(fileId, ct));
        completedCaps.Add(notification.Result.Capability);

        var unblocked = new List<string>();

        foreach (var capability in enabledCapabilities)
        {
            if (completedCaps.Contains(capability)) continue;

            var plugin = _plugins.FirstOrDefault(p =>
                p.CapabilitiesProduced.Contains(capability) &&
                MediaTypes.IsSupported(mediaType, p.SupportedMediaTypes));

            if (plugin is null) continue;

            if (!plugin.RequiredCapabilities.All(rc => completedCaps.Contains(rc))) continue;

            if (await _jobQueue.JobExistsForFileAndCapabilityAsync(fileId, capability, ct)) continue;

            var job = new Job(
                Guid.NewGuid(), fileId, capability, JobStatus.Queued,
                null, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                null, null, mediaType);

            await _jobQueue.EnqueueAsync(job, ct);
            unblocked.Add(capability);
        }

        _logger.ForContext("file_id", fileId)
               .ForContext("completed_capability", notification.Result.Capability)
               .ForContext("unblocked_capabilities", string.Join(",", unblocked))
               .Debug("DagEngine processed completed job, unblocked {Count} capability(ies)", unblocked.Count);
    }
}
