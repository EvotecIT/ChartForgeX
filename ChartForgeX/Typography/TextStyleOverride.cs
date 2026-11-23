using System;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Typography;

/// <summary>
/// Defines optional overrides that can be resolved over a complete <see cref="TextStyle"/>.
/// </summary>
[Fluent(Copy = nameof(Clone))]
public sealed class TextStyleOverride {
    private string? _fontFamily;
    private int? _fontWeight;
    private double? _fontSize;
    private double? _letterSpacing;
    private TextDecorationStyle? _underlineStyle;
    private TextDecorationStyle? _strikethroughStyle;
    private TextBaseline? _baseline;
    private TextCaseTransform? _textCase;
    private string? _openTypeLanguageTag;
    private int? _colorPaletteIndex;
    private bool _italic, _underline, _strikethrough;
    private bool _hasItalic, _hasUnderline, _hasStrikethrough;
    /// <summary>Gets or sets an optional zero-based CPAL palette. Null inherits; zero resets to the default.</summary>
    public int? ColorPaletteIndex {
        get => _colorPaletteIndex;
        set { if (value.HasValue) FontSpec.ValidateColorPaletteIndex(value.Value); _colorPaletteIndex = value; }
    }
    /// <summary>Gets or sets explicit OpenType axes. Null inherits; Default restores the font's default instance.</summary>
    public FontVariationSettings? Variations { get; set; }
    /// <summary>Sets one OpenType variation axis in design units.</summary>
    public TextStyleOverride Variation(string tag, double value) { Variations = (Variations ?? FontVariationSettings.Default).WithAxis(tag, value); return this; }

    /// <summary>Gets or sets the optional text color override.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets the optional CSS font-family override.</summary>
    public string? FontFamily { get => _fontFamily; set => _fontFamily = OptionalText(value); }

    /// <summary>Gets or sets the optional font weight, from 1 to 1000; 400 is regular and 700 bold.</summary>
    public int? FontWeight {
        get => _fontWeight;
        set {
            if (value is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(value), value, "Font weight must be between 1 and 1000.");
            _fontWeight = value;
        }
    }

    /// <summary>Gets or sets the optional OpenType language-system tag, such as SRB or TRK.
    /// A null override retains the fallback style's language; normal selects the font's default.</summary>
    public string? OpenTypeLanguageTag {
        get => _openTypeLanguageTag;
        set => _openTypeLanguageTag = value?.Trim().Equals("normal", StringComparison.OrdinalIgnoreCase) == true ? "normal" : OpenTypeLanguage.Normalize(value);
    }

