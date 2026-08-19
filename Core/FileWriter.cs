using System.Globalization;
using System.Text;

namespace Decomp.Core;

/// <summary>
/// Buffered writer used by legacy module emitters.
/// </summary>
public sealed class FileWriter : IDisposable
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly StreamWriter? writer;
    private readonly TextWriter? textWriter;
    private readonly StringBuilder buffer = new();
    private bool disposed;

    public FileWriter(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var directory = Path.GetDirectoryName(Path.GetFullPath(fileName));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var stream = new FileStream(
            fileName,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        writer = new StreamWriter(
            stream,
            Utf8,
            bufferSize: 64 * 1024)
        {
            NewLine = "\n",
        };
    }

    public FileWriter(TextWriter textWriter)
    {
        this.textWriter = textWriter ?? throw new ArgumentNullException(nameof(textWriter));
    }

    private void WriteToOutput(string value)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (writer is not null)
            buffer.Append(value);
        else
            textWriter!.Write(value);
    }

    private void FlushToOutput()
    {
        if (writer is null || buffer.Length == 0)
            return;

        writer.Write(buffer.ToString());
        buffer.Clear();
    }

    public void Write(char value) => WriteToOutput(value.ToString());

    public void Write(char[]? value) => WriteToOutput(new string(value ?? []));

    public void Write(string? value) => WriteToOutput(value ?? string.Empty);

    public void Write(bool value) => WriteToOutput(value ? "True" : "False");

    public void Write(int value) => WriteToOutput(value.ToString(CultureInfo.InvariantCulture));

    public void Write(uint value) => WriteToOutput(value.ToString(CultureInfo.InvariantCulture));

    public void Write(long value) => WriteToOutput(value.ToString(CultureInfo.InvariantCulture));

    public void Write(ulong value) => WriteToOutput(value.ToString(CultureInfo.InvariantCulture));

    public void Write(float value) => WriteToOutput(value.ToString(CultureInfo.InvariantCulture));

    public void Write(double value) => WriteToOutput(value.ToString(CultureInfo.InvariantCulture));

    public void Write(decimal value) => WriteToOutput(value.ToString(CultureInfo.InvariantCulture));

    public void Write(object? value)
    {
        if (value is not null)
            WriteToOutput(value.ToString() ?? string.Empty);
    }

    public void Write(string format, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(format);
        WriteToOutput(string.Format(CultureInfo.InvariantCulture, format, args));
    }

    public void WriteLine() => WriteToOutput("\n");

    public void WriteLine(char value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(char[]? value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(bool value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(int value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(uint value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(long value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(ulong value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(float value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(double value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(decimal value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(string? value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(object? value)
    {
        Write(value);
        WriteLine();
    }

    public void WriteLine(string format, params object?[] args)
    {
        Write(format, args);
        WriteLine();
    }

    public void Close() => Dispose();

    public void Flush()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        FlushToOutput();
        writer?.Flush();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        FlushToOutput();
        writer?.Dispose();
        disposed = true;
        GC.SuppressFinalize(this);
    }

    public static void WriteAllText(string fileName, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        var directory = Path.GetDirectoryName(Path.GetFullPath(fileName));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(fileName, content, Utf8);
    }

    public static async Task WriteAllTextAsync(
        string fileName,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        var directory = Path.GetDirectoryName(Path.GetFullPath(fileName));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(fileName, content, Utf8, cancellationToken).ConfigureAwait(false);
    }
}
