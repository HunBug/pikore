namespace PiKoRe.Core.Models;

public sealed record AnalysisResult(
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<FaceResult>? Faces = null,
    float[]? Embedding = null,
    Dictionary<string, float>? Scores = null,
    string? Description = null,
    string? PreviewPath = null
);

public sealed record FaceResult(
    string BboxJson,
    float[]? Embedding
);
