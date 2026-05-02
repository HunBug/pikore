using MediatR;

namespace PiKoRe.Core.Events;

public sealed record PluginRegisteredEvent(string PluginName) : INotification;
