using MediatR;
using PiKoRe.Core.Models;

namespace PiKoRe.Core.Events;

public sealed record JobCompletedEvent(JobResult Result) : INotification;
