using System.Collections.Concurrent;
using VIBN_Tools.Core.ViCo;

namespace VIBN_Tools.Infrastructure.ViCo;

public sealed class BoundedFileCopyService : IFileCopyService
{
    private const int BufferSize = 1024 * 1024;
    private readonly int _maximumParallelCopies;

    public BoundedFileCopyService(int maximumParallelCopies = 2)
    {
        if (maximumParallelCopies < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumParallelCopies));

        _maximumParallelCopies = maximumParallelCopies;
    }

    public async Task CopyAsync(
        IReadOnlyCollection<FileCopyItem> items,
        IProgress<FileCopyProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var files = Expand(items, cancellationToken);
        var totalBytes = files.Sum(file => new FileInfo(file.SourcePath).Length);
        long copiedBytes = 0;

        await Parallel.ForEachAsync(
            files,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = _maximumParallelCopies
            },
            async (file, token) =>
            {
                var destinationDirectory = Path.GetDirectoryName(file.DestinationPath);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                await using var source = new FileStream(
                    file.SourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                await using var destination = new FileStream(
                    file.DestinationPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                var buffer = new byte[BufferSize];
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, token)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    var current = Interlocked.Add(ref copiedBytes, bytesRead);
                    var percent = totalBytes == 0 ? 100 : (int)(current * 100 / totalBytes);
                    progress?.Report(new FileCopyProgress(percent, current, totalBytes, file.SourcePath));
                }
            });

        progress?.Report(new FileCopyProgress(100, copiedBytes, totalBytes, string.Empty));
    }

    private static IReadOnlyList<FileCopyItem> Expand(
        IEnumerable<FileCopyItem> items,
        CancellationToken cancellationToken)
    {
        var files = new ConcurrentBag<FileCopyItem>();

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(item.SourcePath))
            {
                files.Add(item);
                continue;
            }

            if (!Directory.Exists(item.SourcePath))
                throw new FileNotFoundException("Quelle wurde nicht gefunden.", item.SourcePath);

            foreach (var sourceFile in Directory.EnumerateFiles(
                         item.SourcePath,
                         "*",
                         SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(item.SourcePath, sourceFile);
                files.Add(new FileCopyItem(
                    sourceFile,
                    Path.Combine(item.DestinationPath, relativePath)));
            }
        }

        return files.ToArray();
    }
}
