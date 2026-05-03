using MediatR;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PiKoRe.Core.Abstractions;
using PiKoRe.Core.Constants;
using PiKoRe.Core.Events;
using PiKoRe.Core.Models;
using PiKoRe.Core.Pipeline;
using Serilog;

namespace PiKoRe.Core.Tests.Pipeline;

public sealed class LocalSequentialRunnerTests
{
    private static LocalSequentialRunner BuildRunner(
        IEnumerable<IInProcessPlugin> plugins,
        IPublisher publisher,
        string? maxCpuSlots = "1")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(maxCpuSlots is null
                ? []
                : new Dictionary<string, string?> { ["Pipeline:MaxCpuSlots"] = maxCpuSlots })
            .Build();

        var logger = new LoggerConfiguration().CreateLogger();
        return new LocalSequentialRunner(plugins, publisher, logger, config);
    }

    private static Job MakeJob(string capability = Capabilities.Exif, string mediaType = MediaTypes.Image)
        => new(Guid.NewGuid(), Guid.NewGuid(), capability, JobStatus.Queued,
               null, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
               null, "/test/photo.jpg", mediaType);

    [Fact]
    public async Task RunAsync_PluginFound_PublishesJobCompletedEvent()
    {
        var plugin = Substitute.For<IInProcessPlugin>();
        plugin.Name.Returns("test-plugin");
        plugin.Version.Returns("1.0");
        plugin.CapabilitiesProduced.Returns([Capabilities.Exif]);
        plugin.RequiredCapabilities.Returns([]);
        plugin.SupportedMediaTypes.Returns([MediaTypes.Image]);
        plugin.AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
              .Returns(new AnalysisResult());

        var publisher = Substitute.For<IPublisher>();
        var runner    = BuildRunner([plugin], publisher);
        var job       = MakeJob();

        var result = await runner.RunAsync(job, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(job.Id, result.JobId);
        Assert.Equal(job.FileId, result.FileId);
        Assert.Equal(job.Capability, result.Capability);
        Assert.Equal(job.MediaType, result.MediaType);
        await publisher.Received(1)
            .Publish(Arg.Any<JobCompletedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_NoPlugin_PublishesJobFailedEvent()
    {
        var publisher = Substitute.For<IPublisher>();
        var runner    = BuildRunner([], publisher);
        var job       = MakeJob();

        var result = await runner.RunAsync(job, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        await publisher.Received(1)
            .Publish(Arg.Any<JobFailedEvent>(), Arg.Any<CancellationToken>());
    }
}
