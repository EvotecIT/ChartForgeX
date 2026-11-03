using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    internal static void ValidatePreparedXZoom(Chart chart) {
        ValidateXViewport(chart);
        if (ChartCartesianOrientation.IsHorizontal(chart) || chart.Options.XAxis.Scale != ChartScaleKind.Linear
            || chart.Series.Any(series => series.Kind is not (ChartSeriesKind.Line or ChartSeriesKind.StepLine)
                || series.Interpolation is not (ChartInterpolation.Linear or ChartInterpolation.Step))
            || chart.Annotations.Any(annotation => annotation.HasSourceGeometry)
            || chart.Options.XAxis.CrossAt.HasValue || chart.Options.YAxis.CrossAt.HasValue || chart.Options.SecondaryYAxis.CrossAt.HasValue
            || chart.Options.XAxis.RuleCrossAt.HasValue || chart.Options.YAxis.RuleCrossAt.HasValue || chart.Options.SecondaryYAxis.RuleCrossAt.HasValue)
            throw new NotSupportedException("Prepared X zoom requires vertical Line/StepLine series with linear X, linear/step interpolation, ordinary guides and outer axes.");
    }

    internal static PreparedChartXZoom PrepareXZoom(Chart chart, VisualRenderContext context, VisualScene frame, ChartRect viewport,
        ChartAxisValueFormatter.Cache formatterCache, ChartColorPresentationSet colorScales, ChartXZoomLevels levels,
        VisualAccessibility accessibility, VisualSvgOptions svgOptions, double legendHeight) {
        Validate(chart); ValidatePreparedXZoom(chart);
        var colors = context.Theme.Resolve(context.ThemeMode);
        var frameBuilder = new VisualSceneBuilder(frame.Size, context.Font);
        frameBuilder.Append(frame);
        viewport = NumericScaleViewport(chart, context, frameBuilder, viewport, colorScales, legendHeight, null, out _);
        frame = frameBuilder.Build();
        var coordinates = ChartBarCoordinateMap.Create(chart);
        var stacks = ChartStackLayout.Create(chart, coordinates);
        var sourceRange = ChartRange.FromChart(chart, coordinates, stacks);
        var sourceSecondary = chart.Series.Any(series => series.ValueAxis == ChartAxisSide.Secondary)
            ? ChartRange.FromSecondaryValueAxis(chart, sourceRange, stacks) : null;
        var layoutBuilder = new VisualSceneBuilder(frame.Size, context.Font);
        var layout = ResolveXZoomLayout(chart, context, layoutBuilder, viewport, sourceRange, sourceSecondary, stacks, colors, formatterCache, levels);
        var range = layout.Range; var secondaryRange = layout.SecondaryRange; var plot = layout.Plot;
        var fullViewport = new ChartAxisWindow(range.MinX, range.MaxX);
        var resolvedPointLabels = chart.Series.Select(series => {
            var style = SeriesLabelStyle(chart, context, series, colors);
            var format = SeriesCaptionFormat(chart, series);
            return (IReadOnlyList<ResolvedPointLabel>)Enumerable.Range(0, series.RenderPoints.Count)
                .Select(index => ResolvePointLabel(chart, series, index, style, format)).ToArray();
        }).ToArray();
        var yProjection = new ChartAxisProjection(range.MinY, range.MaxY, range.MinY, range.MaxY,
            plot, chart.Options.YAxis, ChartAxisDimension.Y);
        var secondaryYProjection = secondaryRange == null ? null : new ChartAxisProjection(secondaryRange.MinY, secondaryRange.MaxY,
            secondaryRange.MinY, secondaryRange.MaxY, plot, chart.Options.SecondaryYAxis, ChartAxisDimension.Y, ChartAxisSide.Secondary);
        var bank = new List<PreparedCartesianXLevel>();
        for (var index = 0; index < levels.Count; index++) {
            var factor = levels[index];
            var zoomCoordinates = new CartesianXZoomCoordinates(range.MinX, range.MaxX, plot, factor, chart.Options.XAxis.Reversed);
            var track = zoomCoordinates.Track;
            var map = new ChartMapper(track, range, chart.Options.XAxis, chart.Options.YAxis);
            var secondaryMap = secondaryRange == null ? null : new ChartMapper(track, secondaryRange, chart.Options.XAxis, chart.Options.SecondaryYAxis);
            if (chart.Series.Any(series => series.Points.Any(point => !ChartMath.IsFinite(map.X(point.X)))))
                throw new ArgumentOutOfRangeException(nameof(levels), "An X zoom level cannot project all source X coordinates into finite logical units.");
            var layers = new CartesianXZoomLayers(frame.Size, context.Font);
            layers.Add(frame, CartesianXMotion.Fixed);
            layers.Draw(CartesianXMotion.Fixed, null, builder => builder.AddRegion(new VisualSemanticRegion("cartesian-plot", "cartesian-plot", plot)));
            var fixedMap = new ChartMapper(plot, range, chart.Options.XAxis, chart.Options.YAxis);
            var fixedSecondary = secondaryRange == null ? null : new ChartMapper(plot, secondaryRange, chart.Options.XAxis, chart.Options.SecondaryYAxis);
            var movingRegions = ResolvePlotRegions(chart, range, secondaryRange, map, secondaryMap);
            var fixedRegions = ResolvePlotRegions(chart, range, secondaryRange, fixedMap, fixedSecondary);
            for (var regionIndex = 0; regionIndex < movingRegions.Count; regionIndex++) {
                var moving = HasXTrack(movingRegions[regionIndex].Source);
                var region = moving ? movingRegions[regionIndex] : fixedRegions[regionIndex];
                layers.Draw(moving ? CartesianXMotion.XTrack : CartesianXMotion.Fixed, plot, builder => DrawPlotRegionFills(context, builder, new[] { region }, colors, sourceXAdmission: moving));
            }
            var labels = layout.Labels[index];
            DrawXZoomAxes(chart, context, layers, plot, track, range, map, secondaryRange, secondaryMap, colors, viewport, labels, layout.TickHeight);

            // Collect native series ink before placing background-band captions; retain drawing order when appending.
            var movingObstacles = new List<LabelObstacle>(); var fixedObstacles = new List<LabelObstacle>();
            var dataLabels = new CartesianLabels(); var seriesScenes = new List<VisualScene>();
            foreach (var seriesIndex in ChartSeriesColours.DrawingOrder(chart)) {
                var builder = new VisualSceneBuilder(frame.Size, context.Font);
                using (builder.PushClip(track))
                using (SeriesGroup(builder, chart, seriesIndex))
                    DrawPoints(chart, context, builder, track, chart.Series[seriesIndex].ValueAxis == ChartAxisSide.Secondary ? secondaryMap! : map,
                        stacks, seriesIndex, colors, dataLabels, movingObstacles, colorScales[seriesIndex], true, pointLabels: resolvedPointLabels[seriesIndex], zoom: layers);
                builder.CaptureChartIdentity(chart, new ChartCartesianProjection(
                    new ChartAxisProjection(range.MinX, range.MaxX, range.MinX, range.MaxX, track, chart.Options.XAxis, ChartAxisDimension.X),
                    yProjection, secondaryY: secondaryYProjection));
                var series = chart.Series[seriesIndex];
                builder.CaptureXZoomSourcePoints(Enumerable.Range(0, series.RenderPoints.Count)
                    .ToDictionary(point => PointId(seriesIndex, point), point => series.RenderPoints[point].X, StringComparer.Ordinal));
                var scene = builder.Build(); seriesScenes.Add(scene); layers.RegisterMarks(scene, CartesianXMotion.XTrack, track);
            }
            DrawXZoomAnnotations(chart, context, layers, plot, track, fixedMap, map, colors, true, fixedObstacles, movingObstacles, fullViewport);
            foreach (var scene in seriesScenes) layers.Add(scene, CartesianXMotion.XTrack, plot);
            DrawXZoomAnnotations(chart, context, layers, plot, track, fixedMap, map, colors, false, fixedObstacles, movingObstacles, fullViewport);
            for (var regionIndex = 0; regionIndex < movingRegions.Count; regionIndex++) {
                var moving = HasXTrack(movingRegions[regionIndex].Source);
                var region = moving ? movingRegions[regionIndex] : fixedRegions[regionIndex];
                var motion = moving ? CartesianXMotion.XTrack : CartesianXMotion.Fixed;
                layers.Draw(motion, plot, builder => DrawPlotRegionLabels(context, builder, new[] { region }, colors,
                    moving ? movingObstacles : fixedObstacles, layers, motion, plot));
            }
            layers.Draw(CartesianXMotion.XTrack, plot, builder => DrawDataLabels(context, builder, track, dataLabels, movingObstacles, layers, plot));
            bank.Add(layers.Seal(zoomCoordinates, layout.Ticks[index]));
        }
        return new PreparedChartXZoom(levels, fullViewport, plot, bank, accessibility, svgOptions, yProjection, secondaryYProjection);
    }

    private static bool HasXTrack(ChartPlotRegion region) => region.XMinimum.HasValue || region.XMaximum.HasValue;

    private static void DrawXZoomAxes(Chart chart, VisualRenderContext context, CartesianXZoomLayers layers, ChartRect plot, ChartRect track,
        ChartRange range, ChartMapper map, ChartRange? secondaryRange, ChartMapper? secondaryMap, VisualThemeColors colors,
        ChartRect viewport, AxisLabelCache labels, double tickHeight) {
        void Draw(AxisParts parts, CartesianXMotion motion, ChartRect clip) => layers.Draw(motion, clip, builder =>
            DrawAxes(chart, context, builder, plot, range, map, secondaryRange, secondaryMap, colors, viewport, labels, plot, parts, track, tickHeight,
                parts is AxisParts.XLabels or AxisParts.XGrid ? layers : null));
        Draw(AxisParts.YGrid, CartesianXMotion.Fixed, plot);
        Draw(AxisParts.XGrid, CartesianXMotion.XTrack, plot);
        Draw(AxisParts.XFrame, CartesianXMotion.Fixed, viewport);
        Draw(AxisParts.XLabels, CartesianXMotion.XTrack, new ChartRect(plot.Left, plot.Bottom, plot.Width, Math.Max(0, viewport.Bottom - plot.Bottom)));
        Draw(AxisParts.YAxes, CartesianXMotion.Fixed, viewport);
    }

    private static void DrawXZoomAnnotations(Chart chart, VisualRenderContext context, CartesianXZoomLayers layers, ChartRect plot, ChartRect track,
        ChartMapper fixedMap, ChartMapper movingMap, VisualThemeColors colors, bool bands, List<LabelObstacle> fixedObstacles, List<LabelObstacle> movingObstacles,
        ChartAxisWindow fullViewport) {
        for (var i = 0; i < chart.Annotations.Count; i++) {
            var index = i; var source = chart.Annotations[index];
            if (source.EndValue.HasValue != bands) continue;
            var moving = source.Kind is ChartAnnotationKind.VerticalLine or ChartAnnotationKind.VerticalBand;
            layers.Draw(moving ? CartesianXMotion.XTrack : CartesianXMotion.Fixed, plot, builder =>
                VisualAnnotationCompiler.Draw(chart.Annotations, context, builder, moving ? track : plot,
                    (moving ? movingMap : fixedMap).X, (moving ? movingMap : fixedMap).Y, colors, bands,
                    moving ? movingObstacles : fixedObstacles, include: annotation => ReferenceEquals(annotation, source),
                    isVisible: annotation => !moving || IsAnnotationVisible(annotation, movingMap, fullViewport, null, annotation.Kind),
                    zoom: layers, outputPlot: plot), marks: !moving && !bands);
        }
    }

    private sealed class XZoomLayout {
        internal XZoomLayout(ChartRect plot, ChartRange range, ChartRange? secondaryRange, IReadOnlyList<AxisLabelCache> labels,
            IReadOnlyList<IReadOnlyList<double>> ticks, double tickHeight) {
            Plot = plot; Range = range; SecondaryRange = secondaryRange; Labels = labels; Ticks = ticks; TickHeight = tickHeight;
        }
        internal ChartRect Plot { get; }
        internal ChartRange Range { get; }
        internal ChartRange? SecondaryRange { get; }
        internal IReadOnlyList<AxisLabelCache> Labels { get; }
        internal IReadOnlyList<IReadOnlyList<double>> Ticks { get; }
        internal double TickHeight { get; }
    }

    private static XZoomLayout ResolveXZoomLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ChartRange sourceRange, ChartRange? sourceSecondary, ChartStackLayout stacks, VisualThemeColors colors,
        ChartAxisValueFormatter.Cache formatterCache, ChartXZoomLevels levels) {
        var reserved = viewport;
        // Reservation only grows. Each pass derives domains afresh against that exact plot; padding never compounds.
        for (var pass = 0; pass < 8; pass++) {
            var range = sourceRange.Copy(); var secondaryRange = sourceSecondary?.Copy();
            ExpandMarkerRanges(chart, context, reserved, range, secondaryRange, stacks, null);
            var caches = new List<AxisLabelCache>(); var ticks = new List<IReadOnlyList<double>>();
            var next = reserved; var tickHeight = 0d;
            foreach (var factor in levels) {
                var cache = new AxisLabelCache(formatterCache);
                var trackTicks = ChartTicks.ForXTrack(chart.Options.XAxis, range.MinX, range.MaxX, factor);
                cache.SetTicks(chart.Options.XAxis, trackTicks);
                cache.IncludeValueTicks(chart.Options.YAxis, range.MinY, range.MaxY);
                if (secondaryRange != null) cache.IncludeValueTicks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
                var measured = MeasurePlot(chart, context, builder, viewport, range, secondaryRange, colors, cache);
                next = new ChartRect(Math.Max(next.Left, measured.Left), Math.Max(next.Top, measured.Top),
                    Math.Max(0, Math.Min(next.Right, measured.Right) - Math.Max(next.Left, measured.Left)),
                    Math.Max(0, Math.Min(next.Bottom, measured.Bottom) - Math.Max(next.Top, measured.Top)));
                var style = chart.Options.TickLabelStyle.Resolve(new TextStyle { Font = context.Font,
                    FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground });
                tickHeight = Math.Max(tickHeight, TickMetrics(builder, chart.Options.XAxis, range.MinX, range.MaxX, style, null, cache).Height);
                caches.Add(cache); ticks.Add(trackTicks);
            }
            if (next.Width <= 0 || next.Height <= 0) throw new InvalidOperationException("An X zoom bank requires a usable plot rectangle.");
            if (next.Equals(reserved)) return new XZoomLayout(reserved, range, secondaryRange, caches, ticks, tickHeight);
            reserved = next;
        }
        throw new InvalidOperationException("The X zoom bank could not reserve a stable plot for its formatted ticks.");
    }
}
