using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Measures and paints descriptive reference circles with the same logical radii as Bubble marks.</summary>
internal sealed class VisualBubbleSizeKeyScene {
    private readonly ChartBubbleSizeScale _scale;
    private readonly TextStyle _style;
    private readonly double[] _values;
    private readonly string[] _captions;
    private readonly double _gap, _rowHeight, _headingHeight;
    internal ChartRect Bounds { get; set; }
    internal double Height { get; private set; }
    internal bool Fits { get; }
    internal double RequiredWidth { get; }

    private VisualBubbleSizeKeyScene(ChartBubbleSizeScale scale, TextStyle style, double[] values,
        string[] captions, double gap, double rowHeight, double headingHeight, double height, double requiredWidth, bool fits) {
        _scale = scale; _style = style.Clone(); _values = values; _captions = captions;
        _gap = gap; _rowHeight = rowHeight; _headingHeight = headingHeight; Height = height; Fits = fits;
        RequiredWidth = requiredWidth;
    }

    internal static VisualBubbleSizeKeyScene Measure(VisualSceneBuilder builder, ChartBubbleSizeScale scale,
        TextStyle style, double gap, ChartRect viewport, bool compact) {
        var values = scale.MinimumValue == scale.MaximumValue ? new[] { scale.MinimumValue }
            : compact ? new[] { scale.MinimumValue, scale.MaximumValue }
            : new[] { scale.MinimumValue, ChartMath.InterpolateRange(scale.MinimumValue, scale.MaximumValue, .5), scale.MaximumValue };
        var captions = values.Select(scale.Presentation.Value).ToArray();
        var widths = values.Select((value, index) => scale.Radius(value, viewport) * 2 + 1 + gap + CaptionWidth(builder, captions[index], style)).ToArray();
        var row = Math.Max(builder.MeasureText("Mg", style).Height, values.Max(value => scale.Radius(value, viewport)) * 2 + 1);
        var heading = builder.MeasureText(scale.Presentation.Name, style);
        var requiredWidth = Math.Max(widths.Sum() + gap * Math.Max(0, values.Length - 1), Math.Ceiling(heading.Width) + 1);
        var fits = requiredWidth <= viewport.Width + .000001;
        return new VisualBubbleSizeKeyScene(scale, style, values, captions, gap, row, heading.Height,
            heading.Height + row + gap * 2, requiredWidth, fits);
    }

    /// <summary>Retains all descriptive facts while omitting a key whose complete circles and captions cannot fit.</summary>
    internal void Omit() => Height = 0;

    internal void Draw(VisualSceneBuilder builder, ChartRect plot) {
        var description = _scale.Presentation.Name + ": " + string.Join(", ", _captions);
        builder.AddRegion(new VisualSemanticRegion("bubble-size-key", "bubble-size-key", Bounds, description));
        var metadata = new Dictionary<string, string> {
            ["data-cfx-label-size"] = _scale.Presentation.Name,
            ["data-cfx-min-value"] = Number(_scale.MinimumValue), ["data-cfx-max-value"] = Number(_scale.MaximumValue),
            ["data-cfx-reversed"] = _scale.Reversed ? "true" : "false",
            ["data-cfx-size-mapping"] = _scale.Mapping, ["aria-label"] = description
        };
        using (builder.PushGroup("bubble-size-key", "bubble-size-key", metadata)) {
            if (Height <= 0) return;
            var title = new ChartRect(Bounds.Left, Bounds.Top, Bounds.Width, _headingHeight);
            VisualStateSceneTools.Text(builder, _scale.Presentation.Name, title, _style, "bubble-size-key-title", "bubble-size-key-title");
            var widths = _values.Select((value, index) => _scale.Radius(value, plot) * 2 + 1 + _gap + CaptionWidth(builder, _captions[index], _style)).ToArray();
            var x = Bounds.Left; var centerY = Bounds.Top + _headingHeight + _gap + _rowHeight / 2;
            for (var index = 0; index < _values.Length; index++) {
                var radius = _scale.Radius(_values[index], plot); var extent = radius + .5;
                var label = new ChartRect(x + extent * 2 + _gap, centerY - _rowHeight / 2,
                    Math.Max(0, widths[index] - extent * 2 - _gap), _rowHeight);
                using (builder.PushGroup("bubble-size-key-sample-" + index, "bubble-size-key-sample", new Dictionary<string, string> {
                    ["data-cfx-size-value"] = Number(_values[index]), ["data-cfx-radius"] = Number(radius), ["aria-label"] = _captions[index]
                })) {
                    if (radius > 0) builder.Ellipse(x + extent, centerY, radius, radius, null, _style.Color, 1,
                        "bubble-size-key-marker", paint: new VisualScenePaintBinding(stroke: VisualChartPaint.Text(_style)));
                    VisualStateSceneTools.Text(builder, _captions[index], label, _style, "bubble-size-key-label", "bubble-size-key-label-" + index);
                }
                x += widths[index] + _gap;
            }
        }
    }

    private static string Number(double value) => VisualStateSceneTools.Number(value);
    // Keep the measured caption's complete glyph run despite fractional subtraction at its final origin.
    private static double CaptionWidth(VisualSceneBuilder builder, string text, TextStyle style) => Math.Ceiling(builder.MeasureText(text, style).Width) + 1;
}
