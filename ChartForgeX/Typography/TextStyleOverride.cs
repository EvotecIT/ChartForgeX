using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Typography;

/// <summary>
/// Defines optional overrides that can be resolved over a complete <see cref="TextStyle"/>.
/// </summary>
public sealed class TextStyleOverride {
    private string? _fontFamily;
    private string? _fontWeight;
    private double? _fontSize;
    private TextDecorationStyle? _underlineStyle;
    private TextDecorationStyle? _strikethroughStyle;
    private TextBaseline? _baseline;
    private TextCaseTransform? _textCase;

    /// <summary>Gets or sets the optional text color override.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets the optional CSS font-family override.</summary>
    public string? FontFamily { get => _fontFamily; set => _fontFamily = OptionalText(value); }

    /// <summary>Gets or sets the optional CSS font-weight override.</summary>
    public string? FontWeight { get => _fontWeight; set => _fontWeight = OptionalText(value); }

    /// <summary>Gets or sets the optional font size override.</summary>
    public double? FontSize {
        get => _fontSize;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0)) throw new ArgumentOutOfRangeException(nameof(value), value, "Font size must be finite and greater than zero.");
            _fontSize = value;
        }
    }

    /// <summary>Gets or sets a value indicating whether text is italic.</summary>
    public bool Italic { get; set; }

    /// <summary>Gets or sets a value indicating whether text is underlined.</summary>
    public bool Underline { get; set; }

    /// <summary>Gets or sets an optional underline pattern. None explicitly disables a fallback underline.</summary>
    public TextDecorationStyle? UnderlineStyle {
        get => _underlineStyle;
        set { ValidateOptionalEnum(value, nameof(value)); _underlineStyle = value; }
    }

    /// <summary>Gets or sets a compatibility value that enables a single solid strikethrough.</summary>
    public bool Strikethrough { get; set; }

    /// <summary>Gets or sets an optional strikethrough pattern. None explicitly disables a fallback strike.</summary>
    public TextDecorationStyle? StrikethroughStyle {
        get => _strikethroughStyle;
        set { ValidateOptionalEnum(value, nameof(value)); _strikethroughStyle = value; }
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
    public bool HasOverrides => Color.HasValue || FontFamily != null || FontWeight != null || FontSize.HasValue || Italic || Underline || UnderlineStyle.HasValue || Strikethrough || StrikethroughStyle.HasValue || Baseline.HasValue || TextCase.HasValue;

    /// <summary>Resolves these overrides over a complete text style without mutating the fallback.</summary>
    public TextStyle Resolve(TextStyle fallback) {
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        var resolved = fallback.Clone();
        if (Color.HasValue) resolved.Color = Color.Value;
        if (FontFamily != null) resolved.Font.Family = FontFamily;
        if (FontWeight != null) resolved.Font.Weight = ResolveWeight(FontWeight, resolved.Font.Weight);
        if (FontSize.HasValue) resolved.FontSize = FontSize.Value;
        if (Italic) resolved.Font.Italic = true;
        if (UnderlineStyle.HasValue) resolved.UnderlineStyle = UnderlineStyle.Value;
        else if (Underline) resolved.UnderlineStyle = TextDecorationStyle.Single;
        if (StrikethroughStyle.HasValue) resolved.StrikethroughStyle = StrikethroughStyle.Value;
        else if (Strikethrough) resolved.StrikethroughStyle = TextDecorationStyle.Single;
        if (Baseline.HasValue) resolved.Baseline = Baseline.Value;
        if (TextCase.HasValue) resolved.TextCase = TextCase.Value;
        return resolved;
    }

    /// <summary>Sets the text color.</summary>
    public TextStyleOverride WithColor(ChartColor color) { Color = color; return this; }

    /// <summary>Sets the text color from hexadecimal notation.</summary>
    public TextStyleOverride WithColor(string hex) { Color = ChartColor.FromHex(hex); return this; }

    /// <summary>Sets the CSS font-family.</summary>
    public TextStyleOverride WithFontFamily(string fontFamily) { FontFamily = fontFamily ?? throw new ArgumentNullException(nameof(fontFamily)); return this; }

    /// <summary>Sets the font size.</summary>
    public TextStyleOverride WithFontSize(double fontSize) { FontSize = fontSize; return this; }

    /// <summary>Sets the CSS font-weight.</summary>
    public TextStyleOverride WithWeight(string fontWeight) { FontWeight = fontWeight ?? throw new ArgumentNullException(nameof(fontWeight)); return this; }

    /// <summary>Sets italic text.</summary>
    public TextStyleOverride WithItalic(bool enabled = true) { Italic = enabled; return this; }

    /// <summary>Sets underlined text.</summary>
    public TextStyleOverride WithUnderline(bool enabled = true) { Underline = enabled; UnderlineStyle = enabled ? TextDecorationStyle.Single : TextDecorationStyle.None; return this; }

    /// <summary>Sets the underline pattern.</summary>
    public TextStyleOverride WithUnderline(TextDecorationStyle style) { UnderlineStyle = style; Underline = style != TextDecorationStyle.None; return this; }

    /// <summary>Sets a single solid strikethrough.</summary>
    public TextStyleOverride WithStrikethrough(bool enabled = true) { Strikethrough = enabled; StrikethroughStyle = enabled ? TextDecorationStyle.Single : TextDecorationStyle.None; return this; }

    /// <summary>Sets the strikethrough pattern.</summary>
    public TextStyleOverride WithStrikethrough(TextDecorationStyle style) { StrikethroughStyle = style; Strikethrough = style != TextDecorationStyle.None; return this; }

    /// <summary>Sets subscript or superscript placement.</summary>
    public TextStyleOverride WithBaseline(TextBaseline baseline) { Baseline = baseline; return this; }

    /// <summary>Sets superscript placement.</summary>
    public TextStyleOverride WithSuperscript() => WithBaseline(TextBaseline.Superscript);

    /// <summary>Sets subscript placement.</summary>
    public TextStyleOverride WithSubscript() => WithBaseline(TextBaseline.Subscript);

    /// <summary>Sets a display-time casing transform.</summary>
    public TextStyleOverride WithTextCase(TextCaseTransform textCase) { TextCase = textCase; return this; }

    /// <summary>Transforms text according to this override.</summary>
    public string TransformText(string text, System.Globalization.CultureInfo? culture = null) => TextCaseTransformer.Apply(text, TextCase ?? TextCaseTransform.None, culture);

    internal int ResolveFontWeight(int fallback) => FontWeight == null ? fallback : ResolveEmphasisWeight(FontWeight, fallback);

    private static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value!.Trim();

    private static int ResolveWeight(string value, int fallback) {
        if (int.TryParse(value, out var numeric)) return Math.Max(100, Math.Min(900, (int)Math.Round(numeric / 100.0) * 100));
        if (string.Equals(value, "normal", StringComparison.OrdinalIgnoreCase)) return 400;
        if (string.Equals(value, "bold", StringComparison.OrdinalIgnoreCase)) return 700;
        if (string.Equals(value, "bolder", StringComparison.OrdinalIgnoreCase)) return 700;
        if (string.Equals(value, "lighter", StringComparison.OrdinalIgnoreCase)) return 400;
        return fallback;
    }

    private static int ResolveEmphasisWeight(string value, int fallback) {
        if (int.TryParse(value, out var numeric)) return Math.Max(1, Math.Min(1000, numeric));
        if (string.Equals(value, "normal", StringComparison.OrdinalIgnoreCase)) return 400;
        if (string.Equals(value, "bold", StringComparison.OrdinalIgnoreCase)) return 700;
        if (string.Equals(value, "bolder", StringComparison.OrdinalIgnoreCase)) return 700;
        if (string.Equals(value, "lighter", StringComparison.OrdinalIgnoreCase)) return 400;
        return fallback;
    }

    private static void ValidateOptionalEnum<T>(T? value, string parameterName) where T : struct {
        if (value.HasValue && !Enum.IsDefined(typeof(T), value.Value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown text formatting value.");
    }
}
