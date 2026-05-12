using System;
using System.IO;
using System.Runtime.InteropServices;

internal static class PakEngineHost
{
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
                return Fail(2, "Usage: PakEngineHost packfolder <folder> <output.pak> | unpackfolder <pak> <sidecar.txt> <outputFolder> | probe");

            string mode = args[0].ToLowerInvariant();
            if (mode == "probe")
                return Probe();

            if (mode == "packfolder")
            {
                if (args.Length != 3)
                    return Fail(2, "Usage: PakEngineHost packfolder <folder> <output.pak>");
                return PackFolder(args[1], args[2]);
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
        for (int i = 0; i < files.Length; i++)
        {
            string relative = MakeRelative(folderPath, files[i]).Replace('/', '\\');
            byte ok = addFile(shell, packageIndex, relative);
            if (ok == 0)
                return Fail(22, "The legacy pack engine could not add file: " + relative);
            added++;
        }

        closePackage(shell, packageIndex);
        Console.WriteLine("Packed " + added.ToString() + " files.");
        return File.Exists(outputPakPath) ? 0 : 23;
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
