using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zlib;

namespace volTool
{
    public static class Utils
    {
        public static byte[] Decompress(byte[] data, bool zlib = false)
        {
            int outputSize = 1024;
            byte[] output = new Byte[outputSize];
            bool expectRfc1950Header = zlib;
            using (MemoryStream ms = new MemoryStream())
            {
                ZlibCodec compressor = new ZlibCodec();
                compressor.InitializeInflate(expectRfc1950Header);

                compressor.InputBuffer = data;
                compressor.AvailableBytesIn = data.Length;
                compressor.NextIn = 0;
                compressor.OutputBuffer = output;

                foreach (var f in new FlushType[] { FlushType.None, FlushType.Finish })
                {
                    int bytesToWrite = 0;
                    do
                    {
                        compressor.AvailableBytesOut = outputSize;
                        compressor.NextOut = 0;
                        compressor.Inflate(f);

                        bytesToWrite = outputSize - compressor.AvailableBytesOut;
                        if (bytesToWrite > 0)
                            ms.Write(output, 0, bytesToWrite);
                    }
                    while ((f == FlushType.None && (compressor.AvailableBytesIn != 0 || compressor.AvailableBytesOut == 0)) ||
                        (f == FlushType.Finish && bytesToWrite != 0));
                }

                compressor.EndInflate();

                return ms.ToArray();
            }
        }

        public static byte[] Compress(byte[] data, bool zlib = false)
        {
            int outputSize = 1024;
            byte[] output = new Byte[outputSize];
            int lengthToCompress = data.Length;
            bool wantRfc1950Header = zlib;

            using (MemoryStream ms = new MemoryStream())
            {
                ZlibCodec compressor = new ZlibCodec();
                compressor.InitializeDeflate(Ionic.Zlib.CompressionLevel.BestCompression, wantRfc1950Header);

                compressor.InputBuffer = data;
                compressor.AvailableBytesIn = lengthToCompress;
                compressor.NextIn = 0;
                compressor.OutputBuffer = output;

                foreach (var f in new FlushType[] { FlushType.None, FlushType.Finish })
                {
                    int bytesToWrite = 0;
                    do
                    {
                        compressor.AvailableBytesOut = outputSize;
                        compressor.NextOut = 0;
                        compressor.Deflate(f);

                        bytesToWrite = outputSize - compressor.AvailableBytesOut;
                        if (bytesToWrite > 0)
                            ms.Write(output, 0, bytesToWrite);
                    }
                    while ((f == FlushType.None && (compressor.AvailableBytesIn != 0 || compressor.AvailableBytesOut == 0)) ||
                        (f == FlushType.Finish && bytesToWrite != 0));
                }

                compressor.EndDeflate();

                ms.Flush();
                return ms.ToArray();
            }
        }

        public static void AlignPosition(BinaryWriter writer, uint align = 0x10)
        {
            writer.BaseStream.Position = GetAlignment((uint)writer.BaseStream.Position, align);
        }

        public static uint GetAlignment(uint val, uint align = 0x10)
        {
            return (align - val % align) + val;
        }

        public static string ReadString(this BinaryReader binaryReader, Encoding encoding)
        {
            if (binaryReader == null) throw new ArgumentNullException("binaryReader");
            if (encoding == null) throw new ArgumentNullException("encoding");

            List<byte> data = new List<byte>();

            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
            {
                data.Add(binaryReader.ReadByte());

                string partialString = encoding.GetString(data.ToArray(), 0, data.Count);

                if (partialString.Length > 0 && partialString.Last() == '\0')
                    return encoding.GetString(data.SkipLast(encoding.GetByteCount("\0")).ToArray()).TrimEnd('\0');
            }
            throw new InvalidDataException("Hit end of stream while reading null-terminated string.");
        }
        private static IEnumerable<TSource> SkipLast<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");

            Queue<TSource> queue = new Queue<TSource>();

            foreach (TSource item in source)
            {
                queue.Enqueue(item);

                if (queue.Count > count) yield return queue.Dequeue();
            }
        }
    }
}
