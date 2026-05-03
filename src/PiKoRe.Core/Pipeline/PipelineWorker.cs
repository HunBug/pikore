using Microsoft.Extensions.Hosting;
using PiKoRe.Core.Abstractions;
using PiKoRe.Core.Models;
using Serilog;

namespace PiKoRe.Core.Pipeline;

public sealed class PipelineWorker : BackgroundService
{
    private readonly IJobQueue _jobQueue;
    private readonly IJobRunner _runner;
    private readonly ILogger _logger;

    public PipelineWorker(IJobQueue jobQueue, IJobRunner runner, ILogger logger)
    {
        _jobQueue = jobQueue;
        _runner   = runner;
        _logger   = logger.ForContext<PipelineWorker>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("PipelineWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            Job? job;
            try
            {
                job = await _jobQueue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (job is null)
            {
                await Task.Delay(100, stoppingToken).ConfigureAwait(false);
                continue;
            }

            var log = _logger
                .ForContext("job_id", job.Id)
                .ForContext("capability", job.Capability);

            log.Information("Dispatching job");

            var result = await _runner.RunAsync(job, stoppingToken);

            if (result.Success)
            {
                await _jobQueue.MarkCompletedAsync(job.Id, stoppingToken);
            }
            else
            {
                await _jobQueue.MarkFailedAsync(job.Id, result.Error ?? "Unknown error", stoppingToken);
            }
        }

        _logger.Information("PipelineWorker stopped");
    }
}
