using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChartForgeX.Raster;

/// <summary>One installed OpenType face (TrueType or CFF outlines), as described by its own name and OS/2 tables.</summary>
internal sealed class InstalledFontFace {
    public InstalledFontFace(string path, int? collectionIndex, string family, string? legacyFamily, int weight, int width, bool italic) {
        Path = path;
        CollectionIndex = collectionIndex;
        Family = family;
        LegacyFamily = legacyFamily;
        Weight = weight;
        Width = width;
        Italic = italic;
    }

    public string Path { get; }
    public int? CollectionIndex { get; }
    /// <summary>The typographic family, which groups every weight ("Segoe UI").</summary>
    public string Family { get; }
    /// <summary>The four-style family when it differs ("Segoe UI Semibold").</summary>
    public string? LegacyFamily { get; }
    public int Weight { get; }
    public int Width { get; }
    public bool Italic { get; }
}

/// <summary>
/// Indexes the OpenType faces (<c>.ttf</c>, <c>.ttc</c>, <c>.otf</c>, <c>.otc</c>) installed on the host by family name, so a family and weight can
/// be matched to a file. The font folders are read once, on the first lookup of a named family;
/// only each file's directory, name, OS/2, and head tables are read. A host with no font
/// folders simply has an empty catalog.
/// </summary>
internal static class InstalledFontCatalog {
    private const int MaximumFiles = 8000;
    private const int MaximumCollectionFaces = 64;
    private const int MaximumNameTableBytes = 1 << 20;
    private static readonly Lazy<Dictionary<string, List<InstalledFontFace>>> Families = new(() => Index(FontDirectories()), true);

    /// <summary>Finds the installed face of <paramref name="family"/> closest to the weight and slant, or null when the family is not installed.</summary>
    internal static InstalledFontFace? Find(string family, int weight, bool italic) => Find(Families.Value, family, weight, italic);

    /// <summary>Finds the face of the same family as the font file at <paramref name="path"/> closest to the weight and slant.</summary>
    internal static InstalledFontFace? FindSibling(string path, int weight, bool italic) {
        var faces = new List<InstalledFontFace>();
        ReadFaces(path, faces);
        return faces.Count == 0 ? null : Find(faces[0].Family, weight, italic);
    }

    internal static InstalledFontFace? Find(Dictionary<string, List<InstalledFontFace>> families, string family, int weight, bool italic) {
        var ranked = Ranked(families, family, weight, italic);
        return ranked.Count == 0 ? null : ranked[0];
    }

    /// <summary>Every installed face of <paramref name="family"/>, closest to the weight and slant first.</summary>
    internal static IReadOnlyList<InstalledFontFace> Ranked(string family, int weight, bool italic) => Ranked(Families.Value, family, weight, italic);

    internal static List<InstalledFontFace> Ranked(Dictionary<string, List<InstalledFontFace>> families, string family, int weight, bool italic) {
        if (string.IsNullOrWhiteSpace(family) || !families.TryGetValue(family.Trim(), out var faces)) return new List<InstalledFontFace>();
        var ranked = new List<InstalledFontFace>(faces);
        // Stable and deterministic: ties go to the lower path.
        ranked.Sort((left, right) => {
            var byScore = Score(left, weight, italic).CompareTo(Score(right, weight, italic));
            if (byScore != 0) return byScore;
            var byPath = string.CompareOrdinal(left.Path, right.Path);
            return byPath != 0 ? byPath : (left.CollectionIndex ?? 0).CompareTo(right.CollectionIndex ?? 0);
        });
        return ranked;
    }

    private static int Score(InstalledFontFace face, int weight, bool italic) =>
        WeightDistance(weight, face.Weight) + Math.Abs(face.Width - 5) * 2000 + (face.Italic == italic ? 0 : 20000);