    /// <summary>Gets or sets the optional font size override.</summary>
    public double? FontSize {
        get => _fontSize;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0)) throw new ArgumentOutOfRangeException(nameof(value), value, "Font size must be finite and greater than zero.");
            _fontSize = value;
        }
    }

    /// <summary>Gets or sets additional logical pixels between eligible shaped groups. Null inherits; zero resets spacing.</summary>
    public double? LetterSpacing {
        get => _letterSpacing;
        set { if (value.HasValue) TextStyle.ValidateLetterSpacing(value.Value); _letterSpacing = value; }
    }

    /// <summary>Gets or sets a value indicating whether text is italic.</summary>
    public bool Italic { get => _italic; set { _italic = value; _hasItalic = true; } }

    /// <summary>Gets or sets a value indicating whether text is underlined. Turning it on selects a single underline unless a style is already set; turning it off selects none.</summary>
    public bool Underline { get => _underline; set { _underline = value; _hasUnderline = true; if (!value) _underlineStyle = TextDecorationStyle.None; else if (_underlineStyle is null or TextDecorationStyle.None) _underlineStyle = TextDecorationStyle.Single; } }

    /// <summary>Gets or sets an optional underline pattern. None explicitly disables a fallback underline; null clears the underline override so the fallback applies.</summary>
    public TextDecorationStyle? UnderlineStyle {
        get => _underlineStyle;
        set {
            ValidateOptionalEnum(value, nameof(value));
            _underlineStyle = value;
            if (value.HasValue) { _underline = value.Value != TextDecorationStyle.None; _hasUnderline = true; } else { _underline = false; _hasUnderline = false; }
        }
    }

    /// <summary>Gets or sets whether text is struck through. Turning it on selects a single strikethrough unless a style is already set; turning it off selects none.</summary>
    public bool Strikethrough { get => _strikethrough; set { _strikethrough = value; _hasStrikethrough = true; if (!value) _strikethroughStyle = TextDecorationStyle.None; else if (_strikethroughStyle is null or TextDecorationStyle.None) _strikethroughStyle = TextDecorationStyle.Single; } }

    /// <summary>Gets or sets an optional strikethrough pattern. None explicitly disables a fallback strike; null clears the strikethrough override so the fallback applies.</summary>
    public TextDecorationStyle? StrikethroughStyle {
        get => _strikethroughStyle;
        set {
            ValidateOptionalEnum(value, nameof(value));
            _strikethroughStyle = value;
            if (value.HasValue) { _strikethrough = value.Value != TextDecorationStyle.None; _hasStrikethrough = true; } else { _strikethrough = false; _hasStrikethrough = false; }
        }
    }

    /// <summary>Gets or sets optional subscript or superscript placement.</summary>
    public TextBaseline? Baseline {
        get => _baseline;
        set { ValidateOptionalEnum(value, nameof(value)); _baseline = value; }
    }

    /// <summary>Gets or sets an optional display-time casing transform.</summary>
    public TextCaseTransform? TextCase {
        get => _textCase;
        set { ValidateOptionalEnum(value, nameof(value)); _textCase = value; }
    }

    /// <summary>Gets a value indicating whether this instance contains explicit overrides.</summary>
    public bool HasOverrides => Color.HasValue || FontFamily != null || FontWeight != null || FontSize.HasValue || LetterSpacing.HasValue || _hasItalic || _hasUnderline || UnderlineStyle.HasValue || _hasStrikethrough || StrikethroughStyle.HasValue || Baseline.HasValue || TextCase.HasValue || OpenTypeLanguageTag != null || Variations != null || ColorPaletteIndex.HasValue;

    /// <summary>Resolves these overrides over a complete text style without mutating the fallback.</summary>
    public TextStyle Resolve(TextStyle fallback) {
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        var resolved = fallback.Clone();
        if (Color.HasValue) resolved.Color = Color.Value;
        if (FontFamily != null) resolved.Font.Family = FontFamily;
        if (FontWeight is { } weight) resolved.Font.Weight = TypographyFontResolver.FontSpecWeight(weight);
        if (FontSize.HasValue) resolved.FontSize = FontSize.Value;
        if (LetterSpacing.HasValue) resolved.LetterSpacing = LetterSpacing.Value;
        if (_hasItalic) resolved.Font.Italic = Italic;
        if (UnderlineStyle.HasValue) resolved.UnderlineStyle = UnderlineStyle.Value;
        else if (_hasUnderline) resolved.UnderlineStyle = Underline ? TextDecorationStyle.Single : TextDecorationStyle.None;
        if (StrikethroughStyle.HasValue) resolved.StrikethroughStyle = StrikethroughStyle.Value;
        else if (_hasStrikethrough) resolved.StrikethroughStyle = Strikethrough ? TextDecorationStyle.Single : TextDecorationStyle.None;
        if (Baseline.HasValue) resolved.Baseline = Baseline.Value;
        if (TextCase.HasValue) resolved.TextCase = TextCase.Value;
        if (OpenTypeLanguageTag != null) resolved.OpenTypeLanguageTag = OpenTypeLanguageTag == "normal" ? null : OpenTypeLanguageTag;
        if (Variations != null) resolved.Font.Variations = Variations;
        if (ColorPaletteIndex.HasValue) resolved.Font.ColorPaletteIndex = ColorPaletteIndex.Value;
        return resolved;
    }

    /// <summary>Transforms text according to this override.</summary>
    public string TransformText(string text, System.Globalization.CultureInfo? culture = null) => TextCaseTransformer.Apply(text, TextCase ?? TextCaseTransform.None, culture);

    /// <summary>Creates an independent copy of the optional typography values.</summary>
    public TextStyleOverride Clone() => (TextStyleOverride)MemberwiseClone();

    // Preserve explicit false separately from inheritance in typed interchange.
    internal bool? ItalicOverride => _hasItalic ? _italic : null;
    internal bool? UnderlineOverride => _hasUnderline ? _underline : null;
    internal bool? StrikethroughOverride => _hasStrikethrough ? _strikethrough : null;

    /// <summary>Returns a copy in which every value set on <paramref name="overrides"/> replaces this one.</summary>
    internal TextStyleOverride Overlay(TextStyleOverride overrides) {
        var copy = Clone();
        if (overrides.Color.HasValue) copy.Color = overrides.Color;
        copy._fontFamily = overrides._fontFamily ?? _fontFamily; copy._fontWeight = overrides._fontWeight ?? _fontWeight;
        copy._fontSize = overrides._fontSize ?? _fontSize; copy._letterSpacing = overrides._letterSpacing ?? _letterSpacing;
        copy._underlineStyle = overrides._underlineStyle ?? _underlineStyle; copy._strikethroughStyle = overrides._strikethroughStyle ?? _strikethroughStyle;
        copy._baseline = overrides._baseline ?? _baseline; copy._textCase = overrides._textCase ?? _textCase;
        copy._openTypeLanguageTag = overrides._openTypeLanguageTag ?? _openTypeLanguageTag;
        copy._colorPaletteIndex = overrides._colorPaletteIndex ?? _colorPaletteIndex;
        copy.Variations = overrides.Variations ?? Variations;
        if (overrides._hasItalic) { copy._italic = overrides._italic; copy._hasItalic = true; }
        if (overrides._hasUnderline) { copy._underline = overrides._underline; copy._hasUnderline = true; }
        if (overrides._hasStrikethrough) { copy._strikethrough = overrides._strikethrough; copy._hasStrikethrough = true; }
        return copy;
    }

    internal int ResolveFontWeight(int fallback) => FontWeight ?? fallback;

    internal TextStyleOverride WithDefaultFontWeight(int weight) {
        if (FontWeight != null) return this;
        var copy = (TextStyleOverride)MemberwiseClone();
        copy.FontWeight = weight;
        return copy;
    }

    internal TextStyleOverride WithDefaultFontFamily(string family) {
        if (FontFamily != null) return this;
        var copy = (TextStyleOverride)MemberwiseClone();
        copy.FontFamily = family;
        return copy;
    }

    /// <summary>The family <see cref="FontFamily"/> would hold after assigning <paramref name="family"/>: trimmed, or null when blank.</summary>
    internal static string? NormalizeFontFamily(string? family) => OptionalText(family);

    private static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value!.Trim();

    /// <summary>Reads a CSS font-weight value (a number or normal, bold, bolder, lighter), or null when it isn't one.</summary>
    internal static int? ParseFontWeight(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value!.Trim();
        // An override has no inherited weight, so the relative keywords resolve against regular text.
        if (text.Equals("bolder", StringComparison.OrdinalIgnoreCase)) return 700;
        if (text.Equals("lighter", StringComparison.OrdinalIgnoreCase)) return 400;
        var weight = TypographyFontResolver.ParseCssWeight(text, -1);
        return weight < 1 ? null : weight;
    }

    private static void ValidateOptionalEnum<T>(T? value, string parameterName) where T : struct, Enum {
        if (value.HasValue && !Enum.IsDefined(typeof(T), value.Value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown text formatting value.");
    }
}
