using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Decomp.Core
{
    public class Text : IDisposable
    {
        private readonly StreamReader _reader;
        private readonly StringBuilder _sb = new();
        private bool _disposed;

        public Text(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            _reader = new StreamReader(fileName, Encoding.UTF8);
        }

        public int Peek() => _reader.Peek();

        public string GetWord()
        {
            _sb.Clear();
            while (Peek() != -1)
            {
                char c = (char)_reader.Read();
                if (char.IsWhiteSpace(c))
                {
                    if (_sb.Length > 0) break;
                    continue;
                }
                _sb.Append(c);
            }
            return _sb.ToString();
        }

        public long GetInt64()
        {
            string s = GetWord();
            return long.TryParse(s, out long result) ? result : 0;
        }

        public ulong GetUInt64()
        {
            string s = GetWord();
            return ulong.TryParse(s, out ulong result) ? result : 0;
        }

        public int GetInt() => (int)GetInt64();
        public uint GetUInt() => (uint)GetUInt64();
        public uint GetDWord() => (uint)GetUInt64();

        public double GetDouble()
        {
            string s = GetWord();
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double result) ? result : 0.0;
        }

        public string GetString() => _reader.ReadLine() ?? string.Empty;

        public void Close()
        {
            _reader.Close();
        }

        public static string GetFirstStringFromFile(string sFileName)
        {
            if (!File.Exists(sFileName)) return string.Empty;

            using var f = new StreamReader(sFileName);
            return f.ReadLine() ?? string.Empty;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reader.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
