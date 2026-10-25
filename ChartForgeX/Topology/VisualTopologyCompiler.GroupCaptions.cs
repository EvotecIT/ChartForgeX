using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    // The frame's background is the standalone fallback for transparent output. An opaque
    // authored ancestor completely removes dependence on that fallback beneath descendants.
    private GroupCaptionSurface GroupCaptionBaseSurface() {
        var surface = new GroupCaptionSurface(_colors.Background, new[] { BackgroundPaint(_colors.Background) }, false);
        if (_context.Frame.ShowCard) surface = surface.Over(_colors.ElevatedSurface,
            SourcePaint(_svgTheme.Surface, _colors.ElevatedSurface, SvgColorRole.Surface, fallback: _colors.ElevatedSurface), false);
        if (_options.CanvasSurfaceStyle != TopologyCanvasSurfaceStyle.Plain)
            surface = surface.Over(_colors.Surface, CardPaint(_colors.Surface), false);
        return surface;
    }

    private static ChartColor GroupCaptionInk(GroupCaptionSurface surface, ChartColor preferred) =>
        !surface.Authored || ChartColorMath.ContrastRatio(preferred, surface.Color) >= 4.5
            ? preferred : ChartColorMath.AccessibleTextOnBackground(surface.Color);

    private SvgPaint? GroupCaptionPaint(GroupCaptionSurface surface, ChartColor ink, bool active, bool secondary) {
        if (!surface.Authored) return secondary ? MutedPaint(ink, HighlightFactor(active)) : (SvgPaint?)null;
        var preferred = secondary ? MutedPaint(Highlight(_colors.MutedForeground, active), HighlightFactor(active))
            : ForegroundPaint(Highlight(_colors.Foreground, active), HighlightFactor(active));
        return SvgPaint.ContrastOver(ink, preferred, surface.Paints);
    }

    private sealed class GroupCaptionSurface {
        internal GroupCaptionSurface(ChartColor color, IReadOnlyList<SvgPaint> paints, bool authored) {
            Color = ChartColorMath.CompositeOverOpaque(color, ChartColor.FromRgb(255, 255, 255));
            Paints = paints; Authored = authored;
        }
        internal ChartColor Color { get; }
        internal IReadOnlyList<SvgPaint> Paints { get; }
        internal bool Authored { get; }
        internal GroupCaptionSurface Over(ChartColor color, SvgPaint paint, bool authored) {
            // Keep ancestors even under an opaque fallback: a bound variable may change alpha.
            // A sentinel overflow count makes the shared paint owner use its exact literal fallback.
            var paints = new List<SvgPaint>(Paints);
            if (paints.Count <= 32) paints.Add(paint);
            return new GroupCaptionSurface(ChartColorMath.CompositeOverOpaque(color, Color), paints, Authored || authored);
        }
    }
}