    /// <summary>Builds the family index for a set of font folders; folders and files that cannot be read are skipped.</summary>
    internal static Dictionary<string, List<InstalledFontFace>> Index(IEnumerable<string> directories) {
        var families = new Dictionary<string, List<InstalledFontFace>>(StringComparer.OrdinalIgnoreCase);
        var files = new List<string>();
        foreach (var directory in directories) AddFontFiles(directory, files, 0);
        files.Sort(StringComparer.OrdinalIgnoreCase);
        var faces = new List<InstalledFontFace>();
        foreach (var file in files) ReadFaces(file, faces);
        foreach (var face in faces) {
            Add(families, face.Family, face);
            if (face.LegacyFamily != null) Add(families, face.LegacyFamily, face);
        }

        return families;
    }

    internal static IEnumerable<string> FontDirectories() {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrEmpty(windows)) yield return Path.Combine(windows, "Fonts");
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(localData)) yield return Path.Combine(localData, "Microsoft", "Windows", "Fonts");
        yield return "/System/Library/Fonts";
        yield return "/Library/Fonts";
        yield return "/usr/share/fonts";
        yield return "/usr/local/share/fonts";
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home)) yield break;
        yield return Path.Combine(home, "Library", "Fonts");
        yield return Path.Combine(home, ".fonts");
        yield return Path.Combine(home, ".local", "share", "fonts");
    }

    // CSS font matching: 400 and 500 stand in for each other first; otherwise lighter requests
    // prefer lighter faces and bolder requests prefer bolder ones.
    private static int WeightDistance(int desired, int actual) {
        if (desired == actual) return 0;
        if ((desired == 400 && actual == 500) || (desired == 500 && actual == 400)) return 50;
        if (desired <= 500) return actual < desired ? desired - actual : 1000 + actual - desired;
        return actual > desired ? actual - desired : 1000 + desired - actual;
    }

    private static void Add(Dictionary<string, List<InstalledFontFace>> families, string family, InstalledFontFace face) {
        if (!families.TryGetValue(family, out var faces)) families[family] = faces = new List<InstalledFontFace>();
        faces.Add(face);
    }

    private static bool IsFontFile(string extension) =>
        extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) || extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".otf", StringComparison.OrdinalIgnoreCase) || extension.Equals(".otc", StringComparison.OrdinalIgnoreCase);

    private static void AddFontFiles(string directory, List<string> files, int depth) {
        if (depth > 6 || files.Count >= MaximumFiles) return;
        try {
            if (!Directory.Exists(directory)) return;
            foreach (var file in Directory.GetFiles(directory)) {
                var extension = Path.GetExtension(file);
                if (!IsFontFile(extension)) continue;
                if (files.Count >= MaximumFiles) return;
                files.Add(file);
            }

            foreach (var child in Directory.GetDirectories(directory)) AddFontFiles(child, files, depth + 1);
        } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException) {
        }
    }

    /// <summary>Reads the faces a font file declares. A file without TrueType or CFF outlines adds nothing.</summary>
    internal static void ReadFaces(string path, List<InstalledFontFace> faces) {
        try {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var header = Read(stream, 0, 12);
            if (header == null) return;
            if (UInt32(header, 0) != 0x74746366) {
                var face = ReadFace(stream, path, 0, null);
                if (face != null) faces.Add(face);
                return;
            }

            var count = (int)Math.Min(MaximumCollectionFaces, UInt32(header, 8));
            var offsets = Read(stream, 12, count * 4);
            if (offsets == null) return;
            for (var index = 0; index < count; index++) {
                var face = ReadFace(stream, path, UInt32(offsets, index * 4), index);
                if (face != null) faces.Add(face);
            }
        } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException) {
        }
    }

    private static InstalledFontFace? ReadFace(FileStream stream, string path, long directoryOffset, int? collectionIndex) {
        var directory = Read(stream, directoryOffset, 12);
        if (directory == null) return null;
        var scaler = UInt32(directory, 0);
        if (scaler != 0x00010000 && scaler != 0x74727565 && scaler != 0x4F54544F) return null;
        var tableCount = UInt16(directory, 4);
        var records = Read(stream, directoryOffset + 12, tableCount * 16);
        if (records == null) return null;
        long nameOffset = -1, os2Offset = -1, headOffset = -1;
        long nameLength = 0, os2Length = 0;
        var hasOutlines = false;
        for (var i = 0; i < tableCount; i++) {
            var record = i * 16;
            var tag = Encoding.ASCII.GetString(records, record, 4);
            var offset = UInt32(records, record + 8);
            var length = UInt32(records, record + 12);
            if (tag == "name") { nameOffset = offset; nameLength = length; }
            else if (tag == "OS/2") { os2Offset = offset; os2Length = length; }
            else if (tag == "head") headOffset = offset;
            else if (tag == "glyf" || tag == "CFF " || tag == "CFF2") hasOutlines = true;
        }

        if (!hasOutlines || nameOffset < 0 || nameLength < 6 || nameLength > MaximumNameTableBytes) return null;
        var names = Read(stream, nameOffset, (int)nameLength);
        if (names == null) return null;
        var legacyFamily = Name(names, 1);
        var family = Name(names, 16) ?? legacyFamily;
        if (family == null) return null;

        var weight = 400;
        var width = 5;
        var italic = false;
        var os2 = os2Offset >= 0 && os2Length >= 64 ? Read(stream, os2Offset, 64) : null;
        if (os2 != null) {
            weight = Math.Max(1, Math.Min(1000, (int)UInt16(os2, 4)));
            width = Math.Max(1, Math.Min(9, (int)UInt16(os2, 6)));
            var selection = UInt16(os2, 62);
            italic = (selection & 0x0001) != 0 || (selection & 0x0200) != 0;
        } else {
            var head = headOffset >= 0 ? Read(stream, headOffset, 46) : null;
            if (head != null) {
                var macStyle = UInt16(head, 44);
                if ((macStyle & 1) != 0) weight = 700;
                italic = (macStyle & 2) != 0;
            }
        }

        return new InstalledFontFace(path, collectionIndex, family, string.Equals(family, legacyFamily, StringComparison.OrdinalIgnoreCase) ? null : legacyFamily, weight, width, italic);
    }

    // Prefers the Windows English record, then any Unicode record, then Macintosh Roman.
    private static string? Name(byte[] table, int nameId) {
        var count = UInt16(table, 2);
        var storage = UInt16(table, 4);
        string? best = null;
        var bestRank = 0;
        for (var i = 0; i < count; i++) {
            var record = 6 + i * 12;
            if (record + 12 > table.Length) break;
            if (UInt16(table, record + 6) != nameId) continue;
            var platform = UInt16(table, record);
            var language = UInt16(table, record + 4);
            var length = UInt16(table, record + 8);
            var offset = storage + UInt16(table, record + 10);
            if (length == 0 || offset + length > table.Length) continue;
            var rank = platform == 3 ? language == 0x0409 ? 4 : 3 : platform == 0 ? 2 : platform == 1 && language == 0 ? 1 : 0;
            if (rank <= bestRank) continue;
            var value = (platform == 1 ? Encoding.ASCII.GetString(table, offset, length) : Encoding.BigEndianUnicode.GetString(table, offset, length)).Trim();
            if (value.Length == 0) continue;
            best = value;
            bestRank = rank;
        }

        return best;
    }

    private static byte[]? Read(FileStream stream, long offset, int length) {
        if (offset < 0 || length < 0 || offset + length > stream.Length) return null;
        var buffer = new byte[length];
        stream.Position = offset;
        var read = 0;
        while (read < length) {
            var chunk = stream.Read(buffer, read, length - read);
            if (chunk <= 0) return null;
            read += chunk;
        }

        return buffer;
    }

    private static ushort UInt16(byte[] data, int offset) => (ushort)((data[offset] << 8) | data[offset + 1]);

    private static uint UInt32(byte[] data, int offset) => ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
}
