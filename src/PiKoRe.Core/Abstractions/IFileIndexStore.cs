using PiKoRe.Core.Models;

namespace PiKoRe.Core.Abstractions;

/// <summary>
/// Read/write access to the SQLite file_index table.
/// Owns all file-identity data: path, hash, size, mtime, media_type.
/// IMediaStore owns analysis results; this interface owns the file record itself.
/// </summary>
public interface IFileIndexStore
{
    /// <summary>
    /// Returns the existing row if path + mtime + size all match — used by
    /// FileScanner to skip unchanged files without re-hashing.
    /// </summary>
    Task<IndexedFile?> GetByPathAsync(string path, CancellationToken ct);

    Task<IndexedFile?> GetByIdAsync(Guid fileId, CancellationToken ct);

    /// <summary>Insert or update the file_index row for this file.</summary>
    Task UpsertAsync(IndexedFile file, CancellationToken ct);
}
