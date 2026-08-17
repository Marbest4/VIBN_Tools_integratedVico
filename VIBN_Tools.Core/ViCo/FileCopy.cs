namespace VIBN_Tools.Core.ViCo;

public sealed record FileCopyItem(string SourcePath, string DestinationPath);

public sealed record FileCopyProgress(
    int Percent,
    long BytesCopied,
    long TotalBytes,
    string CurrentFile);

public interface IFileCopyService
{
    Task CopyAsync(
        IReadOnlyCollection<FileCopyItem> items,
        IProgress<FileCopyProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public interface IExternalPathLauncher
{
    void Open(string path);
}
