using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Typography;

/// <summary>
/// Defines fully resolved text styling shared by charts, topology, visual blocks, canvases, and image composition.
/// </summary>
public sealed class TextStyle {
    private FontSpec _font = FontSpec.SystemSans();
    private double _fontSize = 16;
    private double _lineHeight = 1.2;
    private TextDecorationStyle _underlineStyle;
    private TextDecorationStyle _strikethroughStyle;
    private TextBaseline _baseline;
    private TextCaseTransform _textCase;

    /// <summary>Gets or sets the font selection.</summary>
    public FontSpec Font { get => _font; set => _font = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the font size in logical pixels.</summary>
    public double FontSize {
        get => _fontSize;
        set {
            if (!IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Font size must be finite and greater than zero.");
            _fontSize = value;
        }
    }

    /// <summary>Gets or sets the text color.</summary>
    public ChartColor Color { get; set; } = ChartColor.Black;

    /// <summary>Gets or sets horizontal alignment inside the layout region.</summary>
    public TextAlignment Alignment { get; set; }

    /// <summary>Gets or sets line height as a multiplier of the measured font height.</summary>
    public double LineHeight {
        get => _lineHeight;
        set {
            if (!IsFinite(value) || value < 0.8 || value > 3) throw new ArgumentOutOfRangeException(nameof(value), value, "Line height must be from 0.8 through 3.0.");
            _lineHeight = value;
        }
    }

    /// <summary>Gets or sets a compatibility value that maps to a single solid underline.</summary>
    public bool Underline {
        get => UnderlineStyle != TextDecorationStyle.None;
        set => UnderlineStyle = value ? TextDecorationStyle.Single : TextDecorationStyle.None;
    }

    /// <summary>Gets or sets the underline pattern.</summary>
    public TextDecorationStyle UnderlineStyle {
        get => _underlineStyle;
        set { ValidateEnum(value, nameof(value)); _underlineStyle = value; }
    }

    /// <summary>Gets or sets a compatibility value that maps to a single solid strikethrough.</summary>
    public bool Strikethrough {
        get => StrikethroughStyle != TextDecorationStyle.None;
        set => StrikethroughStyle = value ? TextDecorationStyle.Single : TextDecorationStyle.None;
    }

    /// <summary>Gets or sets the strikethrough pattern.</summary>
    public TextDecorationStyle StrikethroughStyle {
        get => _strikethroughStyle;
        set { ValidateEnum(value, nameof(value)); _strikethroughStyle = value; }
    }

    /// <summary>Gets or sets vertical baseline placement.</summary>
    public TextBaseline Baseline {
        get => _baseline;
        set { ValidateEnum(value, nameof(value)); _baseline = value; }
    }

    /// <summary>Gets or sets the display-time text-case transform.</summary>
    public TextCaseTransform TextCase {
        get => _textCase;
        set { ValidateEnum(value, nameof(value)); _textCase = value; }
    }

    /// <summary>Gets the font size used to measure and render this style.</summary>
    public double EffectiveFontSize => Baseline == TextBaseline.Normal ? FontSize : FontSize * 0.65;

    /// <summary>Creates a copy that can be modified independently.</summary>
    public TextStyle Clone() => new() {
        Font = Font.Clone(),
        FontSize = FontSize,
        Color = Color,
        Alignment = Alignment,
        LineHeight = LineHeight,
        UnderlineStyle = UnderlineStyle,
        StrikethroughStyle = StrikethroughStyle,
        Baseline = Baseline,
        TextCase = TextCase
    };

    /// <summary>Creates a text style using the supplied size and color.</summary>
    public static TextStyle Create(double fontSize, ChartColor color) => new() { FontSize = fontSize, Color = color };

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static void ValidateEnum<T>(T value, string parameterName) where T : struct {
        if (!Enum.IsDefined(typeof(T), value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown text formatting value.");
    }
}
