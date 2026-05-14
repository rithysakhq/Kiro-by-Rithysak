using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

internal sealed class TypeGuess
{
    public string Extension;
    public string DetectedType;
    public string Confidence;
    public string Reason;
}

internal sealed class InventoryEntry
{
    public string Status { get; set; }
    public string ArchiveId { get; set; }
    public string Index { get; set; }
    public string RelativePath { get; set; }
    public string OriginalPathKnown { get; set; }
    public string Extension { get; set; }
    public string DetectedType { get; set; }
    public string Confidence { get; set; }
    public string Reason { get; set; }
    public string EditableHint { get; set; }
    public string RepackSafety { get; set; }
    public long SizeBytes { get; set; }
}

internal sealed class InventoryResult
{
    public string RootPath;
    public List<InventoryEntry> Entries;
    public int ExactNameCount;
    public int TypedUnknownCount;
    public int UnknownBinaryCount;
    public int ToolReportCount;
    public int SkippedCount;
    public long TotalBytes;

    public InventoryResult()
    {
        Entries = new List<InventoryEntry>();
    }

    public int TotalPayloadFiles
    {
        get { return ExactNameCount + TypedUnknownCount + UnknownBinaryCount; }
    }

    public string BuildShortSummary()
    {
        return "Recovered Names: " + ExactNameCount.ToString() +
            " | Typed Unknowns: " + TypedUnknownCount.ToString() +
            " | Unknown Binaries: " + UnknownBinaryCount.ToString() +
            " | Tool Reports: " + ToolReportCount.ToString();
    }
}

