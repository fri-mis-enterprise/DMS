namespace Document_Management.Models;

public sealed class HomeDashboardViewModel
{
    public string Username { get; init; } = string.Empty;

    public long ActiveDocuments { get; init; }

    public long UploadedThisMonth { get; init; }

    public long TotalPages { get; init; }

    public long StorageUsedBytes { get; init; }

    public string FormattedStorageUsed => FormatFileSize(StorageUsedBytes);

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = Math.Max(0, bytes);
        var unitIndex = 0;
        var displaySize = (double)size;

        while (displaySize >= 1024 && unitIndex < units.Length - 1)
        {
            displaySize /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{displaySize:0} {units[unitIndex]}"
            : $"{displaySize:0.##} {units[unitIndex]}";
    }
}
