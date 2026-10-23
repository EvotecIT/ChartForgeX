using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

internal static partial class ChartGuards {
    public static void Finite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
    }

    public static void UnitInterval(double value, string parameterName) {
        Finite(value, parameterName);
        if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be between zero and one.");
    }

    public static List<ChartPoint> Points(IEnumerable<ChartPoint> points, string parameterName) {
        if (points == null) throw new ArgumentNullException(parameterName);
        var materialized = points.ToList();
        for (var i = 0; i < materialized.Count; i++) {
            Finite(materialized[i].X, parameterName + "[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "].X");
            Finite(materialized[i].Y, parameterName + "[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "].Y");
        }

        return materialized;
    }

    public static List<ChartPoint> Values(IEnumerable<double> values, string parameterName) {
        if (values == null) throw new ArgumentNullException(parameterName);
        var points = new List<ChartPoint>();
        var index = 1;
        foreach (var value in values) {
            Finite(value, parameterName + "[" + (index - 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "]");
            points.Add(new ChartPoint(index, value));
            index++;
        }

        return points;
    }

    // Native preparation represents supported empty/all-zero data with a no-data scene.
    // The default retains the stricter preconditions of remaining legacy model callers.
    public static void RenderCompatibility(Chart chart, bool preparing = false) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.ValidateHourWeekdayHeatmapOwnership();
        chart.ValidateExplicitGanttDependencies();
        ValidateRenderableChart(chart, preparing);
        ValidateSourceAnnotations(chart);
        ChartTimeSessionValidation.Validate(chart);
        _ = ChartCategoryGroupPlan.TryCreate(chart);
        chart.ValidatePlotRegions();
        if (chart.Options.MapColorDimension.HasOverrides && chart.Series.Any(series => series.Kind != ChartSeriesKind.RegionMap && series.Kind != ChartSeriesKind.TileMap && series.Kind != ChartSeriesKind.DottedMap))
            throw new InvalidOperationException("Map color presentation requires numeric region, tile or dotted maps.");
        if (chart.Options.HeatmapColorDimension.HasOverrides && chart.Series.Any(series => series.IsCategoricalHeatmapRow
            || (series.Kind != ChartSeriesKind.Heatmap && series.Kind != ChartSeriesKind.HexbinHeatmap && series.Kind != ChartSeriesKind.CalendarHeatmap)))
            throw new InvalidOperationException("Heatmap color presentation requires numeric rectangular, hexagonal or calendar heatmaps.");
        if (chart.Options.Treemap.ColorDimension.HasOverrides && chart.Series.Any(series => series.Kind != ChartSeriesKind.Treemap))
            throw new InvalidOperationException("Treemap color presentation requires a Treemap chart.");
        if (chart.Options.Sunburst.ColorDimension.HasOverrides && chart.Series.Any(series => series.Kind != ChartSeriesKind.Sunburst))
            throw new InvalidOperationException("Sunburst color presentation requires a Sunburst chart.");
        if ((chart.Options.Chord.ColorScale != null || chart.Options.Chord.ColorDimension.HasOverrides)
            && chart.Series.Any(series => series.Kind != ChartSeriesKind.Chord))
            throw new InvalidOperationException("Chord flow color presentation requires a Chord chart.");
        if (chart.Series.Count > 0 && chart.Series[0].Kind == ChartSeriesKind.Chord)
            chart.Options.Chord.ColorDimension.ValidateLegend(chart.Options.Chord.ColorScale,
                chart.Series[0].FlowLinks.Any(link => link.ColorValue.HasValue));
        chart.Options.MapColorDimension.ValidateLegend(chart.Options.MapColorScale);
        chart.Options.HeatmapColorDimension.ValidateLegend(chart.Options.HeatmapColorScale);
        if (chart.Series.Count > 0 && chart.Series[0].Kind is ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst) {
            var sunburst = chart.Series[0].Kind == ChartSeriesKind.Sunburst;
            var dimension = sunburst ? chart.Options.Sunburst.ColorDimension : chart.Options.Treemap.ColorDimension;
            var scale = sunburst ? chart.Options.Sunburst.ColorScale : chart.Options.Treemap.ColorScale;
            // Authored hierarchy color values resolve the existing theme scale during preparation.
            dimension.ValidateLegend(scale, chart.Series[0].HierarchyItems.Any(item => item.ColorValue.HasValue));
        }
        if (chart.Options.HeatmapColorScale != null && chart.Series.Any(series => series.IsCategoricalHeatmapRow
            || (series.Kind != ChartSeriesKind.Heatmap && series.Kind != ChartSeriesKind.HexbinHeatmap && series.Kind != ChartSeriesKind.CalendarHeatmap)))
            throw new InvalidOperationException("Heatmap color scales require numeric rectangular, hexagonal or calendar heatmaps.");
        if (chart.Options.RadialGeometry.CornerRadius > 0 && chart.Series.Count > 0 && !ChartSeriesKindTraits.UsesNumericRadialCoordinates(chart))
            throw new InvalidOperationException("Radial geometry corner radius requires numeric radial bars or columns.");
        if (chart.Options.Sunburst.CornerRadius > 0 && chart.Series.Any(series => series.Kind != ChartSeriesKind.Sunburst))
            throw new InvalidOperationException("Sunburst corner radius requires a Sunburst chart.");
        if ((chart.Options.Sunburst.SectorGap > 0 || chart.Options.Sunburst.MinimumLabelFontSize.HasValue)
            && chart.Series.Any(series => series.Kind != ChartSeriesKind.Sunburst))
            throw new InvalidOperationException("Sunburst sector spacing and caption fitting require a Sunburst chart.");
        if (chart.Options.Sunburst.SecondaryLabelFormatter != null && chart.Series.Any(series => series.Kind != ChartSeriesKind.Sunburst))
            throw new InvalidOperationException("Sunburst secondary labels require a Sunburst chart.");
        if ((chart.Options.XAxis.LabelOrientation != ChartAxisLabelOrientation.Fixed
            || chart.Options.YAxis.LabelOrientation != ChartAxisLabelOrientation.Fixed
            || chart.Options.SecondaryYAxis.LabelOrientation != ChartAxisLabelOrientation.Fixed
            || chart.Options.SecondaryXAxis.LabelOrientation != ChartAxisLabelOrientation.Fixed)
            && !ChartSeriesKindTraits.UsesNumericRadialCoordinates(chart))
            throw new NotSupportedException("Relative axis label orientation requires numeric radial bars or columns.");
        if (ChartSeriesKindTraits.IsMixedNumericRadial(chart)) {
            ValidateMixedNumericRadial(chart);
            return;
        }
        if (ChartMapComposition.ValidateMultiple(chart)) return;
        if (ValidateRadialBands(chart, preparing)) return;
        var exclusiveKinds = chart.Series.Select(series => series.Kind).Where(ChartSeriesKindTraits.IsExclusive).Distinct().ToArray();
        if (exclusiveKinds.Length == 0) return;
        if (exclusiveKinds.Length > 1 || chart.Series.Any(series => series.Kind != exclusiveKinds[0])) {
            throw new InvalidOperationException("Specialized chart types cannot be mixed with other series kinds in the same chart.");
        }

        if (ChartSeriesKindTraits.RequiresSingleSeries(exclusiveKinds[0]) && chart.Series.Count != 1) {
            throw new InvalidOperationException(exclusiveKinds[0].ToString() + " charts support exactly one series.");
        }

        if (!preparing && ChartSeriesKindTraits.RequiresPositiveValues(exclusiveKinds[0]) && !chart.Series[0].RenderPoints.Any(point => point.Y > 0)) {
            throw new InvalidOperationException(exclusiveKinds[0].ToString() + " charts require at least one positive value.");
        }

        if (!preparing && exclusiveKinds[0] == ChartSeriesKind.Waterfall && !chart.Series[0].HasSourceData) {
            throw new InvalidOperationException("Waterfall charts require at least one value.");
        }

        if (exclusiveKinds[0] == ChartSeriesKind.Radar) {
            var categoryCount = ValidateUniqueRadialCategories(chart);
            if (categoryCount < 3 && (!preparing || categoryCount > 0)) throw new InvalidOperationException("Radar charts require at least three categories.");
        }

        if (exclusiveKinds[0] == ChartSeriesKind.Polar) {
            if (chart.Series.Any(series => {
                var axis = series.ValueAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
                return axis.Minimum.HasValue && axis.Minimum.Value < 0;
            })) {
                throw new InvalidOperationException("Polar charts require a non-negative radial-axis minimum.");
            }

            if (!preparing && !chart.Series.SelectMany(series => series.RenderPoints).Any(point => point.Y > 0)) {
                throw new InvalidOperationException("Polar charts require at least one positive radius.");
            }
        }

        ValidateSpecializedShape(chart, exclusiveKinds[0], preparing);
    }

    private static void ValidateMixedNumericRadial(Chart chart) {
        var geometry = chart.Options.RadialGeometry;
        if (Math.Abs(geometry.EndAngleDegrees - geometry.StartAngleDegrees - 360) > 1e-9)
            throw new NotSupportedException("Mixed radial columns and radar series require a full-turn radial viewport.");
        var categoryCount = ValidateUniqueRadialCategories(chart);
        if (categoryCount < 3 && chart.Series.Any(series => series.Kind == ChartSeriesKind.Radar && series.RenderPoints.Count > 0))
            throw new InvalidOperationException("Radar charts require at least three categories.");
    }

    private static int ValidateUniqueRadialCategories(Chart chart) {
        foreach (var series in chart.Series) {
            var ordered = series.RenderPoints.Select(point => point.X).OrderBy(value => value).ToArray();
            for (var index = 0; index < ordered.Length; index++) {
                if (index > 0 && ChartMath.SameCoordinate(ordered[index - 1], ordered[index]))
                    throw new InvalidOperationException("Radar and mixed radial series require unique categories within each series.");
            }
        }
        return ChartBarCoordinateMap.Create(chart).Categories.Count;
    }

    private static void ValidateRenderableChart(Chart chart, bool preparing) {
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i] == null) throw new InvalidOperationException("Chart series collection must not contain null entries.");
            ValidateSeriesShape(chart.Series[i], preparing, chart.Options);
            chart.Series[i].ValidateDifferenceArea(chart.Options);
            if (preparing && chart.Series[i].Kind == ChartSeriesKind.Sunburst && chart.Series[i].Relationships != null)
                _ = chart.Series[i].Relationships!.ResolveHierarchyValues(chart.Options.Sunburst.ParentValuePolicy);
            if (chart.Series[i].IsHistogramDensity) {
                var valueAxis = chart.Series[i].ValueAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
                if ((chart.Options.XAxis.Scale != ChartScaleKind.Linear && chart.Options.XAxis.Scale != ChartScaleKind.Time) || valueAxis.Scale != ChartScaleKind.Linear)
                    throw new InvalidOperationException("Histogram density requires a linear measurement and value scale; a time measurement scale is also supported.");
                if (chart.Series[i].NormalizedTo.HasValue)
                    throw new InvalidOperationException("Histogram density cannot be normalized because rectangular area must retain the raw aggregate.");
            }
        }

        ValidateAxisLabels(chart.Options.XAxis, "X-axis");
        ValidateAxisLabels(chart.Options.YAxis, "Y-axis");
        ValidateAxisLabels(chart.Options.SecondaryYAxis, "Secondary y-axis");
        ValidateAxisLabels(chart.Options.SecondaryXAxis, "Secondary x-axis");
    }

    private static void ValidateAxisLabels(ChartAxis axis, string axisName) {
        foreach (var label in axis.Labels) {
            if (label.Text == null) throw new InvalidOperationException(axisName + " labels must not contain null text.");
        }
    }

    private static void ValidateSeriesShape(ChartSeries series, bool preparing, ChartOptions options) {
        series.ValidateWaterfall();
        series.ValidateInterpolation();
        series.ValidateMarkerAndRadarOptions();
        series.ValidateFinancialOptions();
        series.ValidateMapPointSize();
        series.ValidateColorValues(options);
        if (ChartSeries.IsRelationshipKind(series.Kind)) series.ValidateRelationships(preparing);
        if (series.RenderPoints.Any(point => point.BreakBefore) && series.Kind != ChartSeriesKind.Line && series.Kind != ChartSeriesKind.StepLine && series.Kind != ChartSeriesKind.Area && series.Kind != ChartSeriesKind.StepArea && series.Kind != ChartSeriesKind.Scatter
            && series.Kind != ChartSeriesKind.StackedArea && series.Kind != ChartSeriesKind.RangeBand && series.Kind != ChartSeriesKind.RangeArea && series.Kind != ChartSeriesKind.DifferenceArea)
            throw new InvalidOperationException("Segment breaks are supported only for line, step-line, area, step-area, stacked-area, range-band, range-area, difference-area, and scatter series.");
        if (series.HistogramBinLayout != null) ValidateHistogramSeries(series);
        // Typed observation projections are derived snapshots; empty data retains the native no-data scene.
        if (preparing && series.RenderPoints.Count == 0) return;
        if (series.Kind == ChartSeriesKind.Bubble) {
            ValidateTupleSeries(series, 2, "Bubble");
            for (var i = 0; i + 1 < series.RenderPoints.Count; i += 2) {
                RequireSameX(series.RenderPoints[i], series.RenderPoints[i + 1], "Bubble value and size points must share the same x value.");
                if (series.RenderPoints[i + 1].Y <= 0) throw new InvalidOperationException("Bubble sizes must be positive.");
            }
        } else if (series.Kind == ChartSeriesKind.ErrorBar) {
            ValidateTupleSeries(series, 3, "Error-bar");
            for (var i = 0; i + 2 < series.RenderPoints.Count; i += 3) {
                if (series.RenderPoints[i + 1].X > series.RenderPoints[i].X) throw new InvalidOperationException("Error-bar horizontal lower bounds must not exceed the estimate.");
                if (series.RenderPoints[i + 2].X < series.RenderPoints[i].X) throw new InvalidOperationException("Error-bar horizontal upper bounds must not precede the estimate.");
                if (series.RenderPoints[i + 1].Y > series.RenderPoints[i].Y) throw new InvalidOperationException("Error-bar lower bounds must be less than or equal to the point estimate.");
                if (series.RenderPoints[i + 2].Y < series.RenderPoints[i].Y) throw new InvalidOperationException("Error-bar upper bounds must be greater than or equal to the point estimate.");
            }
        } else if (series.Kind == ChartSeriesKind.Candlestick || series.Kind == ChartSeriesKind.Ohlc) {
            ValidateTupleSeries(series, 4, series.Kind == ChartSeriesKind.Candlestick ? "Candlestick" : "OHLC");
            for (var i = 0; i + 3 < series.RenderPoints.Count; i += 4) {
                var open = series.RenderPoints[i];
                var high = series.RenderPoints[i + 1];
                var low = series.RenderPoints[i + 2];
                var close = series.RenderPoints[i + 3];
                RequireSameX(open, high, "Financial OHLC points in one tuple must share the same x value.");
                RequireSameX(open, low, "Financial OHLC points in one tuple must share the same x value.");
                RequireSameX(open, close, "Financial OHLC points in one tuple must share the same x value.");
                if (high.Y < open.Y || high.Y < close.Y || high.Y < low.Y) throw new InvalidOperationException("Financial OHLC high values must be greater than or equal to open, low, and close values.");
                if (low.Y > open.Y || low.Y > close.Y || low.Y > high.Y) throw new InvalidOperationException("Financial OHLC low values must be less than or equal to open, high, and close values.");
            }
        } else if (series.Kind == ChartSeriesKind.RangeBand || series.Kind == ChartSeriesKind.RangeArea) {
            ValidateTupleSeries(series, 2, series.Kind == ChartSeriesKind.RangeBand ? "Range-band" : "Range-area");
            for (var i = 0; i + 1 < series.RenderPoints.Count; i += 2) {
                RequireSameX(series.RenderPoints[i], series.RenderPoints[i + 1], "Range lower and upper points must share the same x value.");
                if (series.RenderPoints[i].Y > series.RenderPoints[i + 1].Y) throw new InvalidOperationException("Range lower values must be less than or equal to upper values.");
            }
        } else if (series.Kind == ChartSeriesKind.RangeBar || series.Kind == ChartSeriesKind.Dumbbell) {
            ValidateTupleSeries(series, 2, series.Kind == ChartSeriesKind.RangeBar ? "Range-bar" : "Dumbbell");
            for (var i = 0; i + 1 < series.RenderPoints.Count; i += 2) {
                RequireSameX(series.RenderPoints[i], series.RenderPoints[i + 1], "Paired comparison points must share the same x value.");
            }
        } else if (series.Kind == ChartSeriesKind.BoxPlot) {
            ValidateTupleSeries(series, 5, "Box plot");
            for (var i = 0; i + 4 < series.RenderPoints.Count; i += 5) {
                var minimum = series.RenderPoints[i];
                var q1 = series.RenderPoints[i + 1];
                var median = series.RenderPoints[i + 2];
                var q3 = series.RenderPoints[i + 3];
                var maximum = series.RenderPoints[i + 4];
                RequireSameX(minimum, q1, "Box plot summary points must share the same x value.");
                RequireSameX(minimum, median, "Box plot summary points must share the same x value.");
                RequireSameX(minimum, q3, "Box plot summary points must share the same x value.");
                RequireSameX(minimum, maximum, "Box plot summary points must share the same x value.");
                if (minimum.Y > q1.Y || q1.Y > median.Y || median.Y > q3.Y || q3.Y > maximum.Y) throw new InvalidOperationException("Box plot values must be ordered as minimum <= q1 <= median <= q3 <= maximum.");
            }
        }
    }

    private static void ValidateHistogramSeries(ChartSeries series) {
        var layout = series.HistogramBinLayout!;
        if (series.Kind != ChartSeriesKind.Bar || series.RenderPoints.Count != layout.Count) {
            throw new InvalidOperationException("Histogram series must retain exactly one bar point per layout bin.");
        }

        for (var index = 0; index < series.RenderPoints.Count; index++) {
            if (series.RenderPoints[index].X != layout.GetCenter(index) || series.RenderPoints[index].Y != (series.HistogramBins[index].Value ?? 0)) {
                throw new InvalidOperationException("Histogram series points must retain their layout order, centers and raw aggregates; rebuild the histogram to change observations.");
            }
        }
    }

    private static void ValidateTupleSeries(ChartSeries series, int tupleSize, string chartName) {
        if (series.RenderPoints.Count == 0 || series.RenderPoints.Count % tupleSize != 0) {
            throw new InvalidOperationException(chartName + " series require complete " + tupleSize.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-point tuple(s).");
        }
    }

    private static void RequireSameX(ChartPoint first, ChartPoint second, string message) {
        if (Math.Abs(first.X - second.X) > 0.000001) throw new InvalidOperationException(message);
    }

    private static void ValidateSpecializedShape(Chart chart, ChartSeriesKind kind, bool preparing) {
        if (kind == ChartSeriesKind.Heatmap || kind == ChartSeriesKind.HexbinHeatmap) {
            // Native matrices retain empty peers and masked column spans without inventing cells.
            if (!preparing)
                ValidateMinimumPointCount(chart.Series.Where(series => !series.HeatmapColumnCount.HasValue).ToArray(), kind, 1);
            // A categorical matrix may list entities nothing is known about, so it can be empty; a numeric one cannot.
            if (!preparing && !chart.Series.Any(series => series.RenderPoints.Count > 0) && !chart.Series.All(series => series.IsCategoricalHeatmapRow)) throw new InvalidOperationException(kind.ToString() + " charts require at least one visible cell.");
            if (kind == ChartSeriesKind.Heatmap) ValidateHeatmapCategories(chart);
            if (chart.Options.HeatmapColorScale == null && chart.Options.HeatmapRelativeScale && chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) {
                throw new InvalidOperationException("Relative (count) heatmaps use a neutral sequential scale; the semantic status scale is reserved for status data.");
            }
        }
        else if (kind == ChartSeriesKind.CalendarHeatmap) {
            if (!preparing) ValidateMinimumPointCount(chart.Series, kind, 1);
            ValidateNonNegativeValues(chart.Series[0], kind);
        }
        else if (kind == ChartSeriesKind.DottedMap && !preparing) ValidateMinimumPointCount(chart.Series, kind, 1);
        else if (kind == ChartSeriesKind.TileMap || kind == ChartSeriesKind.RegionMap) {
            if (!preparing) ValidateMinimumPointCount(chart.Series, kind, 1);
        }
        else if (kind == ChartSeriesKind.Gauge || kind == ChartSeriesKind.Circle) {
            if (!preparing || chart.Series[0].HasSourceData) ValidateScalePair(chart.Series[0], kind.ToString());
        }
        else if (kind == ChartSeriesKind.ProgressRing) ValidateProgressRing(chart.Series[0], preparing);
        else if (kind == ChartSeriesKind.LayeredRadial) ValidateLayeredRadial(chart.Series[0], preparing);
        else if (kind == ChartSeriesKind.Polar) {
            if (!preparing) ValidateMinimumPointCount(chart.Series, kind, 1);
            foreach (var series in chart.Series) ValidateNonNegativeValues(series, kind);
        }
        else if (kind == ChartSeriesKind.Bullet) {
            if (!preparing || chart.Series.Any(series => series.HasSourceData)) ValidateBullets(chart.Series);
        }
        else if (kind == ChartSeriesKind.Timeline && !preparing) ValidateMinimumPointCount(chart.Series, kind, 1);
        else if (kind == ChartSeriesKind.StateTimeline) ValidateStateTimeline(chart, preparing);
        else if (kind == ChartSeriesKind.GanttLane) ValidateGanttLanes(chart, preparing);
        else if (kind == ChartSeriesKind.Gantt) {
            if (!preparing || chart.Series.Any(series => series.HasSourceData)) ValidateGantt(chart.Series);
        }
        else if (kind == ChartSeriesKind.Pyramid) ChartPyramidWeights.Total(chart.Series[0].RenderPoints);
        else if (kind == ChartSeriesKind.Funnel || kind == ChartSeriesKind.Pie || kind == ChartSeriesKind.Donut || kind == ChartSeriesKind.PolarArea || kind == ChartSeriesKind.Pictorial || kind == ChartSeriesKind.ProgressBar || kind == ChartSeriesKind.WordCloud) ValidateNonNegativeValues(chart.Series[0], kind);
    }

    private static void ValidateNonNegativeValues(ChartSeries series, ChartSeriesKind kind) {
        foreach (var point in series.RenderPoints) {
            if (point.Y < 0) throw new InvalidOperationException(kind.ToString() + " charts require non-negative values.");
        }
    }

    private static void ValidateScalePair(ChartSeries series, string chartName) {
        if (series.RenderPoints.Count < 2) throw new InvalidOperationException(chartName + " charts require scale minimum and maximum points.");
        if (series.RenderPoints[1].X <= series.RenderPoints[0].X) throw new InvalidOperationException(chartName + " chart maximum must be greater than minimum.");
    }

    private static void ValidateProgressRing(ChartSeries series, bool preparing) {
        if (!preparing && series.RenderPoints.Count == 0) throw new InvalidOperationException("ProgressRing charts require at least one value.");
        foreach (var point in series.RenderPoints) {
            if (point.Y < 0 || point.Y > 100) throw new InvalidOperationException("ProgressRing chart values must be between zero and 100.");
        }
    }

    private static void ValidateLayeredRadial(ChartSeries series, bool preparing) {
        if (!preparing && series.RadialLayers.Count == 0) throw new InvalidOperationException("LayeredRadial charts require at least one layer.");
        foreach (var layer in series.RadialLayers) {
            if (layer == null) throw new InvalidOperationException("LayeredRadial layers must not contain null entries.");
            if (layer.Maximum <= layer.Minimum) throw new InvalidOperationException("LayeredRadial layer maximum must be greater than minimum.");
        }
    }

    private static void ValidateBullets(IReadOnlyList<ChartSeries> series) {
        foreach (var item in series) {
            if (!item.HasSourceData) continue;
            if (item.BulletValues.Count != 1) throw new InvalidOperationException("Bullet charts require one typed value and target observation per series.");
        }
    }

    private static void ValidateStateCategories(Chart chart) {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var state in chart.Options.StateCategories) {
            if (state == null) throw new InvalidOperationException("State categories must not contain null entries.");
            if (!keys.Add(state.Key)) throw new InvalidOperationException("State category keys must be unique: " + state.Key);
        }
    }

    private static void ValidateHeatmapCategories(Chart chart) {
        var categorical = 0;
        foreach (var row in chart.Series) {
            if (!row.IsCategoricalHeatmapRow) continue;
            categorical++;
        }

        if (categorical == 0) return;
        if (categorical != chart.Series.Count) throw new InvalidOperationException("A heatmap must use either numeric rows or categorical rows, not both.");
        ValidateStateCategories(chart);
    }

    private static void ValidateGanttLanes(Chart chart, bool preparing) {
        if (chart.Options.XAxis.Scale != ChartScaleKind.Linear && chart.Options.XAxis.Scale != ChartScaleKind.Time)
            throw new InvalidOperationException("GanttLane charts require a linear or time x-axis to preserve elapsed-time geometry.");
        ValidateStateCategories(chart);
        var items = 0;
        foreach (var lane in chart.Series) {
            items += lane.GanttLaneItems.Count;
        }

        if (!preparing && items == 0) throw new InvalidOperationException("GanttLane charts require at least one item.");
    }

    private static void ValidateStateTimeline(Chart chart, bool preparing) {
        if (chart.Options.XAxis.Scale != ChartScaleKind.Linear && chart.Options.XAxis.Scale != ChartScaleKind.Time)
            throw new InvalidOperationException("StateTimeline charts require a linear or time x-axis to preserve elapsed-time geometry.");
        ValidateStateCategories(chart);

        var series = chart.Series;
        var segments = 0;
        foreach (var lane in series) {
            segments += lane.StateTimelineSegments.Count;
        }

        if (!preparing && segments == 0) throw new InvalidOperationException("StateTimeline charts require at least one segment.");
    }

    private static void ValidateMinimumPointCount(IReadOnlyList<ChartSeries> series, ChartSeriesKind kind, int count) {
        foreach (var item in series) {
            if (item.Points.Count < count) throw new InvalidOperationException(kind.ToString() + " charts require at least " + count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " point(s) per series.");
        }
    }

    private static void ValidateGantt(IReadOnlyList<ChartSeries> series) {
        for (var i = 0; i < series.Count; i++) {
            var item = series[i];
            if (item.GanttItems.Count > 1) throw new InvalidOperationException("Gantt charts require at most one typed schedule item per series.");
        }
    }

}
