using System.Text;

namespace Decomp.Core;

/// <summary>
/// Compatibility wrapper for synchronous module readers plus asynchronous bulk helpers.
/// </summary>
public sealed class FileReader : IDisposable
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly StreamReader reader;
    private bool disposed;

    public FileReader(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var stream = new FileStream(
            fileName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        reader = new StreamReader(
            stream,
            Utf8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024);
    }

    public int Read()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return reader.Read();
    }

    public int Peek()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return reader.Peek();
    }

    public string? ReadLine()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return reader.ReadLine();
    }

    public void Close() => Dispose();

    public static string[] ReadAllLines(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllLines(path, Utf8);
    }

    public static async Task<string[]> ReadAllLinesAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return await File.ReadAllLinesAsync(path, Utf8, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return await File.ReadAllTextAsync(path, Utf8, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        reader.Dispose();
        disposed = true;
        GC.SuppressFinalize(this);
    }
}
