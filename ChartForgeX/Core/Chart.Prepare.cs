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
    /// <param name="viewport">The physical Cartesian windows and value-domain policy. Null restores every full domain.
    /// An omitted value axis retains its full domain unless VisibleCategories is selected.</param>
    /// <returns>A prepared scene with its detached native projections when supported.</returns>
    /// <remarks>Explicit windows support non-sparkline Cartesian series, including horizontal mixtures and independent secondary value axes.
    /// Time bounds use the same OLE Automation date units as source observations. Interpolation and full-data stack calculations are retained.
    /// Unrequested full-data domains remain fixed by default; VisibleCategories fits only unrequested value axes. Authored series paint stays fixed. Marks are clipped to the plot even
    /// when ClipMarksToPlot is false; automatic marker padding does not expand the requested bounds.
    /// All source observations and disconnected paths are retained. The chart and authored axes are unchanged.
    /// Full-domain preparation also supports non-Cartesian charts. This API does not add interactive HTML zoom.</remarks>
    /// <exception cref="ArgumentNullException">Context is null.</exception>
    /// <exception cref="NotSupportedException">An explicit window uses an unsupported chart or an unavailable physical secondary slot.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A requested window extends outside its resolved full domain or cannot map source coordinates into finite logical units.</exception>
    /// <exception cref="InvalidOperationException">An explicit window is requested without observations or a usable plot rectangle.</exception>
    public PreparedVisual Prepare(VisualRenderContext context, ChartCartesianViewport? viewport) => PrepareCore(context, viewport);

    private PreparedVisual PrepareCore(VisualRenderContext context, ChartCartesianViewport? viewport, ChartCartesianFullDomain? fullDomain = null, ChartHierarchyView? hierarchyView = null, bool requireHierarchy = false, ChartPolarViewport? polarViewport = null, bool requirePolar = false) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        var view = (Chart)MemberwiseClone();
        view.Options = Options.PresentationView(context);
        view.HierarchyPreparation = ChartHierarchyPreparation.Resolve(view, hierarchyView ?? view.NativeHierarchyVisibility?.HierarchyView, requireHierarchy, view.NativeHierarchyVisibility);
        ChartTimeSpacingPreparation.Capture(view);
        view.PolarPreparation = ChartPolarPreparation.Resolve(view, polarViewport, requirePolar);
        if (!view.Options.HeatmapNaturalHeight || viewport != null || view.Series.Count == 0 || view.Series.Any(series => series.Kind != ChartSeriesKind.Heatmap))
            return view.PrepareScene(context, viewport, fullDomain);
        // Measure the real frame and group headers. Repeat only while the caller permits growth and rows still need space.
        for (var attempt = 0; attempt < 5; attempt++) {
            var prepared = view.PrepareScene(context, viewport, fullDomain);
            var plot = prepared.Regions.FirstOrDefault(region => region.Role == "matrix-plot")?.Bounds;
            if (!plot.HasValue) return prepared;
            var rows = view.Series.ToArray();
            var rowLayout = ChartHeatmapRowLayout.Build(rows, plot.Value.Height, context.Theme.Typography.AxisSize);
            var gap = Math.Max(0, view.Options.HeatmapCellGap ?? 2);
            var wanted = rowLayout.HeadersHeight + rows.Length * 12 + Math.Max(0, rows.Length - 1) * gap;
            if (wanted <= plot.Value.Height + .001) return prepared;
            var height = context.Layout.Size.Height + Math.Ceiling((wanted - plot.Value.Height) * 1.25 + 1);
            if (attempt == 4 || height > 32768) return prepared;
            var layout = new VisualLayoutOptions(new VisualSize(context.Layout.Size.Width, height), context.Layout.PaddingEdges);
            context = new VisualRenderContext(layout, context.Theme, context.ThemeMode, context.Frame, context.Font)
                .WithNativeTextCapture(context.NativeTextCapture).WithNativeValueSource(context.CaptureNativeValueSource);
            view.Options = Options.PresentationView(context);
        }
        throw new InvalidOperationException("Heatmap natural height preparation did not converge.");
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
        var captureHierarchy = Series.Count == 1 && Series[0].Kind is ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst;
        var captureBubble = context.CaptureNativeValueSource && viewport == null && Series.Count > 0 && Series.All(series => series.Kind == ChartSeriesKind.Bubble);
        var captureRange = context.CaptureNativeValueSource && viewport == null && Series.Count > 0 && Series.All(series => series.Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea);
        var captureError = context.CaptureNativeValueSource && viewport == null && Series.Count > 0 && Series.All(series => series.Kind == ChartSeriesKind.ErrorBar);
        var captureFinancial = context.CaptureNativeValueSource && viewport == null && Series.Count > 0 && Series.All(series => series.Kind is ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc);
        var captureCompleteCartesian = family == VisualChartFamily.Cartesian && viewport == null && VisualCartesianCompiler.NeedsCompleteCartesianValues(this);
        var textCapture = context.NativeTextCapture ?? (family == VisualChartFamily.Radial || captureHierarchy || captureCompleteCartesian || captureBubble || captureRange || captureError || captureFinancial ? new VisualSceneTextCapture() : null);
        context = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, frame, context.Font).WithNativeTextCapture(textCapture).WithRequestsOf(context);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font, textCapture);
        var colors = context.Theme.Resolve(context.ThemeMode);
        var mapLegendInset = family == VisualChartFamily.Map && !sourceFrame.HasExplicitLegendPosition && !Options.HasExplicitLegendPosition
            ? VisualMapCompiler.ResolveTileLegendInset(this, context, builder, entries, colorScales.Map) : null;
        var content = VisualFrameLayout.Build(builder, context, mapLegendInset == null ? entries : Array.Empty<VisualLegendEntry>(), out var legendHeight);
        var projection = VisualChartCompiler.Build(family, this, context, builder, content, axisLabels, bubbleScale, colorScales, legendHeight, viewport, mapLegendInset, fullDomain);
        if (mapLegendInset != null) VisualFrameLayout.PaintOverlayLegend(builder, context, entries, mapLegendInset.LegendArea);
        if (family == VisualChartFamily.Radial && textCapture != null && (!textCapture.IsReplay || context.CaptureNativeValueSource))
            builder.CaptureExistingValue(scene => VisualRadialCompiler.TryCaptureValues(this, context, textCapture, scene),
                new VisualSceneBuilder.VisualValueBoundary(0, 0, 0), builder.ValueBoundary());
        if (captureHierarchy && textCapture != null && !textCapture.IsReplay)
            builder.CaptureExistingValue(scene => VisualHierarchyCompiler.TryCaptureValues(this, context, textCapture, scene),
                new VisualSceneBuilder.VisualValueBoundary(0, 0, 0), builder.ValueBoundary());
        if (captureCompleteCartesian && textCapture != null && !textCapture.IsReplay)
            builder.CaptureExistingValue(scene => VisualCartesianCompiler.TryCaptureCompleteValues(this, context, textCapture, scene, projection),
                new VisualSceneBuilder.VisualValueBoundary(0, 0, 0), builder.ValueBoundary());
        if (captureBubble && textCapture != null)
            builder.CaptureExistingValue(scene => VisualCartesianCompiler.TryCaptureBubbleValues(this, context, textCapture, scene, projection, bubbleScale!),
                new VisualSceneBuilder.VisualValueBoundary(0, 0, 0), builder.ValueBoundary());
        if (captureRange && textCapture != null)
            builder.CaptureExistingValue(scene => VisualCartesianCompiler.TryCaptureRangeValues(this, context, textCapture, scene, projection),
                new VisualSceneBuilder.VisualValueBoundary(0, 0, 0), builder.ValueBoundary());
        if (captureError && textCapture != null)
            builder.CaptureExistingValue(scene => VisualCartesianCompiler.TryCaptureErrorValues(this, context, textCapture, scene, projection),
                new VisualSceneBuilder.VisualValueBoundary(0, 0, 0), builder.ValueBoundary());
        if (captureFinancial && textCapture != null)
            builder.CaptureExistingValue(scene => VisualCartesianCompiler.TryCaptureFinancialValues(this, context, textCapture, scene, projection),
                new VisualSceneBuilder.VisualValueBoundary(0, 0, 0), builder.ValueBoundary());
        builder.CaptureChartIdentity(this, projection);
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= !string.IsNullOrWhiteSpace(frame.Title) ? frame.Title
            : !string.IsNullOrWhiteSpace(Title) ? Title : Options.Labels.UntitledChart;
        accessibility.Description ??= Options.Labels.Describe(ChartAccessibleDescription.Facts(this));
        return new PreparedVisual(builder.Build(), accessibility,
            svgOptions: new VisualSvgOptions(colorVariables: Options.SvgColorVariables), xProjection: projection?.X, yProjection: projection?.Y,
            secondaryXProjection: projection?.SecondaryX, secondaryYProjection: projection?.SecondaryY, viewport: viewport,
            cartesianOrientation: projection == null ? null : projection.Horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical,
            hierarchyPreparation: HierarchyPreparation, polarProjection: PolarPreparation?.Projection, polarViewport: PolarPreparation?.Request).WithCapturedText(textCapture?.Freeze());
    }

    /// <summary>Resolves frame typography identically for native preparation and portable layout measurements.</summary>
    internal static TextStyle ResolveFrameTextStyle(VisualRenderContext context, TextStyle? configured, TextStyleOverride model,
        double size, ChartColor color, int weight) {
        var fallback = configured ?? new TextStyle { Font = context.Font, FontSize = size, Color = color, LineHeight = 1 };
        if (configured == null) fallback.Font.Weight = weight;
        return model.Resolve(fallback);
    }
}
