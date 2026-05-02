namespace PiKoRe.Core.Models;

public sealed record Job(
    Guid Id,
    Guid FileId,
    string Capability,
    JobStatus Status,
    Guid? PluginId,
    int Priority,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    string? Error
);
