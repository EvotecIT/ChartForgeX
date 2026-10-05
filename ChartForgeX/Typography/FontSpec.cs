using System;

namespace ChartForgeX.Typography;

/// <summary>
/// Identifies a font without depending on a platform drawing framework.
/// </summary>
public sealed class FontSpec {
    private string _family = "system-ui, sans-serif";
    private string? _filePath;
    private string? _faceName;
    private int? _collectionIndex;
    private int _weight = 400;
    private int _colorPaletteIndex;

    /// <summary>Gets or sets the zero-based CPAL palette index. Zero is the default.
    /// Missing or unusable palettes fall back to the font's default palette; bitmap glyphs are unaffected.</summary>
    public int ColorPaletteIndex {
        get => _colorPaletteIndex;
        set { ValidateColorPaletteIndex(value); _colorPaletteIndex = value; }
    }
    /// <summary>Selects a colour-font palette without changing glyph advances.</summary>
    public FontSpec WithColorPalette(int index) { ColorPaletteIndex = index; return this; }
    internal static void ValidateColorPaletteIndex(int value) {
        if (value < 0 || value > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(value), value, "Palette index must be between 0 and 65535.");
    }
    private FontVariationSettings _variations = FontVariationSettings.Default;

    /// <summary>Gets or sets immutable OpenType axis coordinates. The default uses the face's
    /// default instance; Weight and Italic retain their existing face-selection policy.</summary>
    public FontVariationSettings Variations { get => _variations; set => _variations = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Selects an explicit variation axis in design units.</summary>
    public FontSpec WithVariation(string tag, double value) { Variations = Variations.WithAxis(tag, value); return this; }

    /// <summary>Gets or sets the CSS-compatible family or fallback stack.</summary>
    public string Family {
        get => _family;
        set {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Font family must not be empty.", nameof(value));
            _family = value.Trim();
        }
    }

    /// <summary>Gets or sets an optional TrueType or TrueType collection path used by raster output.</summary>
    public string? FilePath {
        get => _filePath;
        set => _filePath = string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
    }

    /// <summary>Gets or sets an optional TrueType collection face index.</summary>
    public int? CollectionIndex {
        get => _collectionIndex;
        set {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Font collection index must be non-negative.");
            _collectionIndex = value;
        }
    }

    /// <summary>Gets or sets an optional face name used when selecting a collection member.</summary>
    public string? FaceName {
        get => _faceName;
        set => _faceName = string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
    }

    /// <summary>Gets or sets the numeric font weight from 100 through 900.</summary>
    public int Weight {
        get => _weight;
        set {
            if (value < 100 || value > 900) throw new ArgumentOutOfRangeException(nameof(value), value, "Font weight must be from 100 through 900.");
            _weight = value;
        }
    }

    /// <summary>Gets or sets a value indicating whether an italic face is preferred.</summary>
    public bool Italic { get; set; }

    /// <summary>Creates a copy that can be modified independently.</summary>
    public FontSpec Clone() => new() {
        Family = Family,
        FilePath = FilePath,
        CollectionIndex = CollectionIndex,
        FaceName = FaceName,
        Weight = Weight,
        Italic = Italic,
        Variations = Variations,
        ColorPaletteIndex = ColorPaletteIndex
    };

    /// <summary>Creates a system sans-serif font specification.</summary>
    public static FontSpec SystemSans() => new();

    /// <summary>Creates a font specification for a family or fallback stack.</summary>
    public static FontSpec FromFamily(string family) => new() { Family = family };

    /// <summary>Creates a font specification backed by a TrueType font file.</summary>
    public static FontSpec FromFile(string path, int? collectionIndex = null, string? faceName = null) => new() {
        FilePath = path ?? throw new ArgumentNullException(nameof(path)),
        CollectionIndex = collectionIndex,
        FaceName = faceName
    };
}
