using System;
using System.IO;

namespace volTool.VOLArchive
{
    public enum CompressionType: byte
    {
        Zlib = 0x0,
        Deflate = 0x1
    }
    public class VOLBase
    {
        public string VolFileName { get; set; }

        public uint Magic = 0xD177F050;
        public uint Flags { get; set; }
        public CompressionType Compression => (CompressionType)(Flags & 0xFF);
        public bool CompressionFlag => (Flags & 0xFF00) == 0 ? false : true; // not used here
        public uint CompressedSize { get; set; }
        public uint UncompressedSize { get; set; }
        public byte[] DataBuffer { get; set; }
        public bool IsCompressed = true;
        public void Read()
        {
            if (VolFileName == null)
                throw new Exception();
            using(BinaryReader reader = new BinaryReader(File.OpenRead(VolFileName)))
            {
                if(reader.ReadUInt32() != Magic) throw new Exception("[VOLBase] Unknown magic");
                Flags = reader.ReadUInt32();
                CompressedSize = reader.ReadUInt32();
                UncompressedSize = reader.ReadUInt32();
                switch (Compression)
                {
                    case CompressionType.Zlib:
                        DataBuffer = Utils.Decompress(reader.ReadBytes((int)CompressedSize), true);
                        break;
                    case CompressionType.Deflate:
                        DataBuffer = Utils.Decompress(reader.ReadBytes((int)CompressedSize), false);
                        break;
                    default: throw new Exception("[VOLBase] Unknown compression");
                }
                IsCompressed = false;
            }
        }

        public void Write(string newVolFile)
        {
            if (VolFileName == null)
                throw new Exception();
            using(BinaryWriter writer = new BinaryWriter(File.Create(newVolFile)))
            {
                UncompressedSize = (uint)DataBuffer.Length;
                switch (Compression)
                {
                    case CompressionType.Zlib:
                        DataBuffer = Utils.Compress(DataBuffer, true);
                        break;
                    case CompressionType.Deflate:
                        DataBuffer = Utils.Compress(DataBuffer, false);
                        break;
                    default: throw new Exception("[VOLBase] Unknown compression");
                }
                CompressedSize = (uint)DataBuffer.Length;
                IsCompressed = true;

                writer.Write(Magic);
                writer.Write(Flags);
                writer.Write(CompressedSize);
                writer.Write(UncompressedSize);
                writer.Write(DataBuffer);
            }
        }
    }
}
