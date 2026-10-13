using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

public sealed partial class Chart : IVisualRenderable {
    /// <summary>Prepares chart geometry, typography and semantics into a detached shared static scene.</summary>
    /// <remarks>Every chart family uses the same mutable-model validation before its scene is compiled.</remarks>
    public PreparedVisual Prepare(VisualRenderContext context) => PrepareCore(context, null);

    /// <summary>Prepares a detached line-chart scene within an explicit numeric X window.</summary>
    /// <param name="context">The logical size, frame, theme and typography used for preparation.</param>
    /// <param name="viewport">An increasing window contained in the resolved full X domain.</param>
    /// <returns>A prepared scene with its detached X projection.</returns>
    /// <remarks>Supports ordinary vertical Line and StepLine series with linear X and linear or step interpolation; sparklines are excluded.
    /// Full-data primary Y and secondary Y domains and authored series paint remain fixed. Marks are clipped to the plot even
    /// when ClipMarksToPlot is false; automatic marker padding does not expand the requested X bounds.
    /// All source observations and disconnected paths are retained. The chart and authored axes are unchanged.
    /// Call Prepare(context) to prepare the full domain again. This API does not add interactive HTML zoom.</remarks>
    /// <exception cref="ArgumentNullException">Context or viewport is null.</exception>
    /// <exception cref="NotSupportedException">The chart uses an unsupported series, interpolation or X scale.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The viewport extends outside the resolved full X domain or cannot map source X coordinates into finite logical units.</exception>
    /// <exception cref="InvalidOperationException">The chart has no observations or no usable plot rectangle.</exception>
    public PreparedVisual Prepare(VisualRenderContext context, ChartXViewport viewport) {
        if (viewport == null) throw new ArgumentNullException(nameof(viewport));
        return PrepareCore(context, viewport);
    }

    private PreparedVisual PrepareCore(VisualRenderContext context, ChartXViewport? viewport) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        ChartGuards.RenderCompatibility(this, preparing: true);
        if (viewport != null) VisualCartesianCompiler.ValidateXViewport(this);
        var family = VisualChartCompiler.Family(this);
        var bubbleScale = Series.Any(series => series.Kind == ChartSeriesKind.Bubble) ? ChartBubbleSizeScale.Create(this) : null;
        var colorScales = new ChartColorPresentationSet(this);
        var sourceFrame = context.Frame;
        var frameColors = context.Theme.Resolve(context.ThemeMode);
        var axisLabels = family is VisualChartFamily.NumericRadial or VisualChartFamily.Cartesian ? new ChartAxisValueFormatter.Cache() : null;
        var entries = VisualChartCompiler.LegendEntries(family, this, frameColors, axisLabels, bubbleScale, colorScales);
        TextStyle RoleStyle(TextStyle? configured, TextStyleOverride model, double size, ChartColor color, int weight) {
            var fallback = configured ?? new TextStyle { Font = context.Font, FontSize = size, Color = color, LineHeight = 1 };
            if (configured == null) fallback.Font.Weight = weight;
            return model.Resolve(fallback);
        }
        var frame = new VisualFrame(sourceFrame.Title ?? Title, sourceFrame.Subtitle ?? Subtitle,
            sourceFrame.HasExplicitLegend ? sourceFrame.ShowLegend : ChartLegendVisibility.ForPreparedContent(this, entries.Count, colorScales),
            sourceFrame.HasExplicitLegendPosition ? sourceFrame.LegendPosition : Options.LegendPosition,
            sourceFrame.ShowSurface, sourceFrame.TransparentBackground,
            RoleStyle(sourceFrame.TitleStyle, Options.TitleStyle, context.Theme.Typography.TitleSize, frameColors.Foreground, 700),
            RoleStyle(sourceFrame.SubtitleStyle, Options.SubtitleStyle, context.Theme.Typography.SubtitleSize, frameColors.MutedForeground, 400),
            RoleStyle(sourceFrame.LegendStyle, Options.LegendStyle, context.Theme.Typography.LegendSize, frameColors.Foreground, 400),
            sourceFrame.LegendMaximumRows, sourceFrame.LegendMaximumHeightFraction, sourceFrame.ShowCard, sourceFrame.LegendTitle);
        context = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, frame, context.Font);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var colors = context.Theme.Resolve(context.ThemeMode);
        var content = VisualFrameLayout.Build(builder, context, entries, out var legendHeight);
        var projection = VisualChartCompiler.Build(family, this, context, builder, content, axisLabels, bubbleScale, colorScales, legendHeight, viewport);
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= !string.IsNullOrWhiteSpace(frame.Title) ? frame.Title
            : !string.IsNullOrWhiteSpace(Title) ? Title : Options.Labels.UntitledChart;
        accessibility.Description ??= Options.Labels.Describe(ChartAccessibleDescription.Facts(this));
        return new PreparedVisual(builder.Build(), accessibility,
            svgOptions: new VisualSvgOptions(colorVariables: Options.SvgColorVariables), xProjection: projection);
    }
}
