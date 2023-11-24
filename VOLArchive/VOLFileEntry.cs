using System.IO;
using System.Text;

namespace volTool.VOLArchive
{
    internal class VOLFileEntry
    {
        public uint CRC { get; }
        public uint NameOffset { get; set; }
        public uint Offset { get; set; }
        public uint CompressedSize { get; } // not used, always zero
        public uint Size { get; set; } 

        public string Name { get; private set; }

        public VOLFileEntry(BinaryReader reader)
        {
            CRC = reader.ReadUInt32();
            NameOffset = reader.ReadUInt32();
            Offset = reader.ReadUInt32();
            CompressedSize = reader.ReadUInt32();
            Size = reader.ReadUInt32();

            var savepos = reader.BaseStream.Position;
            reader.BaseStream.Position = NameOffset;
            Name = Utils.ReadString(reader, Encoding.ASCII);
            reader.BaseStream.Position = savepos;
        }
        public byte[] GetBytes(BinaryReader reader)
        {
            reader.BaseStream.Position = Offset;
            return reader.ReadBytes((int)Size);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(CRC);
            writer.Write(NameOffset);
            writer.Write(Offset);
            writer.Write(CompressedSize);
            writer.Write(Size);
        }
    }
}
