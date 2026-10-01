using System;
using System.IO;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>The face chosen for a <see cref="FontSpec"/>, and what still has to be faked on top of it.</summary>
internal readonly struct ResolvedTypeface {
    public ResolvedTypeface(TrueTypeFont? font, bool synthesizeBold, bool synthesizeItalic) {
        Font = font;
        SynthesizeBold = synthesizeBold;
        SynthesizeItalic = synthesizeItalic;
    }

    /// <summary>The outline face, or null when the host has no usable font and the built-in bitmap font draws.</summary>
    public TrueTypeFont? Font { get; }
    /// <summary>True when a bold weight was requested but the face is not bold.</summary>
    public bool SynthesizeBold { get; }
    /// <summary>True when italic was requested but the face is upright.</summary>
    public bool SynthesizeItalic { get; }
}

internal static class TypographyFontResolver {
    private const int MaximumCachedFamilies = 256;
    private static readonly object CacheLock = new();
    private static readonly System.Collections.Generic.Dictionary<string, ResolvedTypeface> FamilyCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the face for text drawn from a <see cref="FontSpec"/>: an explicit file first, then
    /// the first installed family of the stack at the closest weight and slant, then the generic
    /// fallback for the stack (with its bold or italic sibling when one is installed).
    /// </summary>
    internal static ResolvedTypeface ResolveFace(FontSpec font) {
        if (font.FilePath != null) {
            var requested = TrueTypeFont.TryLoadFromPath(font.FilePath, font.CollectionIndex, font.FaceName);
            if (requested != null) return new ResolvedTypeface(requested, font.Weight >= 600, font.Italic);
        }

        var key = font.Family + "|" + font.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture) + (font.Italic ? "|i" : "|n");
        lock (CacheLock) {
            if (FamilyCache.TryGetValue(key, out var cached)) return cached;
        }

        var resolved = ResolveFamily(font.Family, font.Weight, font.Italic);
        lock (CacheLock) {
            if (FamilyCache.Count < MaximumCachedFamilies) FamilyCache[key] = resolved;
        }

        return resolved;
    }

    private static ResolvedTypeface ResolveFamily(string family, int weight, bool italic) {
        foreach (var part in family.Split(',')) {
            var name = part.Trim().Trim('"', '\'').Trim();
            if (name.Length == 0 || IsPlatformAlias(name)) continue;
            // A generic keyword ends the named part of the stack; the fallback below picks its face.
            if (IsGenericFamily(name)) break;
            var face = InstalledFontCatalog.Find(name, weight, italic);
            if (face == null) continue;
            var loaded = TrueTypeFont.TryLoadFromPath(face.Path, face.CollectionIndex);
            if (loaded != null && loaded.IsTextFace) return new ResolvedTypeface(loaded, weight >= 600 && face.Weight < 600, italic && !face.Italic);
        }

        var fallback = TrueTypeFont.TryLoadForFamily(family, out var path);
        if (fallback != null && path != null && (weight != 400 || italic)) {
            var sibling = InstalledFontCatalog.FindSibling(path, weight, italic);
            var loaded = sibling == null ? null : TrueTypeFont.TryLoadFromPath(sibling.Path, sibling.CollectionIndex);
            if (loaded != null && loaded.IsTextFace) return new ResolvedTypeface(loaded, weight >= 600 && sibling!.Weight < 600, italic && !sibling!.Italic);
        }

        return new ResolvedTypeface(fallback, weight >= 600, italic);
    }

    private static bool IsGenericFamily(string name) {
        switch (name.ToLowerInvariant()) {
            case "serif":
            case "sans-serif":
            case "monospace":
            case "cursive":
            case "fantasy":
            case "system-ui":
            case "ui-serif":
            case "ui-sans-serif":
            case "ui-monospace":
            case "ui-rounded":
            case "math":
            case "emoji":
            case "fangsong":
                return true;
            default:
                return false;
        }
    }

    // Names browsers on other platforms understand; a stack lists them before its real families.
    private static bool IsPlatformAlias(string name) =>
        name.Equals("-apple-system", StringComparison.OrdinalIgnoreCase) || name.Equals("BlinkMacSystemFont", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the face chart, grid, and topology renderers pair with a theme font stack. It keeps
    /// the generic classification those renderers draw with, so measured layout matches their output.
    /// </summary>
    public static TrueTypeFont? Resolve(FontSpec font) {
        if (font.FilePath != null) {
            var requested = TrueTypeFont.TryLoadFromPath(font.FilePath, font.CollectionIndex, font.FaceName);
            if (requested != null) return requested;
        }

        return TrueTypeFont.TryLoadForFamily(font.Family, out _);
    }
    // SVG hosts select a real bold face, whereas raster drawing currently synthesizes
    // bold from the regular face. Layout must reserve the larger of both advances.
    internal static TrueTypeFont? ResolveBoldMeasurementFace(string family) {
        TrueTypeFont.TryLoadForFamily(family, out var path);
        if (path == null) return null;
        var name = Path.GetFileName(path);
        string? boldName = null;
        if (name.Equals("Arial.ttf", StringComparison.OrdinalIgnoreCase)) boldName = "Arial Bold.ttf";
        else if (name.EndsWith("-Regular.ttf", StringComparison.OrdinalIgnoreCase)) boldName = name.Substring(0, name.Length - "-Regular.ttf".Length) + "-Bold.ttf";
        else if (name.StartsWith("DejaVu", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)) boldName = Path.GetFileNameWithoutExtension(name) + "-Bold.ttf";
        if (boldName != null) {
            var face = TrueTypeFont.TryLoadFromPath(Path.Combine(Path.GetDirectoryName(path)!, boldName));
            if (face != null) return face;
        }
        // Windows uses short filenames for the Arial faces.
        if (name.Equals("arial.ttf", StringComparison.OrdinalIgnoreCase))
            return TrueTypeFont.TryLoadFromPath(Path.Combine(Path.GetDirectoryName(path)!, "arialbd.ttf"));
        return null;
    }
}
