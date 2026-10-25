using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Captures the same actual primary and fallback glyph roots used by the native painter.</summary>
    internal VisualScenePreparedText PrepareText(string text, double size, int weight = 400) => Face(weight).Prepare(text, size);

    /// <summary>Replays an already shaped run at a uniform output scale without resolving or reshaping fonts.</summary>
    internal void PreparedText(VisualScenePreparedText original, double x, double baseline, double scale, ChartColor color,
        string? role = null, string? id = null, TextAlignment alignment = TextAlignment.Left,
        SvgPaint? paint = null, ChartColor? stroke = null, double strokeWidth = 0, SvgPaint? strokePaint = null) {
        ChartGuards.Finite(x, nameof(x)); ChartGuards.Finite(baseline, nameof(baseline)); VisualSize.Positive(scale, nameof(scale));
        var text = original;
        if (scale != 1) {
            var style = original.Style.Clone(); style.FontSize *= scale;
            var lines = Array.AsReadOnly(original.Lines.Select(line => new VisualSceneTextLine(line.Text, line.Width * scale, line.Glyphs)).ToArray());
            text = new VisualScenePreparedText(original.Face, style, original.Size * scale, original.Ascent * scale, lines,
                new TextMetrics(original.Metrics.Width * scale, original.Metrics.Height * scale, original.Metrics.LineHeight * scale));
        }
        _nodes.Add(new VisualSceneText(text, x, baseline, color, alignment, role, id, paint, stroke, strokeWidth, strokePaint));
    }
}
