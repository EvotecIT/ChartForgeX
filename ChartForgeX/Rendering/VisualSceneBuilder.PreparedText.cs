using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Captures the existing text owner once for a producer that subsequently places unchanged shaped runs.</summary>
    internal VisualScenePreparedText PrepareText(string text, TextStyle style) =>
        StyleFace(style ?? throw new ArgumentNullException(nameof(style))).Prepare(text, style.EffectiveFontSize);

    /// <summary>Places already captured text without font resolution, shaping or mutation, optionally adding its captured native decorations.</summary>
    internal void PreparedText(VisualScenePreparedText text, double x, double baseline, ChartColor color,
        TextAlignment alignment, string? role = null, string? id = null, SvgPaint? paint = null, bool includeDecorations = false) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        ChartGuards.Finite(x, nameof(x)); ChartGuards.Finite(baseline, nameof(baseline));
        if (!Enum.IsDefined(typeof(TextAlignment), alignment)) throw new ArgumentOutOfRangeException(nameof(alignment));
        _nodes.Add(new VisualSceneText(text, x, baseline, color, alignment, role, id, paint));
        if (includeDecorations) {
            var style = text.Style.Clone(); style.Alignment = alignment; style.Color = color;
            AddTextDecorations(text, x, baseline, style, paint);
        }
    }
}
