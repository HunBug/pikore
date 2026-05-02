using MediatR;
using PiKoRe.Core.Models;

namespace PiKoRe.Core.Events;

public sealed record FileIndexedEvent(IndexedFile File) : INotification;
