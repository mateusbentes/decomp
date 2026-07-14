using System;
using System.IO;
using System.Text;

namespace Decomp.Core
{
    public class BinaryFileReader : IDisposable
    {
        private readonly BinaryReader _reader;
        private bool _disposed;

        public int Position
        {
            get => (int)_reader.BaseStream.Position;
            set => _reader.BaseStream.Position = value;
        }

        public BinaryFileReader(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            _reader = new BinaryReader(File.OpenRead(fileName), Encoding.UTF8);
        }

        public sbyte ReadSingedByte() => _reader.ReadSByte();
        public byte ReadByte() => _reader.ReadByte();

        public byte[] ReadBytes(int numBytes)
        {
            return _reader.ReadBytes(numBytes);
        }

        public void SkipBytes(int numBytes)
        {
            _reader.BaseStream.Seek(numBytes, SeekOrigin.Current);
        }

        public short ReadInt16() => _reader.ReadInt16();
        public ushort ReadUInt16() => _reader.ReadUInt16();
        public int ReadInt32() => _reader.ReadInt32();
        public uint ReadUInt32() => _reader.ReadUInt32();
        public long ReadInt64() => _reader.ReadInt64();
        public ulong ReadUInt64() => _reader.ReadUInt64();
        public float ReadFloat() => _reader.ReadSingle();
        public double ReadDouble() => _reader.ReadDouble();

        public string ReadAsciiString()
        {
            var length = ReadInt32();
            if (length == 0) return string.Empty;

            var bytes = ReadBytes(length);
            return Encoding.ASCII.GetString(bytes);
        }

        public void Close()
        {
            _reader.Close();
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
