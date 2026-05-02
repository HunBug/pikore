using PiKoRe.Core.Models;

namespace PiKoRe.Core.Abstractions;

/// <summary>Scans and watches a library path, emitting indexed file records.</summary>
public interface IFileScanner
{
    Task ScanAsync(string libraryPath, CancellationToken ct);
    IAsyncEnumerable<IndexedFile> WatchAsync(string libraryPath, CancellationToken ct);
}
