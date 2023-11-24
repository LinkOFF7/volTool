using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using volTool.VOLArchive;

namespace volTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 2)
            {
                if (args[0] == "extract")
                {
                    ExtractVOL(args[1]);
                    return;
                }
                else if(args[0] == "rebuild" && args.Length == 4)
                {
                    RebuildVOL(args[1], args[2], args[3]);
                    return;
                }
                else
                {
                    PrintUsage();
                    return;
                }
            }
            else if(args.Length == 1 && args[0].EndsWith(".vol"))
            {
                ExtractVOL(args[0]);
                return;
            }
            else
            {
                PrintUsage();
                return;
            }
        }

        static void RebuildVOL(string volFile, string inputDirectory, string newVolFile)
        {
            int counter = 0;
            string[] files = Directory.GetFiles(inputDirectory, "*", SearchOption.TopDirectoryOnly);
            if(files.Length == 0)
            {
                Console.WriteLine("Nothing for import in {0}/", inputDirectory);
                Console.ReadKey();
            }
            List<VOLFileEntry> entries = new List<VOLFileEntry>();
            VOLBase vBase = new VOLBase();
            VOLHeader header;
            vBase.VolFileName = volFile;
            vBase.Read();
            using (BinaryReader reader = new BinaryReader(new MemoryStream(vBase.DataBuffer)))
            {
                header = new VOLHeader(reader);
                for (int i = 0; i < header.NumOfFiles; i++)
                    entries.Add(new VOLFileEntry(reader));
            }
            using(MemoryStream ms = new MemoryStream())
            using(BinaryWriter writer = new BinaryWriter(ms))
            {
                // go to data block
                uint dataStart = Utils.GetAlignment((uint)(header.InfoTableStartOffset + (header.NumOfFiles * 0x14) + 0x14));
                writer.BaseStream.Position = dataStart;
                // writing data and update new offset + size
                for (int i = 0; i < entries.Count; i++)
                {
                    VOLFileEntry fileEntry = entries[i];
                    string importedFilePath = inputDirectory + Path.DirectorySeparatorChar + fileEntry.Name;
                    if (!File.Exists(importedFilePath))
                    {
                        Console.WriteLine("File {0} not found in directory {1}. Process will be aborted. Press any key to exit...", fileEntry.Name, inputDirectory);
                        Console.ReadKey();
                        return;
                    }
                    byte[] fileData = File.ReadAllBytes(importedFilePath);
                    fileEntry.Offset = (uint)writer.BaseStream.Position;
                    fileEntry.Size = (uint)fileData.Length;
                    writer.Write(fileData);
                    Utils.AlignPosition(writer);
                    entries[i] = fileEntry;
                    Console.Write("\rImported {0}/{1} files", counter++, entries.Count);
                }
                // writing names and update nameoffset
                for (int i = 0; i < entries.Count; i++)
                {
                    VOLFileEntry fileEntry = entries[i];
                    fileEntry.NameOffset = (uint)writer.BaseStream.Position;
                    writer.Write(Encoding.ASCII.GetBytes(fileEntry.Name));
                    writer.Write(new byte()); // null-terminator
                    entries[i] = fileEntry;
                }
                // writing header
                writer.BaseStream.Position = 0;
                header.Write(writer);
                // writing entries
                foreach(var entry in entries)
                    entry.Write(writer);
                // set new buf
                vBase.DataBuffer = ms.ToArray();
            }
            vBase.Write(newVolFile);
        }

        static void ExtractVOL(string volFile)
        {
            VOLBase vBase = new VOLBase();
            vBase.VolFileName = volFile;
            vBase.Read();
            using (BinaryReader reader = new BinaryReader(new MemoryStream(vBase.DataBuffer)))
            {
                VOLHeader header = new VOLHeader(reader);
                List<VOLFileEntry> entries = new List<VOLFileEntry>();
                for (int i = 0; i < header.NumOfFiles; i++)
                    entries.Add(new VOLFileEntry(reader));
                string outputDir = vBase.VolFileName.Split('.')[0];
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                foreach (var entry in entries)
                {
                    Console.WriteLine("Extracting: {0}", entry.Name);
                    File.WriteAllBytes($"{outputDir}\\{entry.Name}", entry.GetBytes(reader));
                }
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("VOL Archive Unpacker/Packer by LinkOFF");
            Console.WriteLine("Usage: volTool.exe <argument> <VOLFile> [...] [...]");
            Console.WriteLine("Arguments:");
            Console.WriteLine("  extract \t Extracts all content of the archive");
            Console.WriteLine("  rebuild \t Import files from input folder to the new archive");
            Console.WriteLine("Examples:");
            Console.WriteLine("  volTool.exe extract global.vol");
            Console.WriteLine("  volTool.exe rebuild global.vol global_directory new_global.vol");
        }
    }
}
