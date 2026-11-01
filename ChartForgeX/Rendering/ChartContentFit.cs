using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Fits the shared frame and row content before either static backend compiles marks.</summary>
internal static class ChartContentFit {
    internal static VisualRenderContext Resolve(Chart chart, VisualRenderContext context) {
        if (chart.Series.Count == 0 || chart.IsStrip) return context;
        var family = VisualChartCompiler.Family(chart);
        var kind = chart.Series[0].Kind;
        var pitch = 32d; var rows = 0d; var axisRows = 0d;
        if (family == VisualChartFamily.Schedule) {
            rows = kind == ChartSeriesKind.StateTimeline ? ChartStateTimelineModel.Build(chart).Units
                : kind == ChartSeriesKind.GanttLane || ChartSeriesKindTraits.IsMixedSchedule(chart)
                    ? ChartGanttLaneModel.Build(chart, mixed: ChartSeriesKindTraits.IsMixedSchedule(chart)).Units
                    : chart.Series.Count(series => series.HasSourceData && (series.RenderPoints.Count > 0 || series.GanttItems.Count > 0));
            axisRows = chart.Options.ShowAxes && chart.Options.XAxis.Visible ? 1 : 0;
            if (chart.Options.GanttToday.HasValue) axisRows++;
            if (chart.Series.Any(series => series.ResolveDataLabels(chart.Options, defaultVisible: true))) axisRows++;
        } else if (kind == ChartSeriesKind.ProgressBar) rows = chart.Series[0].RenderPoints.Count;
        else if (kind == ChartSeriesKind.Bullet && chart.Options.Bullet.Orientation == ChartOrientation.Horizontal) {
            rows = chart.Series.Count(series => series.HasSourceData); pitch = 40;
            axisRows = chart.Options.ShowAxes && chart.Options.XAxis.Visible ? 1 : 0;
        } else if (kind == ChartSeriesKind.Pictorial) rows = chart.Series[0].RenderPoints.Count;
        else if (kind == ChartSeriesKind.Gauge && chart.Options.Gauge.Form == ChartGaugeForm.Linear
            && chart.Options.Gauge.Orientation == ChartOrientation.Horizontal) { rows = 1; pitch = 56; axisRows = 2; }
        else return context;
        if (rows <= 0) return context;
        var colors = context.Theme.Resolve(context.ThemeMode);
        var source = context.Frame;
        var type = context.Theme.Typography;
        var frame = new VisualFrame(source.Title ?? chart.Title, source.Subtitle ?? chart.Subtitle,
            source.HasExplicitLegend ? source.ShowLegend : chart.Options.ShowLegend,
            source.HasExplicitLegendPosition ? source.LegendPosition : chart.Options.LegendPosition,
            source.ShowSurface, source.TransparentBackground,
            Chart.ResolveFrameTextStyle(context, source.TitleStyle, chart.Options.TitleStyle, type.TitleSize, colors.Foreground, 700),
            Chart.ResolveFrameTextStyle(context, source.SubtitleStyle, chart.Options.SubtitleStyle, type.SubtitleSize, colors.MutedForeground, 400),
            Chart.ResolveFrameTextStyle(context, source.LegendStyle, chart.Options.LegendStyle, type.LegendSize, colors.Foreground, 400),
            source.LegendMaximumRows, source.LegendMaximumHeightFraction, source.ShowCard);
        var probeContext = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, frame, context.Font);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var content = VisualFrameLayout.Build(builder, probeContext, VisualChartCompiler.LegendEntries(family, chart, colors));
        var axisStyle = VisualStateSceneTools.TickStyle(chart, context);
        var axisHeight = axisRows * (builder.MeasureText("Mg", axisStyle).Height + context.Theme.Spacing);
        if (chart.XAxisTitle.Length > 0) axisHeight += type.AxisTitleSize * 1.5 + context.Theme.Spacing;
        var height = Math.Min(context.Layout.Size.Height,
            Math.Ceiling(content.Top + rows * pitch + axisHeight + context.Layout.PaddingEdges.Bottom));
        height = Math.Max(context.Layout.PaddingEdges.Top + context.Layout.PaddingEdges.Bottom + 1, height);
        var layout = new VisualLayoutOptions(new VisualSize(context.Layout.Size.Width, height), context.Layout.PaddingEdges);
        return new VisualRenderContext(layout, context.Theme, context.ThemeMode, context.Frame, context.Font)
            .WithNativeTextCapture(context.NativeTextCapture).WithRequestsOf(context);
    }
}
