using MediatR;
using Microsoft.Extensions.Configuration;
using PiKoRe.Core.Abstractions;
using PiKoRe.Core.Constants;
using PiKoRe.Core.Events;
using PiKoRe.Core.Models;
using Polly;
using Polly.Retry;
using Serilog;

namespace PiKoRe.Core.Pipeline;

public sealed class LocalSequentialRunner : IJobRunner
{
    private readonly IReadOnlyList<IInProcessPlugin> _plugins;
    private readonly IPublisher _publisher;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _cpuSlots;
    private readonly SemaphoreSlim _gpuSlot = new(1, 1);
    private readonly ResiliencePipeline<AnalysisResult> _retryPipeline;

    public LocalSequentialRunner(
        IEnumerable<IInProcessPlugin> plugins,
        IPublisher publisher,
        ILogger logger,
        IConfiguration config)
    {
        _plugins   = plugins.ToList();
        _publisher = publisher;
        _logger    = logger.ForContext<LocalSequentialRunner>();

        var slots = int.TryParse(config["Pipeline:MaxCpuSlots"], out var n) ? n : 4;
        _cpuSlots = new SemaphoreSlim(slots, slots);

        _retryPipeline = new ResiliencePipelineBuilder<AnalysisResult>()
            .AddRetry(new RetryStrategyOptions<AnalysisResult>
            {
                MaxRetryAttempts = 3,
                BackoffType      = DelayBackoffType.Exponential,
                Delay            = TimeSpan.FromMilliseconds(500),
                UseJitter        = true,
            })
            .Build();
    }

    public async Task<JobResult> RunAsync(Job job, CancellationToken ct)
    {
        var log = _logger
            .ForContext("job_id", job.Id)
            .ForContext("capability", job.Capability)
            .ForContext("media_type", job.MediaType);

        var plugin = _plugins.FirstOrDefault(p =>
            p.CapabilitiesProduced.Contains(job.Capability) &&
            MediaTypes.IsSupported(job.MediaType ?? "unknown", p.SupportedMediaTypes));

        if (plugin is null)
        {
            var error = $"No plugin found for capability '{job.Capability}' and media type '{job.MediaType}'";
            log.Warning(error);
            var failed = new JobResult(job.Id, job.FileId, job.Capability, job.MediaType, false, error, null);
            await _publisher.Publish(new JobFailedEvent(job.Id, error), ct);
            return failed;
        }

        await _cpuSlots.WaitAsync(ct);
        try
        {
            log.Information("Running plugin {PluginName}", plugin.Name);

            var request = new AnalysisRequest(job.Id, job.FileId, job.FilePath!, null);

            var analysisResult = await _retryPipeline.ExecuteAsync(
                async token => await plugin.AnalyzeAsync(request, token), ct);

            var result = new JobResult(job.Id, job.FileId, job.Capability, job.MediaType, true, null, analysisResult);
            await _publisher.Publish(new JobCompletedEvent(result), ct);
            log.Information("Job completed");
            return result;
        }
        catch (Exception ex)
        {
            log.Warning(ex, "Job failed after retries");
            var result = new JobResult(job.Id, job.FileId, job.Capability, job.MediaType, false, ex.Message, null);
            await _publisher.Publish(new JobFailedEvent(job.Id, ex.Message), ct);
            return result;
        }
        finally
        {
            _cpuSlots.Release();
        }
    }
}
