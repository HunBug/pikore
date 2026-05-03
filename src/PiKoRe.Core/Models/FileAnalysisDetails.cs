namespace PiKoRe.Core.Models;

/// <summary>
/// Aggregated analysis data for one file from PostgreSQL.
/// Returned by IMediaStore.GetFileDetailsAsync — does NOT include file-identity
/// fields (path, hash, mtime); those come from IFileIndexStore.
/// </summary>
public sealed record FileAnalysisDetails(
    Guid FileId,
    IReadOnlyList<(string Key, string Value)> Metadata,
    IReadOnlyList<(string Label, float Confidence)> Tags,
    string? ThumbnailPath,
    bool HasEmbedding,
    string? Description
);
