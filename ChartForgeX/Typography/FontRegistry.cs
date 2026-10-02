using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>
/// Font files an application registers once and then names by family everywhere raster text is
/// drawn: chart, grid, topology, and visual block themes, <see cref="FontSpec.FromFamily"/> text,
/// VisualCanvas themes, and SVG <c>font-family</c> in <c>SvgRasterizer</c>.
/// </summary>
/// <remarks>
/// A registered family takes precedence over an installed family of the same name, and its faces
/// are chosen by the same weight and slant matching; installed faces of that family are not mixed
/// in. Registering a generic family name (<c>sans-serif</c>, <c>serif</c>, <c>monospace</c>) sets the
/// face that stacks ending in that keyword fall back to, and a registered <c>sans-serif</c> is also
/// the last resort on a host with no usable fonts, such as a bare Linux container. TrueType and CFF
/// outlines are supported (<c>.ttf</c>, <c>.otf</c>, <c>.ttc</c>, <c>.otc</c>). All members are thread-safe.
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
    /// <param name="collectionIndex">The face within a <c>.ttc</c> or <c>.otc</c> collection; the first text face when omitted.</param>
    /// <exception cref="ArgumentException">The family is empty or the file is not a readable OpenType text font.</exception>
    public static void Register(string family, string path, int weight = 400, bool italic = false, int? collectionIndex = null) {
        if (string.IsNullOrWhiteSpace(family)) throw new ArgumentException("Font family must not be empty.", nameof(family));
        if (weight < 1 || weight > 1000) throw new ArgumentOutOfRangeException(nameof(weight), weight, "Font weight must be from 1 through 1000.");
        var fullPath = FullPath(path);
        var font = TrueTypeFont.TryLoadFromPath(fullPath, collectionIndex);
        if (font == null || !font.IsTextFace) throw new ArgumentException("The file is not a readable OpenType text font: " + path, nameof(path));
        Add(new[] { new InstalledFontFace(fullPath, font.CollectionIndex, family.Trim(), null, weight, 5, italic) });
    }

    /// <summary>Registers every face of a font file under the family, weight, and slant the file itself declares.</summary>
    /// <returns>The number of faces registered.</returns>
    /// <exception cref="ArgumentException">The file declares no readable OpenType face.</exception>
    public static int RegisterFile(string path) {
        var faces = new List<InstalledFontFace>();
        InstalledFontCatalog.ReadFaces(FullPath(path), faces);
        faces.RemoveAll(face => TrueTypeFont.TryLoadFromPath(face.Path, face.CollectionIndex)?.IsTextFace != true);
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
                if (!faces.Contains(candidate) && TrueTypeFont.TryLoadFromPath(candidate.Path, candidate.CollectionIndex)?.IsTextFace == true) faces.Add(candidate);
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
