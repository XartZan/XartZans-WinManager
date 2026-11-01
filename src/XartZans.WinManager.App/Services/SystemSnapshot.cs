namespace XartZans.WinManager.App.Services;

public sealed record SystemSnapshot(
    string ComputerName,
    string OperatingSystem,
    string? ProcessorModel,
    string? GraphicsModel,
    double? CpuUsagePercent,
    ulong UsedMemoryBytes,
    ulong TotalMemoryBytes,
    DriveSnapshot? SystemDrive,
    double? DownloadBytesPerSecond,
    double? UploadBytesPerSecond,
    DateTimeOffset CapturedAt);

public sealed record DriveSnapshot(string Name, ulong UsedBytes, ulong TotalBytes, ulong FreeBytes)
{
    public double? UsagePercent => TotalBytes == 0 ? null : (double)UsedBytes / TotalBytes * 100;
}
