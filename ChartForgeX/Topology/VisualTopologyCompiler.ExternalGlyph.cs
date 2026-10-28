using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void ValidateExternalGlyphSlots() {
        foreach (var node in _chart.Nodes) {
            if (!node.GlyphScale.HasValue) continue;
            var plan = TopologyGlyphBounds.Measure(node, _options, _context.Theme.AxisStrokeWidth, _context.Theme.BarRadius);
            var ink = plan.Ink; var allowance = plan.StrokeOverflow + 1e-6;
            if (!Finite(ink.Left) || !Finite(ink.Top) || !Finite(ink.Right) || !Finite(ink.Bottom) || !Finite(allowance)
                || ink.Left < node.X - allowance || ink.Top < node.Y - allowance
                || ink.Right > node.X + node.Width + allowance || ink.Bottom > node.Y + node.Height + allowance)
                throw new InvalidOperationException("Node '" + node.Id + "' scaled native glyph ink exceeds its authored body slot. Increase the preserved body dimensions or reduce GlyphScale.");
        }
        static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private ChartRect IncludeExternalGlyphBounds(ChartRect content) {
        foreach (var node in _chart.Nodes) {
            if (!node.GlyphScale.HasValue) continue;
            var ink = TopologyGlyphBounds.Measure(node, _options, _context.Theme.AxisStrokeWidth, _context.Theme.BarRadius).Ink;
            var left = Math.Min(content.Left, ink.Left); var top = Math.Min(content.Top, ink.Top);
            var right = Math.Max(content.Right, ink.Right); var bottom = Math.Max(content.Bottom, ink.Bottom);
            content = new ChartRect(left, top, right - left, bottom - top);
        }
        return content;
    }
}
