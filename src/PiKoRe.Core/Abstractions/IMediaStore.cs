using PiKoRe.Core.Models;

namespace PiKoRe.Core.Abstractions;

/// <summary>Read/write access to all analysis data in PostgreSQL.</summary>
public interface IMediaStore
{
    /// <summary>
    /// Returns aggregated analysis data for one file from PostgreSQL.
    /// Does not include file-identity fields — query IFileIndexStore for those.
    /// Returns null if no analysis data exists yet for this file.
    /// </summary>
    Task<FileAnalysisDetails?> GetFileDetailsAsync(Guid fileId, CancellationToken ct);

    /// <summary>
    /// Returns file IDs of the top <paramref name="limit"/> nearest neighbours
    /// in embedding space. Call IFileIndexStore.GetByIdAsync to resolve paths.
    /// </summary>
    Task<IReadOnlyList<Guid>> SearchByEmbeddingAsync(float[] queryVector, int limit, CancellationToken ct);

    Task UpsertMetadataAsync(Guid fileId, string key, string value, string sourcePlugin, CancellationToken ct);
    Task UpsertTagAsync(Guid fileId, string label, float confidence, string sourcePlugin, CancellationToken ct);
    Task UpsertEmbeddingAsync(Guid fileId, string modelId, float[] vector, CancellationToken ct);
    Task UpsertDescriptionAsync(Guid fileId, string text, string sourcePlugin, CancellationToken ct);
    Task UpsertThumbnailAsync(Guid fileId, string sizeClass, string dataPath, CancellationToken ct);
}
