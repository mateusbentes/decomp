using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Decomp.Core
{
    public class FileWriter : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly StringBuilder _sb = new StringBuilder();
        private bool _disposed;

        public IFormatProvider FormatProvider { get; set; } = CultureInfo.InvariantCulture;

        public FileWriter(string path)
        {
            _writer = new StreamWriter(path, false, Encoding.UTF8);
        }

        public FileWriter(Stream stream)
        {
            _writer = new StreamWriter(stream, Encoding.UTF8);
        }

        public void Write(char value) => _sb.Append(value);
        public void Write(char[] buffer) => _sb.Append(buffer);
        public void Write(string value) => _sb.Append(value);
        public void Write(bool value) => Write(value ? "True" : "False");
        public void Write(int value) => Write(value.ToString(FormatProvider));
        public void Write(uint value) => Write(value.ToString(FormatProvider));
        public void Write(long value) => Write(value.ToString(FormatProvider));
        public void Write(ulong value) => Write(value.ToString(FormatProvider));
        public void Write(float value) => Write(value.ToString(FormatProvider));
        public void Write(double value) => Write(value.ToString(FormatProvider));
        public void Write(decimal value) => Write(value.ToString(FormatProvider));

        public void Write(object value)
        {
            if (value == null) return;
            Write(value.ToString());
        }

        public void Write(string format, params object[] arg) => Write(arg == null ? format : string.Format(FormatProvider, format, arg));

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

        public void WriteLine(char[] buffer)
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

        public void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }

        public void WriteLine(object value)
        {
            Write(value);
            WriteLine();
        }

        public void WriteLine(string format, params object[] arg)
        {
            Write(format, arg);
            WriteLine();
        }

        public void Close()
        {
            if (_sb.Length > 0)
            {
                _writer.Write(_sb.ToString());
                _sb.Clear();
            }
            _writer.Flush();
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
    }
}
