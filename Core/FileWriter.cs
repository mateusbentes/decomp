using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Decomp.Core
{
    public class FileWriter : IDisposable
    {
        private readonly StreamWriter? _writer;
        private readonly TextWriter? _textWriter;
        private readonly StringBuilder _sb = new();
        private bool _disposed;

        public FileWriter(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            _writer = new StreamWriter(fileName, false, Encoding.UTF8);
        }

        public FileWriter(TextWriter textWriter)
        {
            _textWriter = textWriter ?? throw new ArgumentNullException(nameof(textWriter));
        }

        private void WriteToOutput(string value)
        {
            if (_writer != null)
            {
                _sb.Append(value);
            }
            else if (_textWriter != null)
            {
                _textWriter.Write(value);
            }
        }

        private void FlushToOutput()
        {
            if (_writer != null && _sb.Length > 0)
            {
                _writer.Write(_sb.ToString());
                _sb.Clear();
            }
        }

        public void Write(char value) => WriteToOutput(value.ToString());
        public void Write(char[]? buffer) => WriteToOutput(new string(buffer ?? Array.Empty<char>()));
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
            if (value == null) return;
            WriteToOutput(value.ToString() ?? string.Empty);
        }

        public void Write(string format, params object?[] args)
        {
            if (format == null) return;
            WriteToOutput(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public void WriteLine()
        {
            WriteToOutput(Environment.NewLine);
            FlushToOutput();
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
            FlushToOutput();
            _writer?.Close();
        }

        public void Flush()
        {
            FlushToOutput();
            _writer?.Flush();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Flush();
                    _writer?.Dispose();
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
