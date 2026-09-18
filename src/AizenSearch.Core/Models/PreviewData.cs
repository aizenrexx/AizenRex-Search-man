namespace AizenSearch.Core.Models;

public sealed class PreviewData
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public string Kind { get; init; } = "meta"; // "image", "audio", "video", "text", "folder", "exe", "meta"
    public string Size { get; init; } = "Unavailable";
    public string Modified { get; init; } = string.Empty;
    public string Created { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string IconBase64 { get; init; } = string.Empty;
    public string ExtraInfo { get; init; } = string.Empty;
}
