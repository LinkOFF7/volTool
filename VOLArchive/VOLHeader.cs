using System;
using System.IO;

namespace volTool.VOLArchive
{
    internal class VOLHeader
    {
        public uint Magic = 0xB53D32CB;
        public uint Unknown0x04 { get; private set; }
        public uint Unknown0x08 { get; private set; }
        public uint UnknownTableStartOffset { get; private set; }  // always the same value - 0x1c
        public uint FileSize { get; set; }
        public uint NumOfFiles { get; set; }
        public uint InfoTableStartOffset { get; set; } // always the same value - position after UnknownTableRawData

        private byte[] UnknownTableRawData { get; set; }

        public VOLHeader(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic) throw new Exception();
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            UnknownTableStartOffset = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            NumOfFiles = reader.ReadUInt32();
            InfoTableStartOffset = reader.ReadUInt32();
            UnknownTableRawData = reader.ReadBytes((int)(InfoTableStartOffset - UnknownTableStartOffset));
        }


        public void Write(BinaryWriter writer)
        {
            if (this != null)
            {
                writer.Write(Magic);
                writer.Write(Unknown0x04);
                writer.Write(Unknown0x08);
                writer.Write(UnknownTableStartOffset);
                writer.Write((uint)writer.BaseStream.Length);
                writer.Write(NumOfFiles);
                writer.Write(InfoTableStartOffset);
                writer.Write(UnknownTableRawData);
            }
        }
    }
}
