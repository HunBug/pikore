using MediatR;

namespace PiKoRe.Core.Events;

public sealed record JobFailedEvent(Guid JobId, string Error) : INotification;
