namespace PiKoRe.Core.Models;

public sealed record JobResult(
    Guid JobId,
    Guid FileId,
    string Capability,
    string? MediaType,
    bool Success,
    string? Error,
    AnalysisResult? Result
);
