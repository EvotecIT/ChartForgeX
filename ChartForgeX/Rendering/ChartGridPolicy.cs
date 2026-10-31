using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Design rule 2: gridlines follow the value axis. The default grid style means "value gridlines on, category guides off",
/// so a transposed (horizontal) chart draws vertical lines at its value ticks instead of rules through its bars.
/// A style that names both directions, or only the other one, is honoured literally.
/// </summary>
internal static class ChartGridPolicy {
    internal static (bool Vertical, bool Horizontal) Lines(ChartGridLineStyle style, bool horizontalValues) {
        var defaultShape = style.ShowHorizontalLines && !style.ShowVerticalLines;
        return horizontalValues && defaultShape ? (true, false) : (style.ShowVerticalLines, style.ShowHorizontalLines);
    }
}
