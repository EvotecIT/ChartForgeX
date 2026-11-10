using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>
/// One Cartesian axis's resolved grid: its own style over the chart grid policy, with alternating bands and minor lines.
/// An axis without its own style or minor ticks paints exactly the chart grid lines.
/// </summary>
internal readonly struct CartesianAxisGrid {
    private readonly ChartAxis _axis;
    private readonly ChartColor _color;
    private readonly SvgPaint _paint;
    private readonly double _width;
    private readonly double[]? _dash;
    private readonly double _opacity;
    private readonly double _minorOpacity;
    private readonly ChartColor? _bandFill;
    private readonly VisualScenePaintBinding? _bandPaint;

    internal CartesianAxisGrid(ChartAxis axis, bool chartLines, double chartOpacity, double chartWidth, double[]? chartDash, VisualThemeColors colors) {
        _axis = axis;
        var style = axis.Grid;
        Lines = style?.ShowLines ?? chartLines;
        _opacity = style?.Opacity ?? chartOpacity;
        _width = style?.StrokeWidth ?? chartWidth;
        _dash = style?.Dash is double dash ? dash > 0 && style.Gap > 0 ? new[] { dash, style.Gap!.Value } : null : chartDash;
        _color = style?.Color ?? colors.Grid;
        _paint = style?.Color is { } authored ? SvgPaint.Literal(authored) : SvgPaint.Of(colors.Grid, SvgColorRole.Grid);
        _minorOpacity = style?.MinorOpacity ?? MinorOpacity(_opacity, _color, colors.Background);
        _bandFill = null; _bandPaint = null;
        if (style?.ShowBands == true) {
            // Bands default to a neutral tint of the theme grid ink, so they read as rhythm rather than a highlighted range.
            var band = style.BandColor ?? colors.Grid;
            var opacity = style.BandOpacity ?? (style.BandColor.HasValue ? .08 : .5);
            _bandFill = ChartColorMath.WithOpacity(band, opacity);
            _bandPaint = VisualChartPaint.Fill((style.BandColor.HasValue ? SvgPaint.Literal(band) : SvgPaint.Of(band, SvgColorRole.Grid)).WithOpacity(_bandFill.Value, opacity));
        }
    }

    /// <summary>
    /// Half the major line's lightness step from the plot background, in CIE L*. A pale light-theme grid sits only a few
    /// L* units from its page, where half of that vanishes into anti-aliasing, so minor ink keeps a just-visible floor of
    /// 3.5 L* (never above 80% of the major step). Dark themes, whose grid sits further from the page, keep exactly half.
    /// </summary>
    private static double MinorOpacity(double major, ChartColor grid, ChartColor background) {
        var span = Math.Abs(ChartColorMath.Lightness(grid) - ChartColorMath.Lightness(background));
        if (!(span > 0)) return major / 2;
        var step = major * span;
        return Math.Max(step / 2, Math.Min(step * .8, 3.5)) / span;
    }


    /// <summary>Gets whether this axis draws major and minor grid lines.</summary>
    internal bool Lines { get; }

    /// <summary>Paints one major grid line.</summary>
    internal void Line(VisualSceneBuilder builder, double x1, double y1, double x2, double y2, string role) => Stroke(builder, x1, y1, x2, y2, role, _opacity);

    /// <summary>Paints the bands and minor lines that sit beneath this axis's major lines. Horizontal grid lines belong to a
    /// vertical axis; source X admission keeps prepared X zoom tracks aligned.</summary>
    internal void Underlay(VisualSceneBuilder builder, IReadOnlyList<double> ticks, double minimum, double maximum,
        Func<double, double> map, ChartRect plot, bool horizontal, string role, bool sourceX) {
        if (_bandFill is { } fill) {
            for (var index = 0; index + 1 < ticks.Count; index += 2) {
                using var source = sourceX ? builder.PushSourceX(CartesianXSourceAdmission.Range(ticks[index], ticks[index + 1])) : null;
                var start = map(ticks[index]); var end = map(ticks[index + 1]);
                var bounds = horizontal ? new ChartRect(plot.Left, Math.Min(start, end), plot.Width, Math.Abs(end - start))
                    : new ChartRect(Math.Min(start, end), plot.Top, Math.Abs(end - start), plot.Height);
                if (bounds.Width > 0 && bounds.Height > 0) builder.Rect(bounds, fill, role: role + "-band", paint: _bandPaint);
            }
        }
        if (!Lines || _axis.MinorTickCount == 0) return;
        foreach (var value in ChartAxisTickPolicy.Minor(_axis, ticks, minimum, maximum)) {
            using var source = sourceX ? builder.PushSourceX(CartesianXSourceAdmission.Point(value)) : null;
            var position = map(value);
            if (horizontal) Stroke(builder, plot.Left, position, plot.Right, position, role + "-minor", _minorOpacity);
            else Stroke(builder, position, plot.Top, position, plot.Bottom, role + "-minor", _minorOpacity);
        }
    }

    private void Stroke(VisualSceneBuilder builder, double x1, double y1, double x2, double y2, string role, double opacity) =>
        builder.Line(x1, y1, x2, y2, ChartColorMath.WithOpacity(_color, opacity), _width, role: role, dash: _dash,
            paint: VisualChartPaint.Stroke(_paint.WithOpacity(ChartColorMath.WithOpacity(_color, opacity), opacity)));
}
