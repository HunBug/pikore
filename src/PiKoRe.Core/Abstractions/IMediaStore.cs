using PiKoRe.Core.Models;

namespace PiKoRe.Core.Abstractions;

/// <summary>Read/write access to all analysis data in PostgreSQL.</summary>
public interface IMediaStore
{
    Task<IndexedFile?> GetByIdAsync(Guid fileId, CancellationToken ct);
    Task<IReadOnlyList<IndexedFile>> SearchByEmbeddingAsync(float[] queryVector, int limit, CancellationToken ct);
    Task UpsertMetadataAsync(Guid fileId, string key, string value, string sourcePlugin, CancellationToken ct);
    Task UpsertTagAsync(Guid fileId, string label, float confidence, string sourcePlugin, CancellationToken ct);
    Task UpsertEmbeddingAsync(Guid fileId, string modelId, float[] vector, CancellationToken ct);
    Task UpsertDescriptionAsync(Guid fileId, string text, string sourcePlugin, CancellationToken ct);
}
