namespace PiKoRe.Core.Abstractions;

/// <summary>Base contract for all plugins — in-process and external.</summary>
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    IReadOnlyList<string> CapabilitiesProduced { get; }
    IReadOnlyList<string> RequiredCapabilities { get; }
    /// <summary>
    /// MIME-category strings this plugin can process.
    /// Use constants from <see cref="PiKoRe.Core.Constants.MediaTypes"/>.
    /// Examples: ["image/*"], ["video/*"], ["audio/*"], ["*"] (all types).
    /// </summary>
    IReadOnlyList<string> SupportedMediaTypes { get; }
}
