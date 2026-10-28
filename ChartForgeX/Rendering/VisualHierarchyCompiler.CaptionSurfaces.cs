using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    /// <summary>Retains the frame and actually painted hierarchy ancestry for translucent automatic captions.</summary>
    private sealed class HierarchyCaptionSurface {
        private HierarchyCaptionSurface(ChartColor color, IReadOnlyList<SvgPaint> paints) {
            Color = color; Paints = paints;
        }
        private ChartColor Color { get; }
        private IReadOnlyList<SvgPaint> Paints { get; }
        internal ChartColor Ink => ChartColorMath.AccessibleTextOnBackground(Color);

        internal static HierarchyCaptionSurface Frame(VisualRenderContext context) {
            var colors = context.Theme.Resolve(context.ThemeMode);
            // Transparent standalone output uses the configured background as its fallback,
            // just as the native frame; an external host's unknown backdrop is not inferred.
            var surface = new HierarchyCaptionSurface(ChartColorMath.CompositeOverOpaque(colors.Background, ChartColor.White),
                new[] { SvgPaint.Of(colors.Background, SvgColorRole.Surface) });
            if (context.Frame.ShowCard) surface = surface.Over(colors.ElevatedSurface, SvgPaint.Of(colors.ElevatedSurface, SvgColorRole.Surface));
            if (context.Frame.ShowSurface) surface = surface.Over(colors.Surface, SvgPaint.Of(colors.Surface, SvgColorRole.Surface));
            return surface;
        }

        internal HierarchyCaptionSurface Over(ChartColor color, SvgPaint paint) {
            var layers = new List<SvgPaint>(Paints);
            // Retain one overflow sentinel so the existing contrast owner falls back to exact native ink.
            if (layers.Count <= 32) layers.Add(paint);
            return new HierarchyCaptionSurface(ChartColorMath.CompositeOverOpaque(color, Color), layers);
        }

        internal SvgPaint Paint(ChartColor fill, SvgPaint fillPaint, ChartColor ink) => fill.A == 255
            ? SvgPaint.Contrast(fill, fillPaint) // Preserve incumbent opaque paint bytes.
            : SvgPaint.ContrastOver(ink, SvgPaint.Of(ink, SvgColorRole.Text), Paints);
    }
}
