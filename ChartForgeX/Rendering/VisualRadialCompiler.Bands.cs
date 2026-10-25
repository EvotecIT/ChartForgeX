using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    // Use a shared envelope only for newly supported multi-ring displacement or inner callouts.
    // Incumbent one-ring and undisplaced outer-caption compositions retain their exact compiler path.
    private static RadialBandFrame? ResolveBandFrame(Chart chart, VisualRenderContext context, ChartRect plot,
        IReadOnlyList<ChartRadialBand?> bands, int outerSeries) {
        if (chart.Series.Count < 2) return null;
        bool ActiveExternal(int index) => (chart.Series[index].ShowDataLabels ?? chart.Options.ShowDataLabels ?? false)
            && IsExternal(chart.Series[index].DataLabelPlacement ?? chart.Options.DataLabelPlacement);
        if (!chart.Series.Any(series => series.PointSliceOffsets.Any(offset => offset > 0))
            && !Enumerable.Range(0, chart.Series.Count).Any(index => index != outerSeries && ActiveExternal(index))) return null;
        var placements = Enumerable.Range(0, chart.Series.Count).Where(index => index == outerSeries || ActiveExternal(index))
            .Select(index => chart.Series[index].DataLabelPlacement ?? chart.Options.DataLabelPlacement).ToArray();
        var side = placements.Any(value => value is ChartDataLabelPlacement.Outside or ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right);
        var vertical = placements.Any(value => value is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below);
        var extentRatio = bands.Max(band => band!.OuterRatio);
        var colors = context.Theme.Resolve(context.ThemeMode);
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index]; var band = bands[index]!;
            foreach (var slice in GetSlices(chart, colors, index)) {
                var ratio = slice.RadiusRatio.GetValueOrDefault(1);
                if (slice.Value <= 0 || ratio <= 0) continue;
                var radius = ratio == 1 ? band.OuterRatio : band.InnerRatio + (band.OuterRatio - band.InnerRatio) * ratio;
                var offset = slice.PointIndex >= 0 && slice.PointIndex < series.PointSliceOffsets.Count
                    ? series.PointSliceOffsets[slice.PointIndex] : 0;
                extentRatio = Math.Max(extentRatio, radius * (1 + offset));
            }
        }
        var available = Math.Min(side ? plot.Width * .57 : plot.Width, vertical ? plot.Height * .76 : plot.Height) / 2 - context.Theme.Spacing;
        // Nominal ratios remain relative to the same unshifted full frame, including when all outer slices shrink.
        var radiusScale = Math.Max(0, available / Math.Max(1, extentRatio));
        return new RadialBandFrame(radiusScale, radiusScale * extentRatio);
    }

    private static bool IsExternal(ChartDataLabelPlacement placement) => placement is ChartDataLabelPlacement.Outside
        or ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;

    private static void DrawBandLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, RadialBandFrame frame) {
        if (frame.Labels.Count == 0) return;
        var gap = Math.Max(2, context.Theme.Spacing / 2);
        var requests = frame.Labels.Select(label => BandLabelRequest(chart, builder, plot, frame, label, gap)).ToArray();
        // Shared placement reserves straight source leaders against all captions in both placement orders.
        // Actual bent/curved paths are checked below through the same typed flattening/shape owner.
        var placed = new LabelPlacementService().Place(requests, plot, null, gap, builder.MeasureText, avoidLeaderLabels: true);
        var paths = new ChartPath?[placed.Count];
        var accepted = placed.Select(result => !result.IsDropped).ToArray();
        var haloWidth = chart.Options.DataLabelConnectorStrokeWidth + 2;
        for (var index = 0; index < placed.Count; index++) {
            if (!accepted[index] || !placed[index].HasLeaderLine) continue;
            var label = frame.Labels[index]; var result = placed[index]; var end = result.LeaderEnd;
            var vertical = label.Placement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
            // ConnectorPath's four-unit inset stops the painted stroke before the measured caption.
            var x = vertical ? end.X : end.X + (label.Left ? -4 : 4);
            var y = vertical ? end.Y + (label.Placement == ChartDataLabelPlacement.Above ? -4 : 4) : end.Y;
            var path = ConnectorPath(chart, label.Angle, label.SliceX, label.SliceY, label.Radius, x, y, true, vertical);
            var scenePath = new VisualScenePath(path, false, null, null, 0, null, null);
            var shape = new LabelMarkShape(VisualSceneGeometry.Flatten(scenePath, 1), false, haloWidth);
            if (!LabelPlacementService.Contains(plot, shape.Bounds)
                || Enumerable.Range(0, placed.Count).Any(other => other != index && accepted[other] && shape.Intersects(placed[other].Bounds))) {
                accepted[index] = false;
                continue;
            }
            paths[index] = path;
        }
        var backdrop = ChartStateMark.Backdrop(chart.Options, context.Theme.Resolve(context.ThemeMode), context.Frame);
        for (var index = 0; index < placed.Count; index++) {
            var result = placed[index]; var label = frame.Labels[index]; var id = SliceId(label.Slice);
            var metadata = VisualMarkLabel.Metadata(label.Text, id);
            metadata["data-cfx-series"] = label.Slice.SeriesIndex.ToString(CultureInfo.InvariantCulture);
            metadata["data-cfx-point"] = label.Slice.PointIndex.ToString(CultureInfo.InvariantCulture);
            metadata["data-cfx-radial-callout"] = "overlay";
            metadata["data-cfx-passive-overlay"] = "true"; // Overlay ink must not replace the native sector's hit target.
            metadata["data-cfx-label-dropped"] = accepted[index] ? "false" : "true";
            if (accepted[index]) {
                metadata["data-cfx-label-left"] = N(result.Bounds.Left); metadata["data-cfx-label-top"] = N(result.Bounds.Top);
                metadata["data-cfx-label-width"] = N(result.Bounds.Width); metadata["data-cfx-label-height"] = N(result.Bounds.Height);
            }
            using (builder.PushGroup(id + "-label-source", "radial-data-label-source", metadata)) {
                if (!accepted[index] || result.IsEllipsized)
                    builder.AddDiagnostic(new VisualDiagnostic("radial.label-overflow", "A multi-ring caption or its leader could not fit without covering another caption; complete text and source association remain available."));
                if (!accepted[index]) continue;
                if (paths[index] is ChartPath path) {
                    var color = ChartColorMath.WithOpacity(chart.Options.DataLabelConnectorColor ?? label.Slice.Color, chart.Options.DataLabelConnectorOpacity);
                    // An overlay must remain distinguishable while crossing enclosing occupied bands.
                    builder.Path(path, stroke: backdrop, strokeWidth: haloWidth, role: "data-label-connector-halo", id: id + "-connector-halo", paint: VisualChartPaint.Stroke(SvgPaint.Literal(backdrop)));
                    builder.Path(path, stroke: color, strokeWidth: chart.Options.DataLabelConnectorStrokeWidth, role: "data-label-connector", id: id + "-connector", paint: ConnectorPaint(chart, label.Slice, color));
                }
                var style = result.Request.Style.Clone(); style.FontSize = style.EffectiveFontSize;
                style.Baseline = TextBaseline.Normal; style.Alignment = TextAlignment.Left; style.TextCase = TextCaseTransform.None;
                builder.Text(result.Text, result.Bounds.Left, result.Bounds.Top + builder.TextAscent(style), style,
                    "data-label", id + "-label", paint: VisualChartPaint.Text(style));
            }
        }
    }

    private static LabelPlacementRequest BandLabelRequest(Chart chart, VisualSceneBuilder builder, ChartRect plot,
        RadialBandFrame frame, RadialLabel label, double gap) {
        var cx = plot.Left + plot.Width / 2; var cy = plot.Top + plot.Height / 2;
        var vertical = label.Placement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
        var above = label.Placement == ChartDataLabelPlacement.Above;
        var sideX = label.Left ? cx - frame.Extent - gap : cx + frame.Extent + gap;
        var railY = above ? cy - frame.Extent - gap : cy + frame.Extent + gap;
        var band = vertical ? new ChartRect(plot.Left, above ? plot.Top : Math.Min(plot.Bottom, railY), plot.Width,
            Math.Max(0, above ? railY - plot.Top : plot.Bottom - railY))
            : new ChartRect(label.Left ? plot.Left : Math.Min(plot.Right, sideX), plot.Top,
                Math.Max(0, label.Left ? sideX - plot.Left : plot.Right - sideX), plot.Height);
        var style = FitLabelStyle(builder, label.Text, label.Style, band.Width, band.Height);
        var measured = builder.MeasureText(label.Text, style);
        var anchor = new ChartPoint(label.SliceX + Math.Cos(label.Angle) * label.Radius, label.SliceY + Math.Sin(label.Angle) * label.Radius);
        var targetX = vertical ? Math.Max(band.Left + measured.Width / 2, Math.Min(band.Right - measured.Width / 2, label.X))
            : label.Left ? Math.Min(band.Right, label.X) : Math.Max(band.Left, label.X);
        var targetY = vertical ? above ? band.Bottom : band.Top
            : Math.Max(band.Top + measured.Height / 2, Math.Min(band.Bottom - measured.Height / 2, label.Y));
        var candidates = new List<LabelCandidate>();
        void Candidate(double shift) => candidates.Add(new LabelCandidate(targetX + (vertical ? shift : 0) - anchor.X,
            targetY + (vertical ? 0 : shift) - anchor.Y, vertical ? .5 : label.Left ? 1 : 0, vertical ? above ? 1 : 0 : .5));
        Candidate(0);
        var step = Math.Max(1, (vertical ? measured.Width : measured.Height) + gap);
        for (var shift = 1; shift <= frame.Labels.Count; shift++) { Candidate(shift * step); Candidate(-shift * step); }
        var color = ChartColorMath.WithOpacity(chart.Options.DataLabelConnectorColor ?? label.Slice.Color, chart.Options.DataLabelConnectorOpacity);
        return new LabelPlacementRequest(label.Text, anchor, style, candidates) {
            Bounds = band, MeasuredSize = measured, AssociatedMarkId = SliceId(label.Slice),
            HasLeaderLine = chart.Options.DataLabelConnectorStrokeWidth > 0 && color.A > 0, LeaderOnFirstCandidate = true
        };
    }

    private static string N(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private sealed class RadialBandFrame {
        internal RadialBandFrame(double radius, double extent) { Radius = radius; Extent = extent; }
        internal double Radius { get; }
        internal double Extent { get; }
        internal List<RadialLabel> Labels { get; } = new();
    }
}
