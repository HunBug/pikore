namespace PiKoRe.Core.Models;

public sealed record JobResult(
    Guid JobId,
    bool Success,
    string? Error,
    AnalysisResult? Result
);
