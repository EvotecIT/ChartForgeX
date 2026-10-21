using System;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.Composition;

public sealed partial class ImageComposition {
    /// <summary>Draws an existing measured layout without wrapping or applying its text-case transform again.</summary>
    /// <remarks>Use the font, size, language and line-height settings used to measure the layout. Color,
    /// decorations and alignment may be changed before painting. Coordinates identify the top-left layout region.
    /// Lines wider than the region start at its left edge rather than shifting content outside that edge.</remarks>
    public ImageComposition DrawTextLayout(double x, double y, double width, TextLayout layout, TextStyle style) {
        ValidateFinite(x, nameof(x));
        ValidateFinite(y, nameof(y));
        ValidatePositive(width, nameof(width));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        if (style == null) throw new ArgumentNullException(nameof(style));
        VisualCanvas.ValidateEnum(style.Alignment, nameof(style.Alignment));
        _canvas.DrawStyledTextLayout(x, y, width, layout, style, containAlignment: true);
        return this;
    }

    /// <summary>Draws a single-row left title and right note, trimming the title to the space left by the note.</summary>
    /// <param name="x">Left edge of the header.</param>
    /// <param name="y">Top edge of the header.</param>
    /// <param name="width">Available width in pixels.</param>
    /// <param name="title">Optional title. Its configured text-case transform is applied once.</param>
    /// <param name="note">Optional right-aligned note, given priority when space is limited.</param>
    /// <param name="titleStyle">Typography and color for the title. The supplied instance is not changed.</param>
    /// <param name="noteStyle">Optional note style. When omitted, the title style is used without its casing transform.</param>
    /// <param name="gap">Minimum separation between the two pieces of text.</param>
    /// <returns>The occupied height, or zero when neither piece of text can be displayed.</returns>
    public double DrawTextHeader(double x, double y, double width, string? title, string? note,
        TextStyle titleStyle, TextStyle? noteStyle = null, double gap = 12) {
        ValidateFinite(x, nameof(x));
        ValidateFinite(y, nameof(y));
        ValidatePositive(width, nameof(width));
        ValidateNonNegative(gap, nameof(gap));
        if (titleStyle == null) throw new ArgumentNullException(nameof(titleStyle));
        var height = 0d;
        var noteWidth = 0d;
        if (!string.IsNullOrWhiteSpace(note)) {
            var style = (noteStyle ?? titleStyle).Clone();
            if (noteStyle == null) style.TextCase = TextCaseTransform.None;
            style.Alignment = TextAlignment.Right;
            var layout = TextLayoutEngine.Layout(note!, width, style, TextWrapMode.NoWrap, 1, TextTrimming.Ellipsis);
            if (layout.Lines.Count > 0 && layout.Lines[0].Text.Length > 0) {
                noteWidth = layout.Metrics.Width;
                height = layout.Metrics.Height;
                DrawTextLayout(x, y, width, layout, style);
            }
        }

        var titleWidth = Math.Max(0, width - (noteWidth > 0 ? noteWidth + gap : 0));
        if (!string.IsNullOrWhiteSpace(title) && titleWidth > 0) {
            var style = titleStyle.Clone();
            style.Alignment = TextAlignment.Left;
            var layout = TextLayoutEngine.Layout(title!, titleWidth, style, TextWrapMode.NoWrap, 1, TextTrimming.Ellipsis);
            if (layout.Lines.Count > 0 && layout.Lines[0].Text.Length > 0) {
                height = Math.Max(height, layout.Metrics.Height);
                DrawTextLayout(x, y, titleWidth, layout, style);
            }
        }
        return height;
    }
}
