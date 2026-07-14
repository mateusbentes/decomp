using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Decomp.Core
{
    public class FileReader : IDisposable
    {
        private readonly StreamReader _reader;
        private bool _disposed;

        public FileReader(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            _reader = new StreamReader(fileName, Encoding.UTF8);
        }

        public int Read()
        {
            return _reader.Read();
        }

        public int Peek()
        {
            return _reader.Peek();
        }

        public string? ReadLine()
        {
            return _reader.ReadLine();
        }

        public void Close()
        {
            _reader.Close();
        }

        public static string[] ReadAllLines(string path)
        {
            return File.ReadAllLines(path, Encoding.UTF8);
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
