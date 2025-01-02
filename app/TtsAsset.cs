using MimeTypes;

namespace TTSRehoster;

public record struct TtsAsset
{
    private static readonly string[] ExcludedExtensions = [".html", ".bin"];

    public required string Url;
    public required string UrlHash;
    public string? MimeType;
    public string LocalPath;
    public bool Downloaded;

    private string RawExtension => MimeTypeMap.GetExtension(MimeType ?? "application/octet-stream");

    // Media should use correct mime, but we should just ignore ExcludedExtensions
    public string Extension => ExcludedExtensions.Contains(RawExtension) ? "" : RawExtension;

    public override string ToString()
    {
        return $"{Url} ({UrlHash})";
    }
}