namespace PiKoRe.Core.Abstractions;

/// <summary>Base contract for all plugins — in-process and external.</summary>
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    IReadOnlyList<string> CapabilitiesProduced { get; }
    IReadOnlyList<string> RequiredCapabilities { get; }
}
