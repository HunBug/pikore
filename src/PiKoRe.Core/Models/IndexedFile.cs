namespace PiKoRe.Core.Models;

public sealed record IndexedFile(
    Guid Id,
    string Path,
    long SizeBytes,
    DateTimeOffset MTime,
    string Hash,
    DateTimeOffset IngestedAt,
    string MediaType
);
