using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private const double TileHexInsetRatio = .22;
    /// <summary>Uses available cell spacing for complete captions before considering omissions.</summary>
    private static double TileGap(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartTileMapDefinition definition, ChartRect plot, double strokeWidth) {
        var gap = Math.Min(context.Theme.Spacing / 2, Math.Min(plot.Width / definition.ColumnCount, plot.Height / definition.RowCount) / 6);
        if (!chart.Options.ShowMapLabels) return gap;
        var style = TickStyle(chart, context);
        style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
        var requiredSize = 0d;
        foreach (var region in definition.Regions) {
            var metrics = builder.MeasureText(region.Code, style);
            // A centered rectangle must clear both slanted hexagon edges and the stroke.
            requiredSize = Math.Max(requiredSize, Math.Max(metrics.Height + strokeWidth,
                metrics.Width + strokeWidth + 2 * TileHexInsetRatio * (metrics.Height + strokeWidth)));
        }
        // Contour containment requires strict interior corners. Reserve a small logical margin
        // so the widest caption is not placed exactly on a slanted edge after rounding.
        requiredSize += .01;
        var horizontal = definition.ColumnCount < 2 ? double.PositiveInfinity
            : (plot.Width - strokeWidth - requiredSize * definition.ColumnCount) / (definition.ColumnCount - 1);
        var vertical = definition.RowCount < 2 ? double.PositiveInfinity
            : (plot.Height - strokeWidth - requiredSize * definition.RowCount) / (definition.RowCount - 1);
        return Math.Max(0, Math.Min(gap, Math.Min(horizontal, vertical)));
    }

    /// <summary>Fits complete tile captions above the readable minimum, preserving smaller authored requests.</summary>
    private static void TileLabel(VisualSceneBuilder builder, string code, ChartRect bounds, List<ChartPoint> contour,
        double clearance, TextStyle style, string id, SvgPaint paint) {
        const string role = "tile-map-label";
        builder.AddRegion(new VisualSemanticRegion(id, role, bounds, code));
        using (builder.PushGroup(id + "-source", role + "-source", new Dictionary<string, string> { ["aria-label"] = code, ["data-cfx-label"] = code })) {
            var shape = new LabelMarkShape(new[] { contour }, true, 0);
            style = style.Clone(); style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
            style.Alignment = TextAlignment.Center;
            if (!FitsCode()) {
                var preferred = style.FontSize;
                // Compact maps can fit complete codes at a smaller ordinary size, but must not
                // reduce them below readable native type. Explicit smaller requests remain authoritative.
                var minimum = Math.Min(VisualTextFitting.MinimumOrdinaryFontSize, preferred);
                style.FontSize = minimum;
                if (!FitsCode()) {
                    Omit();
                    return;
                }
                // A fixed search bounds work even for very large authored sizes. Quarter-pixel
                // steps retain the readable caption policy and stay within the measured contour.
                var lower = minimum; var upper = preferred;
                for (var step = 0; step < 16; step++) {
                    style.FontSize = lower + (upper - lower) / 2;
                    if (FitsCode()) lower = style.FontSize;
                    else upper = style.FontSize;
                }
                style.FontSize = Math.Max(minimum, Math.Floor(lower * 4) / 4);
            }
            var metrics = builder.MeasureText(code, style);
            builder.Text(code, bounds.Left + bounds.Width / 2,
                bounds.Top + (bounds.Height - metrics.Height) / 2 + builder.TextAscent(style), style, role, id, paint);

            void Omit() => builder.AddDiagnostic(new VisualDiagnostic("map.label-overflow", "A tile region code was omitted because it does not fit at a readable size; complete identity remains in descriptive regions."));
            bool FitsCode() => ChartTextFitting.FitEnd(code, Fits, "").Length == code.Length;
            bool Fits(string text) {
                var measured = builder.MeasureText(text, style);
                return shape.Contains(new ChartRect(bounds.Left + (bounds.Width - measured.Width) / 2 - clearance,
                    bounds.Top + (bounds.Height - measured.Height) / 2 - clearance,
                    measured.Width + clearance * 2, measured.Height + clearance * 2));
            }
        }
    }
}
