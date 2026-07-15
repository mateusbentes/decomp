using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Decomp.Core
{
    public class Text : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly StringBuilder _stringBuilder = new();
        private bool _disposed;

        public Text(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            _reader = new StreamReader(filePath, Encoding.UTF8);
        }

        public int Peek() => _reader.Peek();

        public string ReadWord()
        {
            _stringBuilder.Clear();
            while (Peek() != -1)
            {
                var character = (char)_reader.Read();
                if (char.IsWhiteSpace(character))
                {
                    if (_stringBuilder.Length > 0) break;
                    continue;
                }
                _stringBuilder.Append(character);
            }
            return _stringBuilder.ToString();
        }

        public long ReadInt64()
        {
            var word = ReadWord();
            return long.TryParse(word, out var result) ? result : 0;
        }

        public ulong ReadUInt64()
        {
            var word = ReadWord();
            return ulong.TryParse(word, out var result) ? result : 0;
        }

        public int ReadInt() => (int)ReadInt64();
        public uint ReadUInt() => (uint)ReadUInt64();
        public uint ReadDWord() => (uint)ReadUInt64();

        public double ReadDouble()
        {
            var word = ReadWord();
            return double.TryParse(word, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0.0;
        }

        public string? ReadLine() => _reader.ReadLine();

        public void Close() => _reader.Close();

        public static string? GetFirstLineFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            using var reader = new StreamReader(filePath);
            return reader.ReadLine();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
                _reader.Dispose();

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
