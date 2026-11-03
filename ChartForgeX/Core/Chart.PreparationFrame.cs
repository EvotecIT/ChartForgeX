using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    private (VisualChartFamily Family, ChartBubbleSizeScale? BubbleScale, ChartColorPresentationSet ColorScales,
        ChartAxisValueFormatter.Cache? AxisLabels, IReadOnlyList<VisualLegendEntry> Entries, VisualFrame Frame) PreparationFrame(VisualRenderContext context) {
        var family = VisualChartCompiler.Family(this);
        var bubbleScale = Series.Any(series => series.Kind == ChartSeriesKind.Bubble) ? ChartBubbleSizeScale.Create(this) : null;
        var colorScales = new ChartColorPresentationSet(this);
        var sourceFrame = context.Frame;
        var frameColors = context.Theme.Resolve(context.ThemeMode);
        var axisLabels = family is VisualChartFamily.NumericRadial or VisualChartFamily.Cartesian ? new ChartAxisValueFormatter.Cache() : null;
        var entries = _visibilityLegendEntries?.Invoke(context)
            ?? VisualChartCompiler.LegendEntries(family, this, frameColors, axisLabels, bubbleScale, colorScales);
        TextStyle RoleStyle(TextStyle? configured, TextStyleOverride model, double size, ChartColor color, int weight) =>
            ResolveFrameTextStyle(context, configured, model, size, color, weight);
        var frame = new VisualFrame(sourceFrame.Title ?? Title, sourceFrame.Subtitle ?? Subtitle,
            sourceFrame.HasExplicitLegend ? sourceFrame.ShowLegend : ChartLegendVisibility.ForPreparedContent(this, entries.Count, colorScales),
            sourceFrame.HasExplicitLegendPosition ? sourceFrame.LegendPosition : Options.LegendPosition,
            sourceFrame.ShowSurface, sourceFrame.TransparentBackground,
            RoleStyle(sourceFrame.TitleStyle, Options.TitleStyle, context.Theme.Typography.TitleSize, frameColors.Foreground, 700),
            RoleStyle(sourceFrame.SubtitleStyle, Options.SubtitleStyle, context.Theme.Typography.SubtitleSize, frameColors.MutedForeground, 400),
            RoleStyle(sourceFrame.LegendStyle, Options.LegendStyle, context.Theme.Typography.LegendSize, frameColors.Foreground, 400),
            sourceFrame.LegendMaximumRows, sourceFrame.LegendMaximumHeightFraction, sourceFrame.ShowCard, sourceFrame.LegendTitle);
        if (!sourceFrame.HasExplicitLegendPosition && !Options.HasExplicitLegendPosition && family != VisualChartFamily.Map)
            frame = frame.WithAutomaticLegendPlacement();
        return (family, bubbleScale, colorScales, axisLabels, entries, frame);
    }

    private (VisualRenderContext Context, VisualSceneBuilder Builder, ChartRect Content,
        ChartAxisValueFormatter.Cache? AxisLabels, ChartColorPresentationSet ColorScales, double LegendHeight) BeginPreparation(VisualRenderContext context) {
        var frame = PreparationFrame(context);
        context = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, frame.Frame, context.Font).WithRequestsOf(context);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var content = VisualFrameLayout.Build(builder, context, frame.Entries, out var legendHeight);
        return (context, builder, content, frame.AxisLabels, frame.ColorScales, legendHeight);
    }

    private ChartForgeX.Accessibility.VisualAccessibility PreparationAccessibility(VisualFrame frame) {
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= !string.IsNullOrWhiteSpace(frame.Title) ? frame.Title : !string.IsNullOrWhiteSpace(Title) ? Title : Options.Labels.UntitledChart;
        accessibility.Description ??= Options.Labels.Describe(ChartAccessibleDescription.Facts(this));
        return accessibility;
    }
}
