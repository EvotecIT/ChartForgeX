using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Composition;

/// <summary>Plain text layer with either an explicit color or the canvas primary foreground.</summary>
public sealed class VisualCanvasTextLayer : VisualCanvasLayer {
    private string _text;
    private double _fontSize;
    private ChartColor? _explicitColor;

    /// <summary>Initializes a text layer that follows the canvas primary foreground at render time.</summary>
    /// <param name="x">The text X coordinate.</param>
    /// <param name="y">The text Y coordinate.</param>
    /// <param name="width">The text width used for alignment and clipping.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="fontSize">The font size.</param>
    public VisualCanvasTextLayer(double x, double y, double width, string text, double fontSize) : base(x, y, width, Math.Max(1, fontSize * 1.25)) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        FontSize = fontSize;
    }

    /// <summary>Initializes a text layer with an explicit color, retained independently of the canvas theme.</summary>
    /// <param name="x">The text X coordinate.</param>
    /// <param name="y">The text Y coordinate.</param>
    /// <param name="width">The text width used for alignment and clipping.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="color">The explicit text color, including white or transparent.</param>
    public VisualCanvasTextLayer(double x, double y, double width, string text, double fontSize, ChartColor color) : this(x, y, width, text, fontSize) {
        Color = color;
    }

    /// <summary>Gets or sets the text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the font size.</summary>
    public double FontSize { get => _fontSize; set { ValidatePositive(value, nameof(value)); _fontSize = value; Height = Math.Max(Height, value * 1.25); } }
    /// <summary>Gets the authored color, or white when none was supplied. Setting this property selects an explicit color; an uncolored layer renders with the canvas primary foreground.</summary>
    public ChartColor Color { get => _explicitColor ?? ChartColor.White; set => _explicitColor = value; }
    /// <summary>Gets or sets the text alignment.</summary>
    public TextAlignment Alignment { get; set; }
    /// <summary>Gets or sets whether the text should use the emphasized raster/SVG treatment.</summary>
    public bool Emphasized { get; set; }

    internal ChartColor ResolveColor(VisualCanvasTheme theme) => _explicitColor ?? theme.HeroTitleColor;
}
