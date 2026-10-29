using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    internal static bool SupportsCompleteValueKind(ChartSeriesKind kind) => kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine
        or ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea or ChartSeriesKind.Scatter or ChartSeriesKind.Bar;

    internal static bool SupportsJointSeriesValues(IVisualValuePlan plan) => plan is not CompleteCartesianValuePlan complete || complete.SupportsJointSeriesValues;

    /// <summary>The incumbent fragment plan remains exact for labels-off unstacked Line/Bar/Scatter.</summary>
    internal static bool NeedsCompleteCartesianValues(Chart chart) => chart.Series.All(series => SupportsCompleteValueKind(series.Kind)) && chart.Series.Any(series =>
        series.Kind is ChartSeriesKind.StepLine or ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea
        || series.ResolveDataLabels(chart.Options, defaultVisible: false) || ChartStackLayout.Participates(chart, series));

    /// <summary>Captures complete native stack, area and caption owners without retaining a caller model or matching primitives.</summary>
    internal static IVisualValuePlan? TryCaptureCompleteValues(Chart chart, VisualRenderContext context,
        VisualSceneTextCapture text, VisualScene scene, ChartCartesianProjection? axes) {
        if (axes == null || ChartCartesianOrientation.IsHorizontal(chart) || !text.SupportsCapturedReplay || chart.Series.Count == 0
            || chart.Series.Any(series => !SupportsCompleteValueKind(series.Kind) || series.Id.IsAutomatic || series.IsVisibilityPreparation
                || series.Points.Count == 0 || series.ObservationIds.Keys.Any(index => index < 0 || index >= series.Points.Count)
                || Enumerable.Range(0, series.Points.Count).Any(index => !series.ObservationIds.ContainsKey(index)))) return null;
        // The HTML value channel keeps one exact accepted axis/plot basis. Auto-domain or label-gutter changes
        // are not disguised as a fixed-basis morph; the actual native projection is checked on every frame too.
        foreach (var side in chart.Series.Select(series => series.ValueAxis).Distinct()) {
            var axis = side == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            if (!axis.Minimum.HasValue || !axis.Maximum.HasValue || axis.Scale == ChartScaleKind.Time) return null;
        }
        try {
            var source = ChartVisibilitySource.CaptureNativeCartesianValues(chart, context);
            var observed = text.CaptureNativeRequests(builder => ObserveCompleteValueText(chart, context, builder), context.Layout.Size, context.Font);
            var expanded = observed.TryCaptureValueRepertoire(chart.Options.ValueFormat, Array.Empty<(string Owner, string Text)>());
            if (!expanded.SupportsCapturedReplay) return null;
            var plan = new CompleteCartesianValuePlan(source, context.WithNativeTextCapture(null), expanded.Freeze(), axes);
            var points = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
            if (points.Length != plan.Readings.Count) return null;
            var identities = new HashSet<(string Series, string Observation)>();
            foreach (var point in points) if (!point.Metadata.TryGetValue("data-cfx-series-key", out var key)
                || !point.Metadata.TryGetValue("data-cfx-observation-id", out var id) || !identities.Add((key, id))) return null;
            foreach (var reading in plan.Readings) if (!identities.Contains((reading.SeriesKey ?? plan.SeriesKey, reading.ObservationId!))) return null;
            text.AdoptNativeRequests(expanded); return plan;
        } catch (NotSupportedException) { return null; }
        catch (ArgumentException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    private static void ObserveCompleteValueText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        foreach (var series in chart.Series) {
            var style = SeriesLabelStyle(chart, context, series, colors);
            var format = SeriesCaptionFormat(chart, series);
            for (var point = 0; point < series.Points.Count; point++) {
                var label = ResolvePointLabel(chart, series, point, style, format);
                builder.MeasureText(label.Text, label.Style); builder.MeasureText("Mg", label.Style); builder.TextAscent(label.Style);
            }
        }
        if (chart.Options.ShowStackTotals) {
            var style = chart.Options.DataLabelStyle.Resolve(new TextStyle {
                Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = colors.Foreground
            });
            builder.MeasureText(ChartNumericFormatter.FormatValue(chart.Options, 0), style);
            builder.MeasureText("Mg", style); builder.TextAscent(style);
        }
    }
}
