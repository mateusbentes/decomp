using System.Globalization;
using System.Text;

namespace Decomp.Core;

/// <summary>
/// Token and line reader for TaleWorlds text resources.
/// </summary>
public sealed class Text : IDisposable
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly StreamReader reader;
    private readonly StringBuilder stringBuilder = new();
    private bool disposed;

    public Text(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var stream = new FileStream(
            filePath,
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

    public int Peek()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return reader.Peek();
    }

    public string ReadWord()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        stringBuilder.Clear();

        while (reader.Peek() != -1)
        {
            var character = (char)reader.Read();
            if (char.IsWhiteSpace(character))
            {
                if (stringBuilder.Length > 0)
                    break;

                continue;
            }

            stringBuilder.Append(character);
        }

        return stringBuilder.ToString();
    }

    public long ReadInt64()
    {
        var word = ReadWord();
        return long.TryParse(
            word,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : 0;
    }

    public ulong ReadUInt64()
    {
        var word = ReadWord();
        return ulong.TryParse(
            word,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : 0;
    }

    public int ReadInt() => (int)ReadInt64();

    public uint ReadUInt() => (uint)ReadUInt64();

    public uint ReadDWord() => ReadUInt();

    public double ReadDouble()
    {
        var word = ReadWord();
        return double.TryParse(
            word,
            NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : 0.0;
    }

    public string? ReadLine()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return reader.ReadLine();
    }

    public string GetString() => ReadLine() ?? string.Empty;

    public int GetInt() => ReadInt();

    public uint GetUInt() => ReadUInt();

    public long GetInt64() => ReadInt64();

    public ulong GetUInt64() => ReadUInt64();

    public uint GetDWord() => ReadDWord();

    public double GetDouble() => ReadDouble();

    public string GetWord() => ReadWord();

    public void Close() => Dispose();

    public static string? GetFirstLineFromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            return null;

        using var reader = new StreamReader(
            filePath,
            Utf8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024);
        return reader.ReadLine();
    }

    public static async Task<string?> GetFirstLineFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            return null;

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(
            stream,
            Utf8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024,
            leaveOpen: false);
        return await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
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
