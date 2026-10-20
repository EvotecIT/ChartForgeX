using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles Cartesian geometry, styles and semantic alternatives into shared immutable scene commands.</summary>
internal static partial class VisualCartesianCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors,
        ChartAxisValueFormatter.Cache? axisLabels = null, ChartBubbleSizeScale? bubbleScale = null, ChartColorPresentationSet? colorScales = null) {
        colorScales ??= new ChartColorPresentationSet(chart);
        var entries = new List<VisualLegendEntry>();
        if (chart.Options.ShowPointLegend && chart.Series.Count == 1 && chart.Series[0].ShowInLegend
            && ChartSeriesKindTraits.SupportsPointLegend(chart.Series[0].Kind) && chart.Series[0].AuthoredObservationCount > 0) {
            var series = chart.Series[0];
            if (series.Kind == ChartSeriesKind.Waterfall) return WaterfallLegendEntries(chart, series, colors, axisLabels ?? new ChartAxisValueFormatter.Cache());
            if (series.Kind == ChartSeriesKind.RangeBar) return RangeBarLegendEntries(chart, series, colors, axisLabels ?? new ChartAxisValueFormatter.Cache());
            if (series.Kind == ChartSeriesKind.BoxPlot) return BoxPlotLegendEntries(chart, series, colors, axisLabels ?? new ChartAxisValueFormatter.Cache());
            var stride = ObservationStride(series.Kind);
            var numericRadial = series.Kind is ChartSeriesKind.RadialBar or ChartSeriesKind.RadialColumn;
            if (numericRadial) axisLabels ??= new ChartAxisValueFormatter.Cache();
            var categoryFormatter = numericRadial ? ChartAxisValueFormatter.Create(chart.Options.XAxis,
                series.RenderPoints.Select(point => point.X).Distinct().OrderBy(value => value).ToArray()) : null;
            for (var point = 0; point < series.AuthoredObservationCount; point++) {
                var label = numericRadial ? axisLabels!.Format(chart.Options.XAxis, series.RenderPoints[point].X, categoryFormatter!)
                    : ChartAxisValueFormatter.FindExplicitLabel(ChartCartesianOrientation.CategoryLabels(chart), series.RenderPoints[point * stride].X) ?? "Item " + Number(point + 1);
                var pattern = point < series.PointFillPatterns.Count && series.PointFillPatterns[point].HasValue ? series.PointFillPatterns[point]!.Value : series.FillPattern;
                var color = PointColor(series, 0, point, colors);
                if (series.Kind == ChartSeriesKind.Candlestick || series.Kind == ChartSeriesKind.Ohlc)
                    color = FinancialColor(series, 0, point, series.RenderPoints[point * stride + 3].Y >= series.RenderPoints[point * stride].Y, colors);
                var paint = VisualChartPaint.Series(series, color, point);
                if (colorScales[0] is ChartSeriesColorSurface surface) (color, paint) = surface.Paint(series, 0, point, colors);
                entries.Add(new VisualLegendEntry(label, color, PointId(0, point), series.Kind, pattern, series.StateRole, series.InteractionIdentityKey,
                    marker: FinancialLegend(chart, series, 0, colors, point, pattern)
                        ?? VisualMarkerScene.Legend(chart, series, color, paint, point, pattern, bubbleScale), paint: paint));
            }
            return entries;
        }
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.ShowInLegend) {
                var color = Color(series, index, colors); var paint = VisualChartPaint.Series(series, color);
                entries.Add(new VisualLegendEntry(series.Name, color, SeriesId(index), series.Kind, series.FillPattern, series.StateRole,
                    series.InteractionIdentityKey, marker: FinancialLegend(chart, series, index, colors, pattern: series.FillPattern)
                        ?? VisualMarkerScene.Legend(chart, series, color, paint, pattern: series.FillPattern, bubbleScale: bubbleScale), paint: paint));
            }
        }
        return entries;
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRect? viewport = null) {
        BuildCore(chart, context, builder, plot, viewport ?? new ChartRect(0, 0, builder.Size.Width, builder.Size.Height), false, null, null, null, 0);
    }

    internal static ChartCartesianProjection? BuildInViewport(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ChartBubbleSizeScale? bubbleScale = null, ChartAxisValueFormatter.Cache? categoryLabels = null, ChartColorPresentationSet? colorScales = null, double legendHeight = 0, ChartCartesianViewport? sourceViewport = null, ChartCartesianFullDomain? fullDomain = null)
        => BuildCore(chart, context, builder, viewport, viewport, true, bubbleScale, categoryLabels, colorScales, legendHeight, sourceViewport, fullDomain);

    private static ChartCartesianProjection? BuildCore(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRect viewport,
        bool measureAxes, ChartBubbleSizeScale? bubbleScale, ChartAxisValueFormatter.Cache? categoryLabels, ChartColorPresentationSet? colorScales, double legendHeight, ChartCartesianViewport? sourceViewport = null, ChartCartesianFullDomain? fullDomain = null) {
        Validate(chart);
        var xViewport = sourceViewport?.X; var yViewport = sourceViewport?.Y;
        if (sourceViewport != null) ValidateViewport(chart, sourceViewport);
        if (bubbleScale == null && chart.Series.Any(series => series.Kind == ChartSeriesKind.Bubble)) bubbleScale = ChartBubbleSizeScale.Create(chart);
        var colors = context.Theme.Resolve(context.ThemeMode);
        colorScales ??= new ChartColorPresentationSet(chart);
        viewport = NumericScaleViewport(chart, context, builder, viewport, colorScales, legendHeight, bubbleScale, out var sizeKey);
        if (!measureAxes) plot = new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, Math.Min(plot.Height, viewport.Bottom - plot.Top)));
        var coordinates = ChartBarCoordinateMap.Create(chart);
        var stacks = ChartStackLayout.Create(chart, coordinates);
        if (!chart.Series.Any(series => series.HasSourceData) && chart.Annotations.Count == 0 && chart.PlotRegions.Count == 0) {
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.no-data", "The chart has no observations."));
            builder.Text(chart.Options.Labels.NoData, plot.Left + plot.Width / 2, plot.Top + plot.Height / 2,
                context.Theme.Typography.DataLabelSize, colors.MutedForeground, role: "no-data", alignment: TextAlignment.Center, paint: SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text));
            return null;
        }
        foreach (var total in stacks.Totals.Where(total => total.NormalizedTo.HasValue && total.SourceValue == 0))
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.stack-zero-total", "Normalized stack '" + (total.Group ?? "default")
                + "' on the " + total.Axis.ToString().ToLowerInvariant() + " axis at " + Number(total.Coordinate)
                + " has a zero " + (total.Positive ? "positive" : "negative") + " total and remains at the baseline with its source observations retained."));
        var range = ChartRange.FromChart(chart, coordinates, stacks);
        ChartTimeSessionValidation.ValidateWindow(chart.Options.XAxis, range.MinX, range.MaxX, "X axis");
        var hasSecondary = chart.Series.Any(series => series.YAxis == ChartAxisSide.Secondary);
        var secondaryRange = hasSecondary ? ChartRange.FromSecondaryYAxis(chart, range, stacks) : null;
        fullDomain?.Apply(chart, range, secondaryRange);
        var labels = new CartesianLabels();
        var categoryGroups = CategoryGroupSource.Create(chart, range);
        var completedDomain = sourceViewport != null && fullDomain != null;
        // An aligned primary-only envelope already includes both drawings' full marker fit. Re-solving that fit
        // here both duplicates layout work and can move a completed endpoint by one floating-point step.
        CartesianAxisLayout? layout = null;
        if (!completedDomain || fullDomain!.Y == null) {
            layout = ResolveAxisLayout(chart, context, builder, plot, viewport, measureAxes, range, secondaryRange,
                stacks, coordinates, bubbleScale, colors, categoryLabels, labels, categoryGroups, expandMarkers: true);
            if (layout == null) {
                if (sourceViewport != null) throw new InvalidOperationException("A Cartesian viewport requires a usable plot rectangle.");
                return null;
            }
        }
        if (completedDomain) {
            // Secondary-axis charts still require their ordinary Y fit; their completed shared X bound remains authoritative.
            range.SetXBounds(fullDomain!.X.Minimum, fullDomain.X.Maximum);
            secondaryRange?.SetXBounds(fullDomain.X.Minimum, fullDomain.X.Maximum);
        }
        var fullMinimum = range.MinX; var fullMaximum = range.MaxX;
        var fullYMinimum = range.MinY; var fullYMaximum = range.MaxY;
        var hasProjection = SupportsXViewport(chart) && chart.Series.Any(series => series.HasSourceData)
            && ChartMath.IsFinite(fullMinimum) && ChartMath.IsFinite(fullMaximum) && fullMaximum > fullMinimum;
        if (sourceViewport != null) {
            if (!hasProjection) throw new InvalidOperationException("A Cartesian viewport requires a finite increasing full X domain.");
            if (xViewport != null) {
                ChartTimeSessionValidation.ValidateWindow(chart.Options.XAxis, xViewport.Minimum, xViewport.Maximum, "viewport");
                if (xViewport.Minimum < fullMinimum || xViewport.Maximum > fullMaximum)
                    throw new ArgumentOutOfRangeException("viewport", "The X viewport must be contained in the resolved full X domain.");
            }
            if (yViewport != null && (yViewport.Minimum < fullYMinimum || yViewport.Maximum > fullYMaximum))
                throw new ArgumentOutOfRangeException("viewport", "The Y viewport must be contained in the resolved full primary Y domain.");
            // Copy resolved numbers only. Authored axes, full-data stacks/color domains and source facts remain untouched.
            range = range.Copy(); secondaryRange = secondaryRange?.Copy();
            if (xViewport != null) { range.SetXBounds(xViewport.Minimum, xViewport.Maximum); secondaryRange?.SetXBounds(xViewport.Minimum, xViewport.Maximum); }
            if (yViewport != null) range.SetYBounds(yViewport.Minimum, yViewport.Maximum);
            layout = ResolveAxisLayout(chart, context, builder, plot, viewport, measureAxes, range, secondaryRange,
                stacks, coordinates, bubbleScale, colors, categoryLabels, labels, categoryGroups, expandMarkers: false)
                ?? throw new InvalidOperationException("A Cartesian viewport requires a usable plot rectangle.");
        }
        var resolvedLayout = layout ?? throw new InvalidOperationException("A Cartesian viewport requires a usable plot rectangle.");
        plot = resolvedLayout.Plot;
        sizeKey?.Draw(builder, plot);
        var labelBounds = resolvedLayout.LabelBounds; var axisLabels = resolvedLayout.AxisLabels;
        var horizontal = resolvedLayout.Horizontal; var categoryCaptions = resolvedLayout.CategoryCaptions;
        var horizontalTotals = resolvedLayout.HorizontalTotals; var verticalTotals = resolvedLayout.VerticalTotals;
        var clipMarks = sourceViewport != null || chart.Options.ClipMarksToPlot;
        // Capture the final mapping extent after every measured axis and total lane.
        // Hosts project this logical rectangle; mark clipping remains an independent choice.
        builder.AddRegion(new VisualSemanticRegion("cartesian-plot", "cartesian-plot", plot));
        var map = horizontal ? ChartMapper.ForHorizontalBars(plot, range, chart.Options.XAxis, chart.Options.YAxis) : new ChartMapper(plot, range, chart.Options.XAxis, chart.Options.YAxis);
        var hasPrimaryDomain = chart.Series.Any(series => series.YAxis == ChartAxisSide.Primary && series.HasSourceData)
            || chart.Annotations.Any(annotation => ChartCartesianOrientation.AnnotationKind(annotation, horizontal) is ChartAnnotationKind.HorizontalLine or ChartAnnotationKind.HorizontalBand)
            || chart.PlotRegions.Any(region => region.YAxis == ChartAxisSide.Primary && (region.YMinimum.HasValue || region.YMaximum.HasValue));
        var hasSecondaryDomain = chart.Series.Any(series => series.YAxis == ChartAxisSide.Secondary && series.HasSourceData)
            || chart.PlotRegions.Any(region => region.YAxis == ChartAxisSide.Secondary && (region.YMinimum.HasValue || region.YMaximum.HasValue));
        var secondaryOnly = hasSecondaryDomain && !hasPrimaryDomain;
        // A screen coordinate has no single value-domain meaning when both Y axes contain observations.
        builder.DataProjection = ChartDataProjection.TryCreate(plot, secondaryOnly ? secondaryRange! : range,
            chart.Options.XAxis, secondaryOnly ? chart.Options.SecondaryYAxis : chart.Options.YAxis, horizontal,
            includeY: !(hasPrimaryDomain && hasSecondaryDomain));
        var secondaryMap = secondaryRange == null ? null : new ChartMapper(plot, secondaryRange, chart.Options.XAxis, chart.Options.SecondaryYAxis);
        if (xViewport != null && chart.Series.Any(series => series.RenderPoints.Any(point => !ChartMath.IsFinite(map.X(point.X)))))
            throw new ArgumentOutOfRangeException("viewport", "The X viewport cannot project the source coordinates into finite logical units.");
        if (yViewport != null) ValidateFiniteYProjection(chart, map);
        var plotRegions = ResolvePlotRegions(chart, range, secondaryRange, map, secondaryMap);
        DrawPlotRegionFills(builder, plotRegions, colors);
        var axisBuilder = !SameAxisFrame(plot, labelBounds)
            || chart.Annotations.Any(annotation => annotation.ShowLabel && annotation.Label.Length > 0)
            ? new VisualSceneBuilder(builder.Size, context.Font) : builder;
        using (axisBuilder.PushClip(viewport)) {
            if (horizontal) DrawHorizontalAxes(chart, context, axisBuilder, plot, range, map, colors, viewport, axisLabels, labelBounds.Left);
            else DrawAxes(chart, context, axisBuilder, plot, range, map, secondaryRange, secondaryMap, colors, viewport, axisLabels, labelBounds);
        }
        var obstacles = new List<LabelObstacle>();
        if (!ReferenceEquals(axisBuilder, builder)) {
            var axes = axisBuilder.Build();
            builder.Append(axes);
            CaptureAxisTextObstacles(axes, obstacles);
        }
        // The owned viewport starts after the measured frame and legend. Its upper
        // title strip can hold a complete guide caption without moving either axis.
        var annotationBounds = new ChartRect(plot.Left, viewport.Top,
            hasSecondary ? plot.Width : Math.Max(plot.Width, viewport.Right - plot.Left), plot.Bottom - viewport.Top);
        // Caption placement needs the actual marks before the background band layer is
        // emitted. Collect those marks once; ordinary charts keep the direct builder path.
        var seriesBuilder = chart.Annotations.Any(annotation => annotation.IsBand && annotation.ShowLabel && annotation.Label.Length > 0)
            ? new VisualSceneBuilder(builder.Size, context.Font) : builder;
        {
            if (ReferenceEquals(seriesBuilder, builder)) {
                DrawAnnotations(chart, context, builder, plot, map, colors, true, obstacles, constrainXToPlot: xViewport != null,
                    xViewport: xViewport, labelBounds: annotationBounds, clipGeometry: clipMarks, secondaryMap: secondaryMap);
            }
            foreach (var index in ChartSeriesColours.DrawingOrder(chart)) {
                var series = chart.Series[index];
                var seriesMap = series.YAxis == ChartAxisSide.Secondary ? secondaryMap! : map;
                var pointSeries = IsPointSeries(series.Kind);
                using (clipMarks && (!pointSeries || sourceViewport != null) ? seriesBuilder.PushClip(plot) : null)
                using (seriesBuilder.PushGroup(SeriesId(index), "series", new Dictionary<string, string> {
                    ["data-cfx-coordinate-system"] = "cartesian",
                    ["data-cfx-series"] = Number(index), ["data-cfx-series-key"] = series.InteractionIdentityKey,
                    ["data-cfx-series-name"] = series.Name, ["data-cfx-state"] = series.StateRole.ToString().ToLowerInvariant(),
                    ["data-cfx-pin-state-colors"] = chart.Options.PinStateColorsInForcedColors && series.StateRole != ChartSeriesState.None ? "true" : "false",
                    ["data-cfx-kind"] = series.Kind.ToString(), ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
                    ["data-cfx-source-points"] = Number(series.SourcePointCount),
                    ["data-cfx-rendered-points"] = Number(series.RenderedPointCount),
                    ["data-cfx-decimation"] = series.DecimationMode?.ToString() ?? string.Empty, ["aria-label"] = series.Name
                })) {
                    if (series.Kind == ChartSeriesKind.Bar) DrawBars(chart, context, seriesBuilder, plot, coordinates, seriesMap, stacks, index, colors, labels, obstacles);
                    else if (pointSeries)
                        DrawPoints(chart, context, seriesBuilder, plot, seriesMap, stacks, index, colors, labels, obstacles, colorScales[index], clipMarks, xViewport, yViewport);
                    else DrawExtensionSeries(chart, context, seriesBuilder, plot, seriesMap, stacks, index, colors, labels, obstacles, bubbleScale, categoryCaptions, colorScales[index], xViewport, yViewport);
                }
            }
            if (!ReferenceEquals(seriesBuilder, builder)) {
                DrawAnnotations(chart, context, builder, plot, map, colors, true, obstacles, constrainXToPlot: xViewport != null,
                    xViewport: xViewport, labelBounds: annotationBounds, clipGeometry: clipMarks, secondaryMap: secondaryMap);
                builder.Append(seriesBuilder.Build());
            }
            DrawAnnotations(chart, context, builder, plot, map, colors, false, obstacles, constrainXToPlot: xViewport != null,
                xViewport: xViewport, labelBounds: annotationBounds, clipGeometry: clipMarks, secondaryMap: secondaryMap);
        }
        if (chart.Options.ShowStackTotals && stacks.HasBarStacks) {
            if (horizontal) AddHorizontalTotals(chart, horizontalTotals, context, builder, plot, map, stacks, labels);
            else AddStackTotals(chart, context, builder, plot, coordinates, stacks, map, secondaryMap, labels, verticalTotals!);
        }
        DrawPlotRegionLabels(context, builder, plotRegions, colors, obstacles);
        using (sourceViewport != null ? builder.PushClip(plot) : null)
            DrawDataLabels(context, builder, labelBounds, labels, obstacles);
        CaptureReadouts(chart, builder, range, secondaryRange, axisLabels, horizontal, secondaryOnly);
        return hasProjection ? new ChartCartesianProjection(
            new ChartXProjection(fullMinimum, fullMaximum, range.MinX, range.MaxX, plot, chart.Options.XAxis.Reversed,
                chart.Options.XAxis.Scale, chart.Options.XAxis.TimeSessions),
            SupportsYViewport(chart) ? new ChartYProjection(fullYMinimum, fullYMaximum, range.MinY, range.MaxY, plot, chart.Options.YAxis) : null) : null;
    }

    private static void Validate(Chart chart) {
        if (chart.Series.Any(series => series.Kind == ChartSeriesKind.BoxPlot && ChartCartesianOrientation.IsHorizontal(chart, series))
            && chart.Series.Any(series => series.Kind != ChartSeriesKind.BoxPlot || series.YAxis != ChartAxisSide.Primary))
            throw new NotSupportedException("Horizontal box plots require only box plot series on the primary X value axis.");
        if (chart.Series.Any(series => series.Kind == ChartSeriesKind.RangeBar && ChartCartesianOrientation.IsHorizontal(chart, series))
            && chart.Series.Any(series => series.Kind != ChartSeriesKind.RangeBar || series.YAxis != ChartAxisSide.Primary))
            throw new NotSupportedException("Horizontal range-bar charts require only range-bar series on the primary X value axis.");
        if (ChartCartesianOrientation.IsHorizontal(chart) && chart.Series.Any(series => !ChartCartesianOrientation.IsHorizontal(chart, series)
            || series.YAxis != ChartAxisSide.Primary))
            throw new NotSupportedException("Horizontal Cartesian charts require homogeneous series on the primary horizontal value axis.");
        if (chart.Series.Any(series => ChartCartesianOrientation.IsHorizontalLine(chart, series))
            && chart.Options.YAxis.Scale != ChartScaleKind.Linear && chart.Options.YAxis.Scale != ChartScaleKind.Time)
            throw new NotSupportedException("Horizontal line charts require a linear or time category axis.");
        foreach (var series in chart.Series) {
            if (series.TimeBucketInterval != null && chart.Options.XAxis.Scale != ChartScaleKind.Time)
                throw new InvalidOperationException("Time buckets require a time X axis.");
            if (!Supports(series.Kind)) throw Unsupported("series kind " + series.Kind);
            if (series.RenderPoints.Count % ObservationStride(series.Kind) != 0)
                throw new InvalidOperationException(series.Kind + " requires complete observation tuples.");
            if ((series.Kind == ChartSeriesKind.Slope || series.Kind == ChartSeriesKind.TrendLine) && series.RenderPoints.Count != 2)
                throw new InvalidOperationException(series.Kind + " requires exactly two endpoints.");
            if (series.Kind == ChartSeriesKind.Waterfall && chart.Series.Count != 1)
                throw new NotSupportedException("A waterfall requires a single cumulative series.");
            var axis = series.YAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            if (series.Kind == ChartSeriesKind.StackedArea && axis.Scale == ChartScaleKind.Logarithmic && series.RenderPoints.Any(point => point.Y <= 0))
                throw new InvalidOperationException("Logarithmic stacked areas require positive values.");
        }
    }

    private static NotSupportedException Unsupported(string feature) => new("The Cartesian compiler does not support " + feature + ".");
    private static ChartColor Color(ChartSeries series, int index, VisualThemeColors colors) => ChartSeriesColours.Resolve(series, index, colors);
    private static ChartColor PointColor(ChartSeries series, int seriesIndex, int pointIndex, VisualThemeColors colors) => ChartSeriesColours.Point(series, seriesIndex, pointIndex, colors);
    private static string SeriesId(int index) => "series-" + Number(index);
    private static string PointId(int series, int point) => SeriesId(series) + "-point-" + Number(point);
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    // Non-paint ownership uses observation indexes, including one index per paired observation.
    // Fill and hatch share the range even when disconnected segments repeat coordinates.
    private static IDisposable FillSegment(VisualSceneBuilder builder, int start, int count) =>
        builder.PushGroup(null, "fill-segment", new Dictionary<string, string> {
            ["data-cfx-fill-point-start"] = Number(start), ["data-cfx-fill-point-count"] = Number(count)
        });

    private static IDisposable PointGroup(VisualSceneBuilder builder, ChartSeries series, int seriesIndex, int pointIndex, ChartRect bounds, ResolvedPointLabel resolvedLabel, ChartLabels chartLabels, ChartStackPoint? stack = null, ChartSeriesColorSurface? colorSurface = null, bool visible = true) {
        var point = series.RenderPoints[pointIndex];
        var id = PointId(seriesIndex, pointIndex);
        var label = series.Name + ": " + resolvedLabel.DisplayedText + " (" + ChartNumericFormatter.FormatCoordinate(point.X) + ", " + ChartNumericFormatter.FormatCoordinate(point.Y) + ")";
        if (series.HistogramBinLayout != null) label = HistogramPointDescription(series, pointIndex, resolvedLabel.DisplayedText);

        var metadata = new Dictionary<string, string> {
            ["data-cfx-series"] = Number(seriesIndex), ["data-cfx-point"] = Number(pointIndex),
            ["data-cfx-source-point"] = Number(pointIndex < series.SourcePointIndices.Count ? series.SourcePointIndices[pointIndex] : pointIndex),
            ["data-cfx-x"] = Number(point.X), ["data-cfx-y"] = Number(point.Y),
            ["data-cfx-state"] = series.StateRole.ToString().ToLowerInvariant(), ["data-cfx-axis"] = series.YAxis.ToString().ToLowerInvariant(),
            ["data-cfx-fill-pattern"] = (pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
                ? series.PointFillPatterns[pointIndex]!.Value : series.FillPattern).ToString(),
            ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
            ["data-cfx-label"] = resolvedLabel.DisplayedText, ["aria-label"] = label
        };
        if (resolvedLabel.NumericCaption("y") is { } numericCaption) metadata["data-cfx-formatted-y"] = numericCaption;
        // Retained off-window facts must not acquire browser hit areas. Marker-free in-window points remain targets.
        if (!visible) metadata["data-cfx-visible"] = "false";
        if (colorSurface != null) label = colorSurface.AddFacts(metadata, pointIndex, label);
        metadata["aria-label"] = label;
        builder.AddRegion(new VisualSemanticRegion(id, "point", bounds, label));
        if (stack.HasValue) ChartStackLayout.AddMetadata(metadata, stack.Value);
        if (series.HistogramBinLayout != null) AddHistogramMetadata(metadata, series, pointIndex, chartLabels);
        return builder.PushGroup(id, "point", metadata);
    }

}
