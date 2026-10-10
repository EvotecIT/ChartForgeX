using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private readonly struct FinancialMarkStyle {
        internal FinancialMarkStyle(ChartColor? fill, SvgPaint fillPaint, ChartColor stroke, SvgPaint strokePaint,
            double strokeWidth, ChartColor wick, SvgPaint wickPaint, double wickWidth) {
            Fill = fill; FillPaint = fillPaint; Stroke = stroke; StrokePaint = strokePaint;
            StrokeWidth = strokeWidth; Wick = wick; WickPaint = wickPaint; WickWidth = wickWidth;
        }
        internal readonly ChartColor? Fill;
        internal readonly SvgPaint FillPaint, StrokePaint, WickPaint;
        internal readonly ChartColor Stroke, Wick;
        internal readonly double StrokeWidth, WickWidth;
    }

    private static FinancialMarkStyle FinancialStyle(ChartSeries series, int index, int item, bool rising, VisualThemeColors colors) {
        var options = rising ? series.Financial.Rising : series.Financial.Falling;
        var fallback = FinancialColor(series, index, item, rising, colors);
        var fallbackRole = SemanticMarkPaintRole(series, item);
        var point = item >= 0 && item < series.PointColors.Count ? series.PointColors[item] : null;
        (ChartColor Color, SvgPaint Paint) Paint(ChartColor? authored, double opacity) {
            var color = point ?? authored ?? fallback;
            var source = SvgPaint.Of(color, point.HasValue || authored.HasValue ? SvgColorRole.Series : fallbackRole);
            var resolved = ChartColorMath.WithOpacity(color, opacity);
            return (resolved, opacity == 1 ? source : source.WithOpacity(resolved, opacity));
        }
        var fillOpacity = options.FillOpacity ?? (rising ? ChartVisualPrimitives.CandlestickRisingFillOpacity : ChartVisualPrimitives.CandlestickFallingFillOpacity);
        var fill = Paint(options.Fill, fillOpacity);
        var stroke = Paint(options.Stroke, options.StrokeOpacity ?? 1);
        var wick = Paint(options.Wick.Stroke ?? options.Stroke, options.Wick.StrokeOpacity ?? options.StrokeOpacity ?? 1);
        var width = options.StrokeWidth ?? (series.HasExplicitStrokeWidth ? series.StrokeWidth
            : series.Kind == ChartSeriesKind.Candlestick ? ChartVisualPrimitives.CandlestickStrokeWidth : ChartVisualPrimitives.OhlcStrokeWidth);
        return new FinancialMarkStyle(fill.Color.A == 0 ? null : fill.Color, fill.Paint,
            stroke.Color, stroke.Paint, width, wick.Color, wick.Paint, options.Wick.StrokeWidth ?? width);
    }

    private static ChartRect FinancialBody(double x, double width, double openY, double closeY) {
        var height = Math.Max(2, Math.Abs(closeY - openY));
        return new ChartRect(x - width / 2, (openY + closeY - height) / 2, width, height);
    }

    // Round caps must end outside the hollow interior, including for a two-pixel doji body.
    private static void FinancialWicks(ChartRect body, double highY, double lowY, FinancialMarkStyle style, Action<double, double> line) {
        if (style.WickWidth == 0 || style.Wick.A == 0) return;
        if (style.Fill.HasValue) { line(highY, lowY); return; }
        var top = Math.Min(highY, lowY); var bottom = Math.Max(highY, lowY);
        var upperEnd = Math.Min(bottom, body.Top - style.WickWidth / 2);
        var lowerStart = Math.Max(top, body.Bottom + style.WickWidth / 2);
        if (upperEnd > top) line(top, upperEnd);
        if (bottom > lowerStart) line(lowerStart, bottom);
    }

    private static ChartRect FinancialBounds(bool candle, ChartRect body, double highY, double lowY, double openY, double closeY, FinancialMarkStyle style) {
        ChartRect? painted = null;
        void Include(ChartRect next) {
            painted = painted.HasValue ? Extents(Math.Min(painted.Value.Left, next.Left), Math.Min(painted.Value.Top, next.Top),
                Math.Max(painted.Value.Right, next.Right), Math.Max(painted.Value.Bottom, next.Bottom)) : next;
        }
        void Line(double left, double top, double right, double bottom, double width) => Include(Extents(left, top, right, bottom, width / 2));
        var x = body.Left + body.Width / 2;
        if (candle) {
            if (style.Fill.HasValue) Include(body);
            if (style.StrokeWidth > 0 && style.Stroke.A > 0) Include(Extents(body.Left, body.Top, body.Right, body.Bottom, style.StrokeWidth / 2));
            FinancialWicks(body, highY, lowY, style, (from, to) => Line(x, from, x, to, style.WickWidth));
        } else if (style.StrokeWidth > 0 && style.Stroke.A > 0) {
            Line(x, highY, x, lowY, style.StrokeWidth);
            Line(body.Left, openY, x, openY, style.StrokeWidth);
            Line(x, closeY, body.Right, closeY, style.StrokeWidth);
        }
        // Hiding all ink retains a source target and its metadata rather than dropping an observation.
        return painted ?? Extents(body.Left, Math.Min(highY, lowY), body.Right, Math.Max(highY, lowY));
    }

    private static void DrawFinancialMark(VisualSceneBuilder builder, bool candle, ChartRect body, double highY, double lowY,
        double openY, double closeY, FinancialMarkStyle style, ChartFillPattern pattern, ChartColor backdrop, bool legend = false, double radiusScale = 1) {
        var x = body.Left + body.Width / 2;
        var prefix = legend ? "legend-financial" : candle ? "candlestick" : "ohlc";
        if (candle) {
            FinancialWicks(body, highY, lowY, style, (from, to) => builder.Line(x, from, x, to, style.Wick,
                style.WickWidth, role: prefix + "-wick", paint: VisualChartPaint.Stroke(style.WickPaint)));
            var radius = Math.Min(ChartVisualPrimitives.CandlestickBodyRadius * radiusScale, body.Height / 2);
            builder.Rect(body, style.Fill, style.StrokeWidth > 0 ? style.Stroke : null, style.StrokeWidth, radius, role: prefix + "-body",
                paint: new VisualScenePaintBinding(fill: style.Fill.HasValue ? style.FillPaint : null,
                    stroke: style.StrokeWidth > 0 ? style.StrokePaint : null));
            if (style.Fill.HasValue) DrawPattern(builder, RoundedRectanglePath(body, radius), pattern, style.Fill.Value, backdrop, prefix + "-pattern");
        } else if (style.StrokeWidth > 0) {
            var paint = VisualChartPaint.Stroke(style.StrokePaint);
            builder.Line(x, highY, x, lowY, style.Stroke, style.StrokeWidth, role: prefix + "-stem", paint: paint);
            builder.Line(body.Left, openY, x, openY, style.Stroke, style.StrokeWidth, role: prefix + "-open", paint: paint);
            builder.Line(x, closeY, body.Right, closeY, style.Stroke, style.StrokeWidth, role: prefix + "-close", paint: paint);
        }
    }

    private static Action<VisualSceneBuilder, ChartRect, VisualRenderContext>? FinancialLegend(Chart chart, ChartSeries series,
        int index, VisualThemeColors colors, int point = -1, ChartFillPattern pattern = ChartFillPattern.None) {
        if (!series.Financial.IsConfigured || series.Kind is not (ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc)) return null;
        var candle = series.Kind == ChartSeriesKind.Candlestick;
        var rising = point < 0 || series.Points[point * 4 + 3].Y >= series.Points[point * 4].Y;
        var first = FinancialStyle(series, index, point, rising, colors);
        var second = FinancialStyle(series, index, -1, false, colors);
        return (builder, bounds, context) => {
            var backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
            void Glyph(ChartRect slot, FinancialMarkStyle style, bool up) {
                var stroke = Math.Max(style.StrokeWidth, candle ? style.WickWidth : 0);
                var scale = Math.Min(slot.Width / (12 + stroke), slot.Height / (36 + stroke));
                var scaled = new FinancialMarkStyle(style.Fill, style.FillPaint, style.Stroke, style.StrokePaint,
                    style.StrokeWidth * scale, style.Wick, style.WickPaint, style.WickWidth * scale);
                var x = slot.Left + slot.Width / 2; var y = slot.Top + slot.Height / 2;
                var open = y + (up ? 8 : -8) * scale; var close = y - (up ? 8 : -8) * scale;
                DrawFinancialMark(builder, candle, FinancialBody(x, 12 * scale, open, close), y - 18 * scale, y + 18 * scale,
                    open, close, scaled, pattern, backdrop, legend: true, radiusScale: scale);
            }
            if (point >= 0) Glyph(bounds, first, rising);
            else {
                Glyph(new ChartRect(bounds.Left, bounds.Top, bounds.Width / 2, bounds.Height), first, true);
                Glyph(new ChartRect(bounds.Left + bounds.Width / 2, bounds.Top, bounds.Width / 2, bounds.Height), second, false);
            }
        };
    }
}
