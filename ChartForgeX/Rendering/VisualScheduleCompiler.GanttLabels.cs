using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private const double PackedGanttCaptionPadding = 1;

    /// <summary>Uses spare row height for feasible authored text while leaving impossible captions to the omission policy.</summary>
    private static double GanttLaneCaptionBand(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartGanttLaneModel model, ChartRect plot, ChartRect projection) {
        var minimum = 0d; var maximum = model.MaximumBand(plot);
        foreach (var row in model.Rows.Where(row => !row.IsGroup)) {
            var series = chart.Series[row.SeriesIndex];
            if (!series.ResolveDataLabels(chart.Options, defaultVisible: true)) continue;
            var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
            if (placement is not (ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside)) continue;
            foreach (var placed in row.Items) {
                if (string.IsNullOrWhiteSpace(placed.Item.Label) || !model.TrySpan(placed, projection, out _, out _)) continue;
                var style = VisualStateSceneTools.DataStyle(chart, context, series, placed.PointIndex, ChartColor.Black);
                var height = builder.MeasureText(placed.Item.Label!, style).Height + PackedGanttCaptionPadding * 2;
                if (height <= maximum) minimum = Math.Max(minimum, height);
            }
        }
        return minimum;
    }

    /// <summary>Places packed captions against every final bar and visible group band before any item is painted.</summary>
    private static Dictionary<string, PackedGanttItem> GanttLaneLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartGanttLaneModel model, ChartRect plot, ChartRect projection, double[] tops, double minimumCaptionBand) {
        var band = model.Band(plot, minimumCaptionBand);
        var items = new Dictionary<string, PackedGanttItem>();
        var obstacles = new List<LabelObstacle>();
        var requests = new List<LabelPlacementRequest>();
        var requested = new List<PackedGanttItem>();
        var colors = context.Theme.Resolve(context.ThemeMode);
        for (var rowIndex = 0; rowIndex < model.Rows.Count; rowIndex++) {
            var row = model.Rows[rowIndex];
            if (row.IsGroup) {
                if (row.Name.Length > 0 && chart.Options.ShowAxes && chart.Options.YAxis.Visible)
                    obstacles.Add(new LabelObstacle("gantt-group-" + rowIndex, model.RowBounds(plot, rowIndex, tops)));
                continue;
            }
            var series = chart.Series[row.SeriesIndex];
            foreach (var placed in row.Items) {
                var visible = model.TrySpan(placed, projection, out var left, out var width);
                var top = visible ? model.BarTop(plot, tops[rowIndex], placed.SubRow, minimumCaptionBand) : model.RowBounds(plot, rowIndex, tops).Top;
                var bounds = new ChartRect(visible ? left : plot.Left, top,
                    visible ? Math.Max(0, Math.Min(width, plot.Right - left)) : 0, Math.Max(0, band));
                var id = VisualStateSceneTools.SourceId(row.SeriesIndex, placed.PointIndex);
                var item = new PackedGanttItem(id, bounds, visible);
                items.Add(id, item);
                if (!visible || bounds.Width <= 0 || bounds.Height <= 0) continue;
                obstacles.Add(new LabelObstacle(id, bounds));
                if (!series.ResolveDataLabels(chart.Options, defaultVisible: true) || string.IsNullOrWhiteSpace(placed.Item.Label)) continue;
                var style = VisualStateSceneTools.DataStyle(chart, context, series, placed.PointIndex,
                    ChartColorMath.AccessibleTextOnBackground(colors.Surface));
                item.Placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
                // A thin task needs an outside lane. Give it first use before a caption
                // that fits across its own bar takes the only clear neighboring space.
                item.Priority = builder.MeasureText(placed.Item.Label!, style).Width + PackedGanttCaptionPadding * 2 > bounds.Width ? 1 : 0;
                var request = GanttLaneLabelRequest(placed.Item.Label!, style, item, context.Theme.Spacing / 2, shorten: false);
                item.Request = request; requests.Add(request); requested.Add(item);
            }
        }

        // Complete captions get first use of the available local lanes. An abbreviated
        // automatic caption stays inside its own bar rather than displacing another item.
        var service = new LabelPlacementService();
        var complete = service.Place(requests, plot, obstacles, 2, builder.MeasureText);
        var shortened = new List<LabelPlacementRequest>(); var retry = new List<PackedGanttItem>();
        for (var index = 0; index < complete.Count; index++) {
            var item = requested[index]; var label = complete[index]; item.Label = label;
            if (!label.IsDropped) obstacles.Add(new LabelObstacle(item.Id + "-accepted-label", label.Bounds));
            else {
                var request = item.Request!;
                shortened.Add(GanttLaneLabelRequest(request.Text, request.Style, item, context.Theme.Spacing / 2, shorten: true));
                retry.Add(item);
            }
        }
        var fallback = service.Place(shortened, plot, obstacles, 2, builder.MeasureText);
        for (var index = 0; index < fallback.Count; index++) retry[index].Label = fallback[index];
        if (requested.Any(item => item.Label!.IsDropped || item.Label!.IsEllipsized))
            builder.AddDiagnostic(new VisualDiagnostic("schedule.data-label-overflow", "Packed item captions were shortened or omitted to avoid marks, group headings and other captions; complete item facts remain available."));
        return items;
    }

    private static LabelPlacementRequest GanttLaneLabelRequest(string text, TextStyle style, PackedGanttItem item, double gap, bool shorten) {
        var bounds = item.Bounds; var anchor = new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        var placement = item.Placement; var candidates = new List<LabelCandidate>();
        var inside = placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Inside or ChartDataLabelPlacement.Center;
        if (inside) candidates.Add(new LabelCandidate(0, 0, .5, .5));
        void Vertical(bool above) {
            foreach (var alignment in new[] { .5, 0d, 1d }) candidates.Add(new LabelCandidate(0,
                above ? bounds.Top - anchor.Y - gap : bounds.Bottom - anchor.Y + gap, alignment, above ? 1 : 0));
        }
        void Side(bool left) => candidates.Add(new LabelCandidate(left ? bounds.Left - anchor.X - gap : bounds.Right - anchor.X + gap, 0, left ? 1 : 0, .5));
        if (!shorten || !inside) {
            if (placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Above) Vertical(true);
            if (placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Below) Vertical(false);
            if (placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside) Side(false);
            if (placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Left) Side(true);
        }
        var request = new LabelPlacementRequest(text, anchor, style, candidates, item.Priority) {
            AssociatedMarkId = item.Id, Padding = PackedGanttCaptionPadding,
            Fallback = shorten ? LabelFallbackRule.EllipsisThenDrop : LabelFallbackRule.Drop
        };
        if (inside && (shorten || placement != ChartDataLabelPlacement.Auto)) request.Bounds = bounds;
        return request;
    }

    /// <summary>Retains the complete description and paints the fitted text within its original source-mark group.</summary>
    private static void DrawGanttLaneLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series, int index,
        PackedGanttItem item, VisualThemeColors colors, ChartStateMark mark) {
        if (item.Label == null) return;
        var label = item.Label; var id = item.Id + "-label";
        builder.AddRegion(new VisualSemanticRegion(id, "data-label", label.IsDropped ? item.Bounds : label.Bounds, label.Request.Text));
        if (label.IsDropped) return;
        var contained = LabelPlacementService.Contains(item.Bounds, label.Bounds);
        var ink = ChartMarkText.OnStateMark(chart, colors, context.Frame, mark);
        var style = VisualStateSceneTools.DataStyle(chart, context, series, index,
            contained ? ink.Color : ChartColorMath.AccessibleTextOnBackground(colors.Surface));
        style.TextCase = TextCaseTransform.None; style.Alignment = TextAlignment.Left;
        if (!contained && (label.Bounds.Right <= item.Bounds.Left || label.Bounds.Left >= item.Bounds.Right))
            VisualStateSceneTools.Connector(builder, chart.Options, label.Request.Anchor, label.LeaderEnd, colors.MutedForeground);
        builder.Text(label.Text, label.Bounds.Left + label.Request.Padding,
            label.Bounds.Top + label.Request.Padding + builder.TextAscent(style), style, role: "data-label", id: id,
            paint: VisualStateSceneTools.DataPaint(chart, series, index, style, contained ? ink.Paint : null));
    }

    private sealed class PackedGanttItem {
        internal PackedGanttItem(string id, ChartRect bounds, bool visible) { Id = id; Bounds = bounds; Visible = visible; }
        internal string Id { get; }
        internal ChartRect Bounds { get; }
        internal bool Visible { get; }
        internal ChartDataLabelPlacement Placement { get; set; }
        internal int Priority { get; set; }
        internal LabelPlacementRequest? Request { get; set; }
        internal PlacedLabel? Label { get; set; }
    }
}
