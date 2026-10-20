using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

public sealed partial class Chart : IVisualRenderable {
    /// <summary>Prepares chart geometry, typography and semantics into a detached shared static scene.</summary>
    /// <remarks>Every chart family uses the same mutable-model validation before its scene is compiled.</remarks>
    public PreparedVisual Prepare(VisualRenderContext context) => Prepare(context, null);

    /// <summary>Prepares a detached scene within optional source-coordinate Cartesian windows.</summary>
    /// <param name="context">The logical size, frame, theme and typography used for preparation.</param>
    /// <param name="viewport">The explicit X, primary Y or XY windows. Null prepares the full domain, including when resetting a previous viewport.
    /// An omitted axis retains its full-data domain. Y windows require no secondary-axis series.</param>
    /// <returns>A prepared scene with its detached native projections when supported.</returns>
    /// <remarks>Explicit windows support ordinary vertical line, area, scatter, bubble, financial and paired-boundary series with linear or time X axes; sparklines are excluded.
    /// Time bounds use the same OLE Automation date units as source observations. Interpolation and full-data stack calculations are retained.
    /// Unrequested primary Y and secondary Y domains and authored series paint remain fixed. Marks are clipped to the plot even
    /// when ClipMarksToPlot is false; automatic marker padding does not expand the requested bounds.
    /// All source observations and disconnected paths are retained. The chart and authored axes are unchanged.
    /// Full-domain preparation also supports non-Cartesian charts. This API does not add interactive HTML zoom.</remarks>
    /// <exception cref="ArgumentNullException">Context is null.</exception>
    /// <exception cref="NotSupportedException">An explicit window uses an unsupported chart, scale or secondary Y axis.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A requested window extends outside its resolved full domain or cannot map source coordinates into finite logical units.</exception>
    /// <exception cref="InvalidOperationException">An explicit window is requested without observations or a usable plot rectangle.</exception>
    public PreparedVisual Prepare(VisualRenderContext context, ChartCartesianViewport? viewport) => PrepareCore(context, viewport);

    private PreparedVisual PrepareCore(VisualRenderContext context, ChartCartesianViewport? viewport, ChartCartesianFullDomain? fullDomain = null) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        var view = (Chart)MemberwiseClone();
        view.Options = Options.PresentationView(context);
        return view.PrepareScene(context, viewport, fullDomain);
    }

    private PreparedVisual PrepareScene(VisualRenderContext context, ChartCartesianViewport? viewport, ChartCartesianFullDomain? fullDomain) {
        ChartGuards.RenderCompatibility(this, preparing: true);
        if (viewport != null) VisualCartesianCompiler.ValidateViewport(this, viewport);
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
        var mapLegendInset = family == VisualChartFamily.Map && !sourceFrame.HasExplicitLegendPosition && !Options.HasExplicitLegendPosition
            ? VisualMapCompiler.ResolveTileLegendInset(this, context, builder, entries, colorScales.Map) : null;
        var content = VisualFrameLayout.Build(builder, context, mapLegendInset == null ? entries : Array.Empty<VisualLegendEntry>(), out var legendHeight);
        var projection = VisualChartCompiler.Build(family, this, context, builder, content, axisLabels, bubbleScale, colorScales, legendHeight, viewport, mapLegendInset, fullDomain);
        if (mapLegendInset != null) VisualFrameLayout.PaintOverlayLegend(builder, context, entries, mapLegendInset.LegendArea);
        builder.CaptureChartIdentity(this);
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= !string.IsNullOrWhiteSpace(frame.Title) ? frame.Title
            : !string.IsNullOrWhiteSpace(Title) ? Title : Options.Labels.UntitledChart;
        accessibility.Description ??= Options.Labels.Describe(ChartAccessibleDescription.Facts(this));
        return new PreparedVisual(builder.Build(), accessibility,
            svgOptions: new VisualSvgOptions(colorVariables: Options.SvgColorVariables), xProjection: projection?.X, yProjection: projection?.Y);
    }
}
