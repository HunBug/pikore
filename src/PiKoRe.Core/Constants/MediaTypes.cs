namespace PiKoRe.Core.Constants;

public static class MediaTypes
{
    public const string Image = "image/*";
    public const string Video = "video/*";
    public const string Audio = "audio/*";
    public const string All   = "*";

    private static readonly Dictionary<string, string> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"]  = Image,
        [".jpeg"] = Image,
        [".png"]  = Image,
        [".gif"]  = Image,
        [".webp"] = Image,
        [".heic"] = Image,
        [".heif"] = Image,
        [".tiff"] = Image,
        [".bmp"]  = Image,

        [".mp4"]  = Video,
        [".mov"]  = Video,
        [".mkv"]  = Video,
        [".avi"]  = Video,
        [".webm"] = Video,
        [".m4v"]  = Video,

        [".mp3"]  = Audio,
        [".flac"] = Audio,
        [".wav"]  = Audio,
        [".aac"]  = Audio,
        [".m4a"]  = Audio,
        [".ogg"]  = Audio,
        [".opus"] = Audio,
        [".wma"]  = Audio,
    };

    /// <summary>
    /// Returns the media type category for a file extension (with or without leading dot).
    /// Returns null if the extension is not recognised.
    /// </summary>
    public static string? FromExtension(string extension)
    {
        var ext = extension.StartsWith('.') ? extension : '.' + extension;
        return ExtensionMap.TryGetValue(ext, out var mediaType) ? mediaType : null;
    }

    /// <summary>
    /// Returns true if <paramref name="fileMediaType"/> matches any entry in
    /// <paramref name="supported"/>. Supports wildcard <c>"*"</c> (all types).
    /// </summary>
    public static bool IsSupported(string fileMediaType, IReadOnlyList<string> supported)
        => supported.Contains(All) || supported.Contains(fileMediaType);
}
