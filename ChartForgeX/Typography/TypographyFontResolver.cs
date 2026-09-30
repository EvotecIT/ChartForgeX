using System;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>The face chosen for a <see cref="FontSpec"/>, and what still has to be faked on top of it.</summary>
internal readonly struct ResolvedTypeface {
    public ResolvedTypeface(TrueTypeFont? font, bool synthesizeBold, bool synthesizeItalic, string? path = null) {
        Font = font;
        SynthesizeBold = synthesizeBold;
        SynthesizeItalic = synthesizeItalic;
        Path = path;
    }

    /// <summary>The outline face, or null when the host has no usable font and the built-in bitmap font draws.</summary>
    public TrueTypeFont? Font { get; }
    /// <summary>True when a bold weight was requested but the face is not bold.</summary>
    public bool SynthesizeBold { get; }
    /// <summary>True when italic was requested but the face is upright.</summary>
    public bool SynthesizeItalic { get; }
    /// <summary>The file the face was read from, when known.</summary>
    public string? Path { get; }
}

internal static class TypographyFontResolver {
    private const int MaximumCachedFamilies = 256;
    private static readonly object CacheLock = new();
    private static int _cacheVersion;
    private static readonly System.Collections.Generic.Dictionary<string, ResolvedTypeface> FamilyCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the face for text drawn from a <see cref="FontSpec"/>: an explicit file first, then
    /// the first installed family of the stack at the closest weight and slant, then the generic
    /// fallback for the stack (with its bold or italic sibling when one is installed).
    /// </summary>
    internal static ResolvedTypeface ResolveFace(FontSpec font) {
        if (font.FilePath != null) {
            var requested = TrueTypeFont.TryLoadFromPath(font.FilePath, font.CollectionIndex, font.FaceName);
            if (requested != null) return new ResolvedTypeface(requested, font.Weight >= 600, font.Italic, font.FilePath);
        }

        return ResolveFace(font.Family, font.Weight, font.Italic);
    }

    /// <summary>
    /// Resolves a CSS family stack at any CSS weight from 1 through 1000 (SVG and HTML output use
    /// values such as 650 or 850 that <see cref="FontSpec.Weight"/> does not accept).
    /// </summary>
    internal static ResolvedTypeface ResolveFace(string? family, int weight, bool italic) {
        family = string.IsNullOrWhiteSpace(family) ? "sans-serif" : family!.Trim();
        weight = Math.Max(1, Math.Min(1000, weight));
        var key = family + "|" + weight.ToString(System.Globalization.CultureInfo.InvariantCulture) + (italic ? "|i" : "|n");
        int version;
        lock (CacheLock) {
            if (FamilyCache.TryGetValue(key, out var cached)) return cached;
            version = _cacheVersion;
        }

        var resolved = ResolveFamily(family, weight, italic);
        lock (CacheLock) {
            // A registration that landed while this stack was resolving may have changed the answer.
            if (version == _cacheVersion && FamilyCache.Count < MaximumCachedFamilies) FamilyCache[key] = resolved;
        }

        return resolved;
    }

    /// <summary>
    /// Reads a CSS <c>font-weight</c> value: a number from 1 through 1000, <c>normal</c>, <c>bold</c>,
    /// or <c>bolder</c>/<c>lighter</c> relative to the inherited weight. Anything else keeps the inherited weight.
    /// </summary>
    internal static int ParseCssWeight(string? value, int inherited) {
        var text = (value ?? string.Empty).Trim();
        if (text.Equals("normal", StringComparison.OrdinalIgnoreCase)) return 400;
        if (text.Equals("bold", StringComparison.OrdinalIgnoreCase)) return 700;
        // CSS Fonts 4 relative weights.
        if (text.Equals("bolder", StringComparison.OrdinalIgnoreCase)) return inherited < 350 ? 400 : inherited < 550 ? 700 : 900;
        if (text.Equals("lighter", StringComparison.OrdinalIgnoreCase)) return inherited < 550 ? 100 : inherited < 750 ? 400 : 700;
        if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var numeric) && numeric >= 1 && numeric <= 1000) {
            return (int)Math.Round(numeric);
        }

        return inherited;
    }

    private static ResolvedTypeface ResolveFamily(string family, int weight, bool italic) {
        foreach (var part in family.Split(',')) {
            var name = part.Trim().Trim('"', '\'').Trim();
            if (name.Length == 0 || IsPlatformAlias(name)) continue;
            // A generic keyword ends the named part of the stack: a face registered under the keyword
            // answers it, otherwise the fallback below picks one.
            if (IsGenericFamily(name)) {
                if (TryLoad(FontRegistry.Find(name, weight, italic), weight, italic, out var registered)) return registered;
                break;
            }

            // Registered faces take precedence over installed faces of the same family.
            if (TryLoad(FontRegistry.Find(name, weight, italic) ?? InstalledFontCatalog.Find(name, weight, italic), weight, italic, out var resolved)) return resolved;
        }

        var fallback = TrueTypeFont.TryLoadForFamily(family, out var path);
        // A host with no usable fonts, such as a bare container, still has any registered sans-serif face.
        if (fallback == null && TryLoad(FontRegistry.Find("sans-serif", weight, italic), weight, italic, out var lastResort)) return lastResort;
        if (fallback != null && path != null && (weight != 400 || italic)) {
            var sibling = InstalledFontCatalog.FindSibling(path, weight, italic);
            var loaded = sibling == null ? null : TrueTypeFont.TryLoadFromPath(sibling.Path, sibling.CollectionIndex);
            if (loaded != null && loaded.IsTextFace) return new ResolvedTypeface(loaded, weight >= 600 && sibling!.Weight < 600, italic && !sibling!.Italic, sibling!.Path);
        }

        return new ResolvedTypeface(fallback, weight >= 600, italic, path);
    }

    private static bool TryLoad(InstalledFontFace? face, int weight, bool italic, out ResolvedTypeface resolved) {
        var loaded = face == null ? null : TrueTypeFont.TryLoadFromPath(face.Path, face.CollectionIndex);
        resolved = loaded != null && loaded.IsTextFace ? new ResolvedTypeface(loaded, weight >= 600 && face!.Weight < 600, italic && !face!.Italic, face!.Path) : default;
        return loaded != null && loaded.IsTextFace;
    }

    /// <summary>Forgets resolved stacks after the registered fonts change.</summary>
    internal static void ClearCache() {
        lock (CacheLock) {
            FamilyCache.Clear();
            _cacheVersion++;
        }
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
    /// The regular face chart, grid, topology, and visual block renderers draw a theme font stack
    /// with: the same installed-family matching as <see cref="FontSpec"/> text. Inside an open
    /// <see cref="RgbaCanvas.OpenEmphasisScope"/> the face is paired with its real bold face, so
    /// emphasized text in that stack draws and measures bold instead of being drawn twice.
    /// </summary>
    internal static TrueTypeFont? ResolveThemeFont(string? family) {
        var regular = ResolveFace(family, 400, italic: false).Font;
        RgbaCanvas.PairEmphasisFace(regular, ResolveThemeBoldFont(family));
        return regular;
    }

    /// <summary>The real bold face of a theme font stack, or null when bold has to be synthesized from the regular face.</summary>
    internal static TrueTypeFont? ResolveThemeBoldFont(string? family) {
        var bold = ResolveFace(family, 700, italic: false);
        return bold.SynthesizeBold ? null : bold.Font;
    }
}