using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Decomp.Core
{
    public class FileWriter : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly StringBuilder _sb = new();
        private bool _disposed;

        public FileWriter(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            _writer = new StreamWriter(fileName, false, Encoding.UTF8);
        }

        public void Write(char value) => _sb.Append(value);
        public void Write(char[]? buffer) => _sb.Append(buffer ?? Array.Empty<char>());
        public void Write(string? value) => _sb.Append(value ?? string.Empty);
        public void Write(bool value) => Write(value ? "True" : "False");
        public void Write(int value) => Write(value.ToString(CultureInfo.InvariantCulture));
        public void Write(uint value) => Write(value.ToString(CultureInfo.InvariantCulture));
        public void Write(long value) => Write(value.ToString(CultureInfo.InvariantCulture));
        public void Write(ulong value) => Write(value.ToString(CultureInfo.InvariantCulture));
        public void Write(float value) => Write(value.ToString(CultureInfo.InvariantCulture));
        public void Write(double value) => Write(value.ToString(CultureInfo.InvariantCulture));
        public void Write(decimal value) => Write(value.ToString(CultureInfo.InvariantCulture));

        public void Write(object? value)
        {
            if (value == null) return;
            Write(value.ToString() ?? string.Empty);
        }

        public void Write(string format, params object?[] args)
        {
            if (format == null) return;
            Write(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void WriteLine()
        {
            _writer.WriteLine(_sb.ToString());
            _sb.Clear();
        }

        public void WriteLine(char value)
        {
            Write(value);
            WriteLine();
        }

        public void WriteLine(char[]? buffer)
        {
            Write(buffer);
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

        public void Close()
        {
            if (_sb.Length > 0)
            {
                _writer.Write(_sb.ToString());
                _sb.Clear();
            }
            _writer.Close();
        }

        public void Flush()
        {
            if (_sb.Length > 0)
            {
                _writer.Write(_sb.ToString());
                _sb.Clear();
            }
            _writer.Flush();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Flush();
                    _writer.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static void WriteAllText(string fileName, string content)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            File.WriteAllText(fileName, content, Encoding.UTF8);
        }
    }
}
