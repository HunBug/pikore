namespace PiKoRe.Core.Models;

public sealed record AnalysisRequest(
    Guid JobId,
    Guid FileId,
    string FilePath,
    string? PreviewPath
);