internal static class InventoryScanner
{
    public static InventoryResult Scan(string outputFolder)
    {
        InventoryResult result = new InventoryResult();
        result.RootPath = outputFolder;

        if (String.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
            return result;

        try
        {
            foreach (string path in Directory.EnumerateFiles(outputFolder, "*", SearchOption.AllDirectories))
            {
                InventoryEntry entry = BuildEntry(outputFolder, path);
                result.Entries.Add(entry);
                result.TotalBytes += entry.SizeBytes;

                if (entry.Status == "ExactName")
                    result.ExactNameCount++;
                else if (entry.Status == "TypedUnknown")
                    result.TypedUnknownCount++;
                else if (entry.Status == "UnknownBinary")
                    result.UnknownBinaryCount++;
                else if (entry.Status == "ToolReport")
                    result.ToolReportCount++;
                else if (entry.Status == "Skipped")
                    result.SkippedCount++;
            }
        }
        catch
        {
        }

        result.Entries.Sort(CompareEntries);
        return result;
    }

    private static InventoryEntry BuildEntry(string root, string path)
    {
        InventoryEntry entry = new InventoryEntry();
        entry.RelativePath = MakeRelative(root, path);
        entry.Extension = Path.GetExtension(path);
        entry.ArchiveId = "";
        entry.Index = "";
        entry.OriginalPathKnown = "false";
        entry.SizeBytes = SafeLength(path);

        string fileName = Path.GetFileName(path);
        bool toolReport = IsToolReport(fileName);
        string archiveId;
        string index;
        bool idNamed = IsUnknownIdPath(entry.RelativePath, fileName, out archiveId, out index);
        entry.ArchiveId = archiveId;
        entry.Index = index;
        TypeGuess guess = GuessFile(path, entry.Extension);

        entry.Extension = String.IsNullOrEmpty(entry.Extension) ? guess.Extension : entry.Extension;
        entry.DetectedType = guess.DetectedType;
        entry.Confidence = guess.Confidence;
        entry.EditableHint = EditableHint(guess.DetectedType);

        if (toolReport)
        {
            entry.Status = "ToolReport";
            entry.OriginalPathKnown = "false";
            entry.RepackSafety = "unsafe";
            entry.Reason = "Tool-generated report, not game payload.";
            entry.EditableHint = "text";
            return entry;
        }

        if (idNamed)
        {
            entry.OriginalPathKnown = "false";
            entry.Status = IsKnownPayloadType(guess.DetectedType) ? "TypedUnknown" : "UnknownBinary";
            entry.RepackSafety = "uncertain";
            entry.Reason = guess.Reason + " Original path is missing; this does not mean the file is corrupted.";
            return entry;
        }

        entry.Status = "ExactName";
        entry.OriginalPathKnown = "true";
        entry.Confidence = "exact";
        entry.RepackSafety = "safe";
        entry.Reason = "Original path is present in the extracted folder. " + guess.Reason;
        return entry;
    }

    private static TypeGuess GuessFile(string path, string extension)
    {
        FileTypeGuess raw = FallbackNameRecovery.Guess(path);
        string guessedExtension = raw.Extension;
        string detectedType = TypeFromExtension(!String.IsNullOrEmpty(guessedExtension) ? guessedExtension : extension);

        TypeGuess guess = new TypeGuess();
        guess.Extension = !String.IsNullOrEmpty(guessedExtension) ? guessedExtension : extension;
        guess.DetectedType = detectedType;
        guess.Confidence = Confidence(raw.Confidence, detectedType);
        guess.Reason = String.IsNullOrWhiteSpace(raw.Reason) ? "Type inferred from file extension." : raw.Reason;

        if (detectedType == "unknown" && !String.IsNullOrEmpty(extension))
        {
            guess.DetectedType = TypeFromExtension(extension);
            if (guess.DetectedType != "unknown")
            {
                guess.Confidence = "medium";
                guess.Reason = "Type inferred from current file extension.";
            }
        }

        return guess;
    }

    private static string Confidence(int value, string detectedType)
    {
        if (detectedType == "unknown")
            return "low";
        if (value >= 90)
            return "high";
        if (value >= 70)
            return "medium";
        return "low";
    }

    private static bool IsKnownPayloadType(string detectedType)
    {
        return detectedType != "unknown" && detectedType != "binary";
    }

    private static string TypeFromExtension(string extension)
    {
        if (String.IsNullOrEmpty(extension))
            return "unknown";

        string value = extension.TrimStart('.').ToLowerInvariant();
        if (value == "ini")
            return "ini";
        if (value == "txt")
            return "txt";
        if (value == "lua")
            return "lua";
        if (value == "xml")
            return "xml";
        if (value == "spr")
            return "spr";
        if (value == "asf")
            return "asf";
        if (value == "png" || value == "jpg" || value == "jpeg" || value == "gif" || value == "bmp" || value == "dds")
            return "image";
        if (value == "ogg" || value == "wav" || value == "mp3")
            return "audio";
        if (value == "zip")
            return "archive";
        if (value == "bin")
            return "binary";
        return "unknown";
    }

    private static string EditableHint(string detectedType)
    {
        if (detectedType == "ini" || detectedType == "txt" || detectedType == "lua" || detectedType == "xml")
            return "text";
        if (detectedType == "spr" || detectedType == "asf")
            return "sprite";
        if (detectedType == "unknown")
            return "unknown";
        return "binary";
    }

    private static bool IsToolReport(string fileName)
    {
        return String.Equals(fileName, "_pak_tool_inventory.tsv", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(fileName, "_pak_tool_recovery_summary.txt", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(fileName, "_pak_tool_name_recovery_report.txt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnknownIdPath(string relativePath, string fileName, out string archiveId, out string index)
    {
        archiveId = "";
        index = "";

        string normalized = relativePath.Replace('/', '\\');
        if (normalized.StartsWith("_unknown_by_id\\", StringComparison.OrdinalIgnoreCase))
        {
            TryParseUnknownIdName(fileName, out archiveId, out index);
            return true;
        }

        if (GeneratedIdNames.IsGeneratedIdFileName(fileName))
        {
            archiveId = Path.GetFileNameWithoutExtension(fileName);
            if (archiveId.StartsWith("_ID_", StringComparison.OrdinalIgnoreCase))
                archiveId = archiveId.Substring(4);
            else if (archiveId.StartsWith("_-ID-_", StringComparison.OrdinalIgnoreCase))
                archiveId = archiveId.Substring(6);
            archiveId = archiveId.ToUpperInvariant();
            return true;
        }

        return TryParseUnknownIdName(fileName, out archiveId, out index);
    }

    private static bool TryParseUnknownIdName(string fileName, out string archiveId, out string index)
    {
        archiveId = "";
        index = "";
        string stem = Path.GetFileNameWithoutExtension(fileName);
        if (String.IsNullOrEmpty(stem) || stem.Length < 14)
            return false;

        int underscore = stem.IndexOf('_');
        if (underscore != 8)
            return false;

        string id = stem.Substring(0, 8);
        string idx = stem.Substring(9);
        if (!IsHex(id) || !IsDigits(idx))
            return false;

        archiveId = id.ToUpperInvariant();
        index = idx;
        return true;
    }

    private static bool IsHex(string value)
    {
        if (value.Length == 0)
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool ok = (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');
            if (!ok)
                return false;
        }

        return true;
    }

    private static bool IsDigits(string value)
    {
        if (value.Length == 0)
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] < '0' || value[i] > '9')
                return false;
        }

        return true;
    }

    private static long SafeLength(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch
        {
            return 0;
        }
    }

    private static int CompareEntries(InventoryEntry left, InventoryEntry right)
    {
        int status = String.Compare(left.Status, right.Status, StringComparison.OrdinalIgnoreCase);
        if (status != 0)
            return status;
        return String.Compare(left.RelativePath, right.RelativePath, StringComparison.OrdinalIgnoreCase);
    }

    private static string MakeRelative(string root, string path)
    {
        try
        {
            Uri rootUri = new Uri(AppendSlash(Path.GetFullPath(root)));
            Uri pathUri = new Uri(Path.GetFullPath(path));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', '\\');
        }
        catch
        {
            return Path.GetFileName(path);
        }
    }

    private static string AppendSlash(string path)
    {
        if (path.EndsWith("\\", StringComparison.Ordinal) || path.EndsWith("/", StringComparison.Ordinal))
            return path;
        return path + Path.DirectorySeparatorChar;
    }
}

internal static class ReportWriter
{
    public static string WriteInventory(string outputFolder, InventoryResult inventory)
    {
        string path = Path.Combine(outputFolder, "_pak_tool_inventory.tsv");
        using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
        {
            writer.WriteLine("Status\tArchiveId\tIndex\tRelativePath\tOriginalPathKnown\tExtension\tDetectedType\tConfidence\tReason\tEditableHint\tRepackSafety\tSizeBytes");
            for (int i = 0; i < inventory.Entries.Count; i++)
            {
                InventoryEntry entry = inventory.Entries[i];
                writer.WriteLine(Clean(entry.Status) + "\t" +
                    Clean(entry.ArchiveId) + "\t" +
                    Clean(entry.Index) + "\t" +
                    Clean(entry.RelativePath) + "\t" +
                    Clean(entry.OriginalPathKnown) + "\t" +
                    Clean(entry.Extension) + "\t" +
                    Clean(entry.DetectedType) + "\t" +
                    Clean(entry.Confidence) + "\t" +
                    Clean(entry.Reason) + "\t" +
                    Clean(entry.EditableHint) + "\t" +
                    Clean(entry.RepackSafety) + "\t" +
                    entry.SizeBytes.ToString());
            }
        }

        return path;
    }

    public static string WriteRecoverySummary(string outputFolder, string sourcePak, string manifestStatus, string manifestPath, OperationResult operation, InventoryResult inventory)
    {
        string path = Path.Combine(outputFolder, "_pak_tool_recovery_summary.txt");
        using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
        {
            writer.WriteLine("Kiro by Rithysak recovery summary");
            writer.WriteLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteLine("Source PAK: " + sourcePak);
            writer.WriteLine("Output folder: " + outputFolder);
            writer.WriteLine("Manifest status: " + manifestStatus);
            writer.WriteLine("Manifest path: " + manifestPath);
            writer.WriteLine();
            writer.WriteLine("Total archive entries: " + operation.FileCount.ToString());
            writer.WriteLine("Recovered Names: " + inventory.ExactNameCount.ToString());
            writer.WriteLine("Typed Unknowns: " + inventory.TypedUnknownCount.ToString());
            writer.WriteLine("Unknown Binaries: " + inventory.UnknownBinaryCount.ToString());
            writer.WriteLine("Tool Reports: " + inventory.ToolReportCount.ToString());
            writer.WriteLine();
            writer.WriteLine("Meaning:");
            writer.WriteLine("Recovered Names have usable extracted paths and are the safest normal repack candidates.");
            writer.WriteLine("Typed Unknowns were extracted successfully and their content type was inferred, but the original path is missing.");
            writer.WriteLine("Unknown Binaries were extracted successfully, but the app cannot identify their format confidently.");
            writer.WriteLine("Unknown does not mean corrupted.");
            writer.WriteLine();
            if (!String.IsNullOrWhiteSpace(operation.RecoverySummary))
                writer.WriteLine("Name recovery: " + operation.RecoverySummary);
            if (!String.IsNullOrWhiteSpace(operation.RecoveryReportPath))
                writer.WriteLine("Recovery report: " + operation.RecoveryReportPath);
            if (!String.IsNullOrWhiteSpace(operation.InventoryReportPath))
                writer.WriteLine("Inventory: " + operation.InventoryReportPath);
        }

        return path;
    }

    private static string Clean(string value)
    {
        if (value == null)
            return "";
        return value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }
}

internal static class InventoryNarrator
{
    public static string Describe(InventoryEntry entry)
    {
        if (entry == null)
            return "No file selected.";

        string text = "What happened\r\n";
        if (entry.Status == "ExactName")
            text += "This file was extracted with a usable path.\r\n";
        else if (entry.Status == "TypedUnknown")
            text += "This file was extracted successfully, but its original path was not recovered.\r\n";
        else if (entry.Status == "UnknownBinary")
            text += "This file was extracted successfully, but the app cannot identify its format confidently.\r\n";
        else if (entry.Status == "ToolReport")
            text += "This is a report generated by the tool, not a game payload file.\r\n";
        else
            text += "The scanner could not classify this entry cleanly.\r\n";

        text += "\r\nWhat we know\r\n";
        text += "Status: " + entry.Status + "\r\n";
        text += "Type: " + entry.DetectedType + " (" + entry.Confidence + " confidence)\r\n";
        text += "Reason: " + entry.Reason + "\r\n";
        if (!String.IsNullOrWhiteSpace(entry.ArchiveId))
            text += "Archive ID: " + entry.ArchiveId + "\r\n";

        text += "\r\nRecommended next step\r\n";
        if (entry.Status == "ExactName")
            text += "This is the safest category for normal edit and repack workflows.\r\n";
        else if (entry.Status == "TypedUnknown")
            text += "You can inspect or externally edit the file type if you understand it, but normal repacking may not make the game use it unless the original path is recovered.\r\n";
        else if (entry.Status == "UnknownBinary")
            text += "Treat this as valid but unidentified binary data. Inspect cautiously and do not assume it is useless or corrupted.\r\n";
        else
            text += "Keep this out of game repack workflows.\r\n";

        text += "\r\nPath\r\n" + entry.RelativePath;
        return text;
    }
}
