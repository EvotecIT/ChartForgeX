using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart : IVisualRenderable {
    /// <summary>Prepares the Phase 1 Cartesian or pie/donut model directly into a shared static scene.</summary>
    /// <remarks>Legacy exporters remain available for families and options awaiting migration. Unsupported prepared options throw explicitly.</remarks>
    public PreparedVisual Prepare(VisualRenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (Series.Any(series => series == null)) throw new InvalidOperationException("Chart series must not contain null entries.");
        var radial = Series.Count == 1 && (Series[0].Kind == ChartSeriesKind.Pie || Series[0].Kind == ChartSeriesKind.Donut);
        if (radial) {
            if (Series[0].Points.Any(point => double.IsNaN(point.Y) || double.IsInfinity(point.Y) || point.Y < 0))
                throw new InvalidOperationException("Pie and donut values must be finite and non-negative.");
        } else {
            if (Series.Any(series => series.Kind != ChartSeriesKind.Line && series.Kind != ChartSeriesKind.StepLine &&
                series.Kind != ChartSeriesKind.Area && series.Kind != ChartSeriesKind.StepArea && series.Kind != ChartSeriesKind.StackedArea &&
                series.Kind != ChartSeriesKind.Bar && series.Kind != ChartSeriesKind.Scatter))
                throw new NotSupportedException("This chart family has not been migrated to the prepared pipeline. Use the legacy exporter until its migration phase.");
            ChartGuards.RenderCompatibility(this);
        }
        var sourceFrame = context.Frame;
        var frame = new VisualFrame(sourceFrame.Title ?? Title, sourceFrame.Subtitle ?? Subtitle,
            sourceFrame.ShowLegend, sourceFrame.LegendPosition, sourceFrame.ShowSurface, sourceFrame.TransparentBackground);
        context = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, frame, context.Font);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var colors = context.Theme.Resolve(context.ThemeMode);
        var entries = radial ? VisualRadialCompiler.LegendEntries(this, colors) : VisualCartesianCompiler.LegendEntries(this, colors);
        var content = VisualFrameLayout.Build(builder, context, entries);
        if (radial) VisualRadialCompiler.Build(this, context, builder, content);
        else VisualCartesianCompiler.BuildInViewport(this, context, builder, content);
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= frame.Title;
        accessibility.Description ??= frame.Subtitle;
        return new PreparedVisual(builder.Build(), accessibility);
    }
}
