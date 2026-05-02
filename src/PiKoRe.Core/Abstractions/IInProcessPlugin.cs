using PiKoRe.Core.Models;

namespace PiKoRe.Core.Abstractions;

/// <summary>Plugin running in the same process as core — called directly, no HTTP round-trip.</summary>
public interface IInProcessPlugin : IPlugin
{
    Task<AnalysisResult> AnalyzeAsync(AnalysisRequest request, CancellationToken ct);
}
