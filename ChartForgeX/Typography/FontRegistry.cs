using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>
/// Font faces an application registers once from files, bytes, or streams and then names by family everywhere raster text is
/// drawn: chart, grid, topology, and visual block themes, <see cref="FontSpec.FromFamily"/> text,
/// VisualCanvas themes, and SVG <c>font-family</c> in <c>SvgRasterizer</c>.
/// </summary>
/// <remarks>
/// A registered family takes precedence over an installed family of the same name, and its faces
/// are chosen by the same weight and slant matching; installed faces of that family are not mixed
/// in. Registering a generic family name (<c>sans-serif</c>, <c>serif</c>, <c>monospace</c>) sets the
/// face that stacks ending in that keyword fall back to, and a registered <c>sans-serif</c> is also
/// the last resort on a host with no usable fonts, such as a bare Linux container. TrueType and CFF
/// outlines and colour-only emoji faces are supported (<c>.ttf</c>, <c>.otf</c>, <c>.ttc</c>, <c>.otc</c>). All members are thread-safe.
/// </remarks>
public static class FontRegistry {
    private static readonly object Gate = new();
    private static volatile Dictionary<string, List<InstalledFontFace>> _families = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the registered family names.</summary>
    public static IReadOnlyCollection<string> Families {
        get {
            var families = _families;
            var names = new List<string>(families.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }
    }

    /// <summary>Registers one face of a font file under a family name.</summary>
    /// <param name="family">The family name stacks will use, such as <c>Inter</c> or <c>sans-serif</c>.</param>
    /// <param name="path">The OpenType font or collection file (<c>.ttf</c>, <c>.otf</c>, <c>.ttc</c>, <c>.otc</c>).</param>
    /// <param name="weight">The CSS weight of this face, 1 through 1000.</param>
    /// <param name="italic">True when this face is the italic of the family.</param>
    /// <param name="collectionIndex">The face within a <c>.ttc</c> or <c>.otc</c> collection; the first usable text or colour face when omitted.</param>
    /// <exception cref="ArgumentException">The family is empty or the file is not a readable OpenType text or colour font.</exception>
    public static void Register(string family, string path, int weight = 400, bool italic = false, int? collectionIndex = null) {
        if (string.IsNullOrWhiteSpace(family)) throw new ArgumentException("Font family must not be empty.", nameof(family));
        if (weight < 1 || weight > 1000) throw new ArgumentOutOfRangeException(nameof(weight), weight, "Font weight must be from 1 through 1000.");
        var fullPath = FullPath(path);
        var font = TrueTypeFont.TryLoadFromPath(fullPath, collectionIndex);
        if (font == null || !font.IsTextFace) throw new ArgumentException("The file is not a readable OpenType text or colour font: " + path, nameof(path));
        Add(new[] { new InstalledFontFace(fullPath, font.CollectionIndex, family.Trim(), null, weight, 5, italic) });
    }

    /// <summary>Registers one OpenType face from bytes under a family name, without filesystem access.</summary>
    /// <param name="family">The family name used by font stacks, including generic names such as <c>sans-serif</c>.</param>
    /// <param name="data">The complete TrueType, CFF, or OpenType collection data. Registration retains its own copy.</param>
    /// <param name="weight">The CSS weight of this face, 1 through 1000.</param>
    /// <param name="italic">True when this face is the italic of the family.</param>
    /// <param name="collectionIndex">The collection face index; the first usable text or colour face when omitted.</param>
    /// <remarks>The caller may modify the input after registration. Registering the same weight and slant replaces the earlier face and invalidates font resolution caches.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    /// <exception cref="ArgumentException">The family is empty or the data is not a readable OpenType text or colour font.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The weight is outside 1 through 1000.</exception>
    public static void Register(string family, byte[] data, int weight = 400, bool italic = false, int? collectionIndex = null) {
        ValidateMemoryRegistration(family, weight);
        if (data == null) throw new ArgumentNullException(nameof(data));
        RegisterOwnedBytes(family, (byte[])data.Clone(), weight, italic, collectionIndex, nameof(data));
    }

    /// <summary>Registers one OpenType face from a readable stream, without filesystem access.</summary>
    /// <param name="family">The family name used by font stacks, including generic names such as <c>sans-serif</c>.</param>
    /// <param name="stream">A readable stream containing a complete font from its current position to its end. It need not support seeking.</param>
    /// <param name="weight">The CSS weight of this face, 1 through 1000.</param>
    /// <param name="italic">True when this face is the italic of the family.</param>
    /// <param name="collectionIndex">The collection face index; the first usable text or colour face when omitted.</param>
    /// <remarks>Registration consumes the remaining bytes synchronously, retains its own data, and leaves the stream open. The face has the same matching and replacement behavior as file registration.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException">The family is empty, the stream is unreadable, or its remaining data is not a readable OpenType text or colour font.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The weight is outside 1 through 1000.</exception>
    /// <exception cref="IOException">Reading the stream fails.</exception>
    public static void Register(string family, Stream stream, int weight = 400, bool italic = false, int? collectionIndex = null) {
        ValidateMemoryRegistration(family, weight);
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new ArgumentException("Font stream must be readable.", nameof(stream));
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        RegisterOwnedBytes(family, buffer.ToArray(), weight, italic, collectionIndex, nameof(stream));
    }

    private static void ValidateMemoryRegistration(string family, int weight) {
        if (string.IsNullOrWhiteSpace(family)) throw new ArgumentException("Font family must not be empty.", nameof(family));
        if (weight < 1 || weight > 1000) throw new ArgumentOutOfRangeException(nameof(weight), weight, "Font weight must be from 1 through 1000.");
    }

    private static void RegisterOwnedBytes(string family, byte[] data, int weight, bool italic, int? collectionIndex, string parameter) {
        var font = TrueTypeFont.TryLoad(data, collectionIndex);
        if (font == null || !font.IsTextFace) throw new ArgumentException("The data is not a readable OpenType text or colour font.", parameter);
        Add(new[] { new InstalledFontFace(null, font.CollectionIndex, family.Trim(), null, weight, 5, italic, font) });
    }

    /// <summary>Registers every face of a font file under the family, weight, and slant the file itself declares.</summary>
    /// <returns>The number of faces registered.</returns>
    /// <exception cref="ArgumentException">The file declares no readable OpenType face.</exception>
    public static int RegisterFile(string path) {
        var faces = new List<InstalledFontFace>();
        InstalledFontCatalog.ReadFaces(FullPath(path), faces);
        faces.RemoveAll(face => face.LoadFont()?.IsTextFace != true);
        if (faces.Count == 0) throw new ArgumentException("The file declares no readable OpenType text face: " + path, nameof(path));
        Add(faces);
        return faces.Count;
    }

    /// <summary>Registers every readable <c>.ttf</c>, <c>.otf</c>, <c>.ttc</c>, and <c>.otc</c> face in a folder and its subfolders under their own family names.</summary>
    /// <returns>The number of faces registered; files that are not readable OpenType text fonts are skipped.</returns>
    public static int RegisterDirectory(string directory) {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Font directory must not be empty.", nameof(directory));
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException("Font directory was not found: " + directory);
        var faces = new List<InstalledFontFace>();
        foreach (var face in InstalledFontCatalog.Index(new[] { Path.GetFullPath(directory) }).Values) {
            foreach (var candidate in face) {
                if (!faces.Contains(candidate) && candidate.LoadFont()?.IsTextFace == true) faces.Add(candidate);
            }
        }

        Add(faces);
        return faces.Count;
    }

    /// <summary>Removes every registered face, so families resolve to installed fonts again.</summary>
    public static void Clear() {
        lock (Gate) {
            _families = new Dictionary<string, List<InstalledFontFace>>(StringComparer.OrdinalIgnoreCase);
            TypographyFontResolver.ClearCache();
            TrueTypeFont.ClearFileCache();
        }
    }

    /// <summary>Finds the registered face of <paramref name="family"/> closest to the weight and slant, or null when the family is not registered.</summary>
    internal static InstalledFontFace? Find(string family, int weight, bool italic) {
        var families = _families;
        return families.Count == 0 ? null : InstalledFontCatalog.Find(families, family, weight, italic);
    }

    /// <summary>Every registered face of <paramref name="family"/>, closest to the weight and slant first.</summary>
    internal static IReadOnlyList<InstalledFontFace> Ranked(string family, int weight, bool italic) {
        var families = _families;
        return families.Count == 0 ? Array.Empty<InstalledFontFace>() : InstalledFontCatalog.Ranked(families, family, weight, italic);
    }

    private static void Add(IEnumerable<InstalledFontFace> faces) {
        lock (Gate) {
            // Readers see either the old or the new snapshot, never one being edited.
            var next = new Dictionary<string, List<InstalledFontFace>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in _families) next[pair.Key] = new List<InstalledFontFace>(pair.Value);
            foreach (var face in faces) {
                AddUnder(next, face.Family, face);
                if (face.LegacyFamily != null) AddUnder(next, face.LegacyFamily, face);
            }

            _families = next;
            TypographyFontResolver.ClearCache();
        }
    }

    private static void AddUnder(Dictionary<string, List<InstalledFontFace>> families, string family, InstalledFontFace face) {
        if (!families.TryGetValue(family, out var list)) families[family] = list = new List<InstalledFontFace>();
        // Registering the same face again replaces it rather than duplicating it.
        list.RemoveAll(existing => existing.Weight == face.Weight && existing.Italic == face.Italic && existing.Width == face.Width);
        list.Add(face);
    }

    private static string FullPath(string path) {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Font path must not be empty.", nameof(path));
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Font file was not found.", fullPath);
        return fullPath;
    }
}
