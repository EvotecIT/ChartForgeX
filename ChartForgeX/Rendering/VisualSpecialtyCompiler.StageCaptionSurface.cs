using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    /// <summary>Captures actual stage/connection surfaces; shared contour and contrast owners resolve the placed caption.</summary>
    private sealed class StageCaptionSurface {
        private readonly ChartColor _frame;
        private readonly List<SvgPaint> _framePaints = new();
        private readonly List<StageCaptionLayer> _layers = new();
        internal StageCaptionSurface(VisualRenderContext context) {
            var colors = context.Theme.Resolve(context.ThemeMode);
            // As with other prepared captions, a transparent export assumes its configured theme background.
            // An unknown external host surface cannot be inferred from chart data.
            var frame = ChartColorMath.CompositeOverOpaque(colors.Background, ChartColor.White);
            _framePaints.Add(SvgPaint.Of(colors.Background, SvgColorRole.Surface));
            void Over(ChartColor color) {
                frame = ChartColorMath.CompositeOverOpaque(color, frame);
                _framePaints.Add(SvgPaint.Of(color, SvgColorRole.Surface));
            }
            if (context.Frame.ShowCard) Over(colors.ElevatedSurface);
            if (context.Frame.ShowSurface) Over(colors.Surface);
            _frame = frame;
        }

        internal void Add(ChartPath path, ChartColor fill, SvgPaint paint, bool patterned) {
            if (fill.A == 0 && !patterned) return;
            _layers.Add(new StageCaptionLayer(Shape(path, true, 0), fill, paint, patterned));
        }
        internal void AddStroke(ChartPath path, double width, bool closed = false) {
            if (width > 0) _layers.Add(new StageCaptionLayer(Shape(path, false, width, closed), null, null, false));
        }

        internal StageCaptionInk Filled(ChartRect bounds, ChartPath path, ChartColor fill, SvgPaint paint, bool patterned) {
            if (patterned || !Shape(path, true, 0).Contains(bounds)) return Halo();
            return Ink(ChartColorMath.CompositeOverOpaque(fill, _frame), new List<SvgPaint>(_framePaints) { paint });
        }
        internal StageCaptionInk Resolve(ChartRect bounds, StageCaptionFrame? unfilledStage) {
            var surface = _frame; var paints = new List<SvgPaint>(_framePaints); var mixed = false;
            foreach (var layer in _layers) {
                if (!layer.Shape.Intersects(bounds)) continue;
                if (!layer.Fill.HasValue || layer.Patterned || !layer.Shape.Contains(bounds)) { mixed = true; continue; }
                var fill = layer.Fill.Value;
                if (fill.A == 255) mixed = false; // A later fully covering opaque layer replaces earlier surface variation.
                surface = ChartColorMath.CompositeOverOpaque(fill, surface); paints.Add(layer.Paint!.Value);
            }
            if (unfilledStage != null && unfilledStage.StrokeWidth > 0
                && Shape(unfilledStage.Path, false, unfilledStage.StrokeWidth).Intersects(bounds)) mixed = true;
            return mixed ? Halo() : Ink(surface, paints);
        }

        private StageCaptionInk Ink(ChartColor surface, IReadOnlyList<SvgPaint> paints) {
            var ink = ChartColorMath.AccessibleTextOnBackground(surface);
            return new StageCaptionInk(ink, SvgPaint.ContrastOver(ink, SvgPaint.Of(ink, SvgColorRole.Text), paints), null, null);
        }
        private StageCaptionInk Halo() {
            var ink = ChartColorMath.AccessibleTextOnBackground(_frame);
            var halo = ChartColorMath.AccessibleTextOnBackground(ink);
            // A mixed/patterned surface has no single fill operand. Keep complementary opaque glyph/halo paints
            // literal in both backends, rather than claiming one adaptive contrast token describes that surface.
            return new StageCaptionInk(ink, SvgPaint.Literal(ink), halo, SvgPaint.Literal(halo));
        }
        private static LabelMarkShape Shape(ChartPath path, bool filled, double stroke, bool closed = false) => new(
            VisualSceneGeometry.Flatten(new VisualScenePath(path, filled || closed, null, null, 0, null, null), 1), filled, stroke);
    }

    private sealed class StageCaptionLayer {
        internal StageCaptionLayer(LabelMarkShape shape, ChartColor? fill, SvgPaint? paint, bool patterned) {
            Shape = shape; Fill = fill; Paint = paint; Patterned = patterned;
        }
        internal LabelMarkShape Shape { get; }
        internal ChartColor? Fill { get; }
        internal SvgPaint? Paint { get; }
        internal bool Patterned { get; }
    }
    private readonly struct StageCaptionInk {
        internal StageCaptionInk(ChartColor color, SvgPaint paint, ChartColor? halo, SvgPaint? haloPaint) {
            Color = color; Paint = paint; Halo = halo; HaloPaint = haloPaint;
        }
        internal ChartColor Color { get; }
        internal SvgPaint Paint { get; }
        internal ChartColor? Halo { get; }
        internal SvgPaint? HaloPaint { get; }
    }
}
