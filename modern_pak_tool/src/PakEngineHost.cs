using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

internal static class PakEngineHost
{
    private const uint PackMagic = 0x4B434150;
    private const int HeaderSize = 32;
    private const uint HashModulus = 0x8000000B;
    private const uint HashXor = 0x12345678;
    private const uint SizeMask = 0x07FFFFFF;
    private const uint FlagMask = 0xF8000000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CreatePackFileShellDelegate();

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length < 1)
                return Fail(2, "Usage: PakEngineHost version | probe [runtime] | packfolder [runtime] <folder> <output.pak> | packfolderlegacy [runtime] <folder> <output.pak> | unpackfolder <pak> <sidecar.txt> <outputFolder>");

            string mode = args[0].ToLowerInvariant();
            if (mode == "version")
            {
                Console.WriteLine("KiroHostVersion=0.1.3");
                return 0;
            }

            if (mode == "probe")
                return Probe();

            if (mode == "packfolder")
            {
                if (args.Length == 3)
                    return PackFolder(args[1], args[2]);
                if (args.Length == 4)
                    return PackFolder(args[2], args[3]);
                return Fail(2, "Usage: PakEngineHost packfolder [runtime] <folder> <output.pak>");
            }

            if (mode == "packfolderlegacy")
            {
                if (args.Length == 3)
                    return PackFolderLegacy(args[1], args[2]);
                if (args.Length == 4)
                    return PackFolderLegacy(args[2], args[3]);
                return Fail(2, "Usage: PakEngineHost packfolderlegacy [runtime] <folder> <output.pak>");
            }

            if (mode == "unpackfolder")
            {
                if (args.Length != 4)
                    return Fail(2, "Usage: PakEngineHost unpackfolder <pak> <sidecar.txt> <outputFolder>");
                return UnpackFolder(args[1], args[2], args[3]);
            }

            return Fail(2, "Unknown mode: " + args[0]);
        }
        catch (Exception ex)
        {
            return Fail(1, ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static int Probe()
    {
        IntPtr engine = LoadEngine();
        RequireProc(engine, "CreatePackFileShell");
        RequireProc(engine, "g_LoadPackageFiles");
        RequireProc(engine, "g_ClearPackageFiles");
        Console.WriteLine("OK: legacy engine loaded");
        return 0;
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void SetRootPathDelegate(
        IntPtr self,
        [MarshalAs(UnmanagedType.LPStr)] string rootPath);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int OpenPackageDelegate(
        IntPtr self,
        [MarshalAs(UnmanagedType.LPStr)] string packageFile,
        int openExisting,
        int reserved);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void ClosePackageDelegate(IntPtr self, int packageIndex);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate byte AddFileDelegate(
        IntPtr self,
        int packageIndex,
        [MarshalAs(UnmanagedType.LPStr)] string relativeFile);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate byte ExtractAllDelegate(
        IntPtr self,
        int packageIndex,
        ref int extractedCount,
        [MarshalAs(UnmanagedType.LPStr)] string outputFolder,
        [MarshalAs(UnmanagedType.LPStr)] string pathPrefix);

    private static int PackFolder(string folderPath, string outputPakPath)
    {
        folderPath = Path.GetFullPath(folderPath);
        outputPakPath = Path.GetFullPath(outputPakPath);

        if (!Directory.Exists(folderPath))
            return Fail(3, "Folder not found: " + folderPath);

        string parent = Path.GetDirectoryName(outputPakPath);
        if (!Directory.Exists(parent))
            Directory.CreateDirectory(parent);

        string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        if (files.Length == 0)
            return Fail(5, "Folder has no files to pack: " + folderPath);

        PackEntry[] entries = BuildPackEntries(folderPath, files);
        WritePackManaged(outputPakPath, entries);
        ValidatePackOutput(outputPakPath, entries);
        Console.WriteLine("PackedFiles=" + files.Length.ToString());
        Console.WriteLine("OutputPak=" + outputPakPath);
        Console.WriteLine("OutputSidecar=" + outputPakPath + ".txt");
        Console.WriteLine("PackValidation=Success");
        Console.WriteLine("PackResult=Success");
        return File.Exists(outputPakPath) ? 0 : 23;
    }

    private static int PackFolderLegacy(string folderPath, string outputPakPath)
    {
        folderPath = Path.GetFullPath(folderPath);
        outputPakPath = Path.GetFullPath(outputPakPath);

        if (!Directory.Exists(folderPath))
            return Fail(3, "Folder not found: " + folderPath);

        string parent = Path.GetDirectoryName(outputPakPath);
        if (!Directory.Exists(parent))
            Directory.CreateDirectory(parent);

        string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        if (files.Length == 0)
            return Fail(5, "Folder has no files to pack: " + folderPath);

        IntPtr engine = LoadEngine();
        IntPtr proc = RequireProc(engine, "CreatePackFileShell");
        CreatePackFileShellDelegate create = (CreatePackFileShellDelegate)Marshal.GetDelegateForFunctionPointer(
            proc, typeof(CreatePackFileShellDelegate));

        IntPtr shell = new IntPtr(create());
        if (shell == IntPtr.Zero)
            return Fail(20, "CreatePackFileShell returned null.");

        SetRootPathDelegate setRoot = GetVTableDelegate<SetRootPathDelegate>(shell, 1);
        OpenPackageDelegate openPackage = GetVTableDelegate<OpenPackageDelegate>(shell, 2);
        ClosePackageDelegate closePackage = GetVTableDelegate<ClosePackageDelegate>(shell, 3);
        AddFileDelegate addFile = GetVTableDelegate<AddFileDelegate>(shell, 6);

        setRoot(shell, AppendSlash(folderPath));
        int packageIndex = openPackage(shell, outputPakPath, 0, 0);
        if (packageIndex < 0)
            return Fail(21, "The legacy pack engine could not create the output PAK.");

        int added = 0;
        try
        {
            for (int i = 0; i < files.Length; i++)
            {
                string relative = MakeRelative(folderPath, files[i]).Replace('/', '\\');
                byte ok = addFile(shell, packageIndex, relative);
                if (ok == 0)
                    return Fail(22, "The legacy pack engine could not add file: " + relative);
                added++;
            }
        }
        finally
        {
            closePackage(shell, packageIndex);
        }

        Console.WriteLine("PackedFiles=" + added.ToString());
        Console.WriteLine("OutputPak=" + outputPakPath);
        Console.WriteLine("OutputSidecar=" + outputPakPath + ".txt");
        Console.WriteLine("PackMode=LegacyEngine");
        Console.WriteLine("PackResult=Success");
        return File.Exists(outputPakPath) ? 0 : 23;
    }

    private static PackEntry[] BuildPackEntries(string folderPath, string[] files)
    {
        PackEntry[] entries = new PackEntry[files.Length];
        Dictionary<uint, string> seenHashes = new Dictionary<uint, string>();
        long offset = HeaderSize;

        for (int i = 0; i < files.Length; i++)
        {
            string relative = MakeRelative(folderPath, files[i]).Replace('/', '\\');
            string virtualPath = "\\" + relative;
            byte[] encodedPath = EncodeGamePathStrict(virtualPath);
            uint hash = Jx2FileNameHash(encodedPath);
            string existingPath;
            if (seenHashes.TryGetValue(hash, out existingPath))
            {
                throw new InvalidOperationException("Duplicate JX2 archive ID " + hash.ToString("x8") + " for " + existingPath + " and " + virtualPath + ". Rename or remove one file before packing.");
            }
            seenHashes.Add(hash, virtualPath);

            byte[] data = File.ReadAllBytes(files[i]);
            uint fileCrc = Crc32(data);

            entries[i] = new PackEntry();
            entries[i].Path = relative;
            entries[i].Hash = hash;
            entries[i].Offset = checked((uint)offset);
            entries[i].Size = checked((uint)data.Length);
            entries[i].PackedSizeAndFlags = checked((uint)data.Length);
            entries[i].Crc = fileCrc;
            entries[i].Time = File.GetLastWriteTime(files[i]);
            entries[i].Data = data;

            offset = checked(offset + data.Length);
        }

        Array.Sort(entries, ComparePackEntries);
        offset = HeaderSize;
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i].Offset = checked((uint)offset);
            offset = checked(offset + entries[i].Data.Length);
        }

        return entries;
    }

    private static int ComparePackEntries(PackEntry left, PackEntry right)
    {
        int hashCompare = left.Hash.CompareTo(right.Hash);
        if (hashCompare != 0)
            return hashCompare;
        return String.Compare(left.Path, right.Path, StringComparison.OrdinalIgnoreCase);
    }

    private static void WritePackManaged(string outputPakPath, PackEntry[] entries)
    {
        uint packageTime = ToUnixTime(DateTime.Now);
        uint indexOffset = GetIndexOffset(entries);

        byte[] table = BuildIndexTable(entries);
        uint packageCrc = Crc32(table);

        using (FileStream stream = new FileStream(outputPakPath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(PackMagic);
            writer.Write(entries.Length);
            writer.Write(indexOffset);
            writer.Write((uint)HeaderSize);
            writer.Write(packageCrc);
            writer.Write(packageTime);
            writer.Write(0);
            writer.Write(0);

            for (int i = 0; i < entries.Length; i++)
                writer.Write(entries[i].Data);

            writer.Write(table);
        }

        WriteSidecar(outputPakPath + ".txt", entries, packageTime, packageCrc);
    }

    private static uint GetIndexOffset(PackEntry[] entries)
    {
        if (entries.Length == 0)
            return HeaderSize;

        PackEntry last = entries[entries.Length - 1];
        return checked(last.Offset + last.Size);
    }

    private static void ValidatePackOutput(string outputPakPath, PackEntry[] entries)
    {
        string sidecarPath = outputPakPath + ".txt";
        if (!File.Exists(outputPakPath))
            throw new InvalidOperationException("Managed pack writer did not produce a PAK file.");
        if (!File.Exists(sidecarPath))
            throw new InvalidOperationException("Managed pack writer did not produce a sidecar manifest.");

        byte[] expectedTable = BuildIndexTable(entries);
        using (FileStream stream = File.OpenRead(outputPakPath))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            if (stream.Length < HeaderSize)
                throw new InvalidOperationException("Generated PAK is too small to contain a valid header.");

            uint magic = reader.ReadUInt32();
            int fileCount = reader.ReadInt32();
            uint indexOffset = reader.ReadUInt32();
            uint dataOffset = reader.ReadUInt32();
            uint packageCrc = reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();

            if (magic != PackMagic)
                throw new InvalidOperationException("Generated PAK header magic is not PACK.");
            if (fileCount != entries.Length)
                throw new InvalidOperationException("Generated PAK file count " + fileCount.ToString() + " does not match source file count " + entries.Length.ToString() + ".");
            if (dataOffset != HeaderSize)
                throw new InvalidOperationException("Generated PAK data offset is not the expected header size.");
            if (indexOffset + expectedTable.Length > stream.Length)
                throw new InvalidOperationException("Generated PAK index table points outside the archive.");
            if (packageCrc != Crc32(expectedTable))
                throw new InvalidOperationException("Generated PAK header CRC does not match the generated index table.");

            stream.Position = indexOffset;
            byte[] actualTable = reader.ReadBytes(expectedTable.Length);
            if (!BytesEqual(actualTable, expectedTable))
                throw new InvalidOperationException("Generated PAK index table does not match the planned manifest.");

            for (int i = 0; i < entries.Length; i++)
            {
                uint packedSize = entries[i].PackedSizeAndFlags & SizeMask;
                uint flags = entries[i].PackedSizeAndFlags & FlagMask;
                if (flags != 0)
                    throw new InvalidOperationException("Generated entry " + entries[i].Path + " unexpectedly has compression flags.");
                if (packedSize != entries[i].Size)
                    throw new InvalidOperationException("Generated entry " + entries[i].Path + " has mismatched packed and unpacked sizes.");
                if (entries[i].Offset < HeaderSize || entries[i].Offset + packedSize > indexOffset)
                    throw new InvalidOperationException("Generated entry " + entries[i].Path + " points outside the payload region.");
            }
        }

        ValidateSidecar(sidecarPath, entries);
    }

    private static void ValidateSidecar(string sidecarPath, PackEntry[] entries)
    {
        using (StreamReader reader = new StreamReader(sidecarPath, GetGameEncoding()))
        {
            string first = reader.ReadLine();
            if (first == null || !first.StartsWith("TotalFile:" + entries.Length.ToString(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Generated sidecar TotalFile header does not match the source file count.");

            string header = reader.ReadLine();
            if (!String.Equals(header, "Index\tID\tTime\tFileName\tSize\tInPakSize\tComprFlag\tCRC", StringComparison.Ordinal))
                throw new InvalidOperationException("Generated sidecar column header is not the expected PAK list format.");

            for (int i = 0; i < entries.Length; i++)
            {
                string line = reader.ReadLine();
                if (line == null)
                    throw new InvalidOperationException("Generated sidecar ended before entry " + i.ToString() + ".");

                string[] parts = line.Split('\t');
                if (parts.Length != 8)
                    throw new InvalidOperationException("Generated sidecar entry " + i.ToString() + " has " + parts.Length.ToString() + " columns instead of 8.");

                string expectedId = entries[i].Hash.ToString("x");
                if (!String.Equals(parts[0], i.ToString(), StringComparison.Ordinal) ||
                    !String.Equals(parts[1], expectedId, StringComparison.OrdinalIgnoreCase) ||
                    !String.Equals(parts[3], "\\" + entries[i].Path, StringComparison.Ordinal) ||
                    !String.Equals(parts[4], entries[i].Size.ToString(), StringComparison.Ordinal) ||
                    !String.Equals(parts[5], entries[i].Size.ToString(), StringComparison.Ordinal) ||
                    !String.Equals(parts[6], "0", StringComparison.Ordinal) ||
                    !String.Equals(parts[7], entries[i].Crc.ToString("x"), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Generated sidecar entry " + i.ToString() + " does not match the planned manifest.");
                }
            }

            if (reader.ReadLine() != null)
                throw new InvalidOperationException("Generated sidecar has more entries than the source manifest.");
        }
    }

    private static byte[] BuildIndexTable(PackEntry[] entries)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            for (int i = 0; i < entries.Length; i++)
            {
                writer.Write(entries[i].Hash);
                writer.Write(entries[i].Offset);
                writer.Write(entries[i].Size);
                writer.Write(entries[i].PackedSizeAndFlags);
            }

            return stream.ToArray();
        }
    }

    private static void WriteSidecar(string sidecarPath, PackEntry[] entries, uint packageTime, uint packageCrc)
    {
        using (StreamWriter writer = new StreamWriter(sidecarPath, false, GetGameEncoding()))
        {
            DateTime pakTime = FromUnixTime(packageTime);
            writer.WriteLine("TotalFile:" + entries.Length.ToString() +
                "\tPakTime:" + FormatTime(pakTime) +
                "\tPakTimeSave:" + packageTime.ToString("x") +
                "\tCRC:" + packageCrc.ToString("x"));
            writer.WriteLine("Index\tID\tTime\tFileName\tSize\tInPakSize\tComprFlag\tCRC");

            for (int i = 0; i < entries.Length; i++)
            {
                writer.WriteLine(i.ToString() + "\t" +
                    entries[i].Hash.ToString("x") + "\t" +
                    FormatTime(entries[i].Time) + "\t" +
                    "\\" + entries[i].Path + "\t" +
                    entries[i].Size.ToString() + "\t" +
                    entries[i].Size.ToString() + "\t0\t" +
                    entries[i].Crc.ToString("x"));
            }
        }
    }

    private static uint Jx2FileNameHash(byte[] bytes)
    {
        uint value = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            int b = bytes[i];
            if (b >= 0x41 && b <= 0x5A)
                b += 0x20;
            else if (b == 0x2F)
                b = 0x5C;

            if (b >= 0x80)
                b -= 0x100;

            long next = ((long)value + (long)b * (i + 1)) % HashModulus;
            if (next < 0)
                next += HashModulus;
            value = unchecked((uint)((long)next * -17L));
        }

        return value ^ HashXor;
    }

    private static byte[] EncodeGamePathStrict(string path)
    {
        try
        {
            return Encoding.GetEncoding(936, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(path);
        }
        catch (EncoderFallbackException ex)
        {
            throw new InvalidOperationException("Path cannot be represented in the JX2 GBK code page: " + path + ". " + ex.Message);
        }
    }

    private static Encoding GetGameEncoding()
    {
        return Encoding.GetEncoding(936);
    }

    private static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;
        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
                return false;
        }
        return true;
    }

    private static uint Crc32(byte[] data)
    {
        return Crc32Update(0, data);
    }

    private static uint Crc32Update(uint previous, byte[] data)
    {
        uint crc = previous ^ 0xFFFFFFFF;
        for (int i = 0; i < data.Length; i++)
        {
            crc ^= data[i];
            for (int bit = 0; bit < 8; bit++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ 0xEDB88320;
                else
                    crc >>= 1;
            }
        }

        return crc ^ 0xFFFFFFFF;
    }

    private static uint ToUnixTime(DateTime value)
    {
        DateTime utc = value.ToUniversalTime();
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return checked((uint)(utc - epoch).TotalSeconds);
    }

    private static DateTime FromUnixTime(uint seconds)
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddSeconds(seconds).ToLocalTime();
    }

    private static string FormatTime(DateTime value)
    {
        return value.Year.ToString() + "-" + value.Month.ToString() + "-" + value.Day.ToString() +
            " " + value.Hour.ToString() + ":" + value.Minute.ToString() + ":" + value.Second.ToString();
    }

    private sealed class PackEntry
    {
        public string Path;
        public uint Hash;
        public uint Offset;
        public uint Size;
        public uint PackedSizeAndFlags;
        public uint Crc;
        public DateTime Time;
        public byte[] Data;
    }

    private static int UnpackFolder(string pakPath, string sidecarPath, string outputFolder)
    {
        pakPath = Path.GetFullPath(pakPath);
        sidecarPath = Path.GetFullPath(sidecarPath);
        outputFolder = Path.GetFullPath(outputFolder);

        if (!File.Exists(pakPath))
            return Fail(3, "PAK not found: " + pakPath);
        if (!File.Exists(sidecarPath))
            return Fail(4, "Sidecar manifest not found: " + sidecarPath);

        Directory.CreateDirectory(outputFolder);

        string tempDir = null;
        string enginePakPath = pakPath;
        string requiredSidecarPath = pakPath + ".txt";
        if (!String.Equals(sidecarPath, requiredSidecarPath, StringComparison.OrdinalIgnoreCase))
        {
            tempDir = Path.Combine(Path.GetTempPath(), "modern-pak-tool-unpack-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            enginePakPath = Path.Combine(tempDir, Path.GetFileName(pakPath));
            File.Copy(pakPath, enginePakPath, true);
            File.Copy(sidecarPath, enginePakPath + ".txt", true);
        }

        try
        {
            IntPtr engine = LoadEngine();
            IntPtr proc = RequireProc(engine, "CreatePackFileShell");
            CreatePackFileShellDelegate create = (CreatePackFileShellDelegate)Marshal.GetDelegateForFunctionPointer(
                proc, typeof(CreatePackFileShellDelegate));

            IntPtr shell = new IntPtr(create());
            if (shell == IntPtr.Zero)
                return Fail(20, "CreatePackFileShell returned null.");

            OpenPackageDelegate openPackage = GetVTableDelegate<OpenPackageDelegate>(shell, 2);
            ClosePackageDelegate closePackage = GetVTableDelegate<ClosePackageDelegate>(shell, 3);
            ExtractAllDelegate extractAll = GetVTableDelegate<ExtractAllDelegate>(shell, 13);

            int packageIndex = openPackage(shell, enginePakPath, 1, 0);
            if (packageIndex < 0)
                return Fail(24, "The legacy pack engine could not open the PAK and sidecar manifest.");

            int extracted = 0;
            byte ok = extractAll(shell, packageIndex, ref extracted, outputFolder, "");
            closePackage(shell, packageIndex);
            if (ok == 0)
                return Fail(25, "The legacy pack engine could not extract the PAK.");

            Console.WriteLine("Unpacked " + extracted.ToString() + " files.");
            return 0;
        }
        finally
        {
            if (tempDir != null)
                TryDelete(tempDir);
        }
    }

    private static T GetVTableDelegate<T>(IntPtr instance, int slot)
    {
        int vtable = Marshal.ReadInt32(instance);
        int address = Marshal.ReadInt32(new IntPtr(vtable + slot * 4));
        return (T)(object)Marshal.GetDelegateForFunctionPointer(new IntPtr(address), typeof(T));
    }

    private static string MakeRelative(string root, string file)
    {
        Uri rootUri = new Uri(AppendSlash(Path.GetFullPath(root)));
        Uri fileUri = new Uri(Path.GetFullPath(file));
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString());
    }

    private static string AppendSlash(string path)
    {
        if (path.EndsWith("\\") || path.EndsWith("/"))
            return path;
        return path + Path.DirectorySeparatorChar;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {
        }
    }

    private static IntPtr LoadEngine()
    {
        string enginePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine.dll");
        IntPtr engine = LoadLibrary(enginePath);
        if (engine == IntPtr.Zero)
            throw new InvalidOperationException("Unable to load engine.dll. Win32 error " + Marshal.GetLastWin32Error().ToString());
        return engine;
    }

    private static IntPtr RequireProc(IntPtr module, string name)
    {
        IntPtr proc = GetProcAddress(module, name);
        if (proc == IntPtr.Zero)
            throw new MissingMethodException("Missing export: " + name);
        return proc;
    }

    private static int Fail(int code, string message)
    {
        Console.Error.WriteLine(message);
        return code;
    }
}
