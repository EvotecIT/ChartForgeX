using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

internal static class ChartGuards {
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
        ValidateRenderableChart(chart, preparing);
        var exclusiveKinds = chart.Series.Select(series => series.Kind).Where(ChartSeriesKindTraits.IsExclusive).Distinct().ToArray();
        if (exclusiveKinds.Length == 0) return;
        if (exclusiveKinds.Length > 1 || chart.Series.Any(series => series.Kind != exclusiveKinds[0])) {
            throw new InvalidOperationException("Specialized chart types cannot be mixed with other series kinds in the same chart.");
        }

        if (ChartSeriesKindTraits.RequiresSingleSeries(exclusiveKinds[0]) && chart.Series.Count != 1) {
            throw new InvalidOperationException(exclusiveKinds[0].ToString() + " charts support exactly one series.");
        }

        if (!preparing && ChartSeriesKindTraits.RequiresPositiveValues(exclusiveKinds[0]) && !chart.Series[0].Points.Any(point => point.Y > 0)) {
            throw new InvalidOperationException(exclusiveKinds[0].ToString() + " charts require at least one positive value.");
        }

        if (!preparing && exclusiveKinds[0] == ChartSeriesKind.Waterfall && chart.Series[0].Points.Count == 0) {
            throw new InvalidOperationException("Waterfall charts require at least one value.");
        }

        if (exclusiveKinds[0] == ChartSeriesKind.Radar) {
            var categoryCount = chart.Series
                .Where(series => series.Kind == ChartSeriesKind.Radar)
                .SelectMany(series => series.Points.Select(point => point.X))
                .Distinct()
                .Count();
            if (categoryCount < 3 && (!preparing || categoryCount > 0)) throw new InvalidOperationException("Radar charts require at least three categories.");
        }

        if (exclusiveKinds[0] == ChartSeriesKind.Polar) {
            if (chart.Options.YAxis.Minimum.HasValue && chart.Options.YAxis.Minimum.Value < 0) {
                throw new InvalidOperationException("Polar charts require a non-negative radial-axis minimum.");
            }

            if (!preparing && !chart.Series.SelectMany(series => series.Points).Any(point => point.Y > 0)) {
                throw new InvalidOperationException("Polar charts require at least one positive radius.");
            }
        }

        ValidateSpecializedShape(chart, exclusiveKinds[0], preparing);
    }

    private static void ValidateRenderableChart(Chart chart, bool preparing) {
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i] == null) throw new InvalidOperationException("Chart series collection must not contain null entries.");
            ValidateSeriesShape(chart.Series[i], preparing);
            if (chart.Series[i].IsHistogramDensity) {
                var valueAxis = chart.Series[i].YAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
                if ((chart.Options.XAxis.Scale != ChartScaleKind.Linear && chart.Options.XAxis.Scale != ChartScaleKind.Time) || valueAxis.Scale != ChartScaleKind.Linear)
                    throw new InvalidOperationException("Histogram density requires a linear measurement and value scale; a time measurement scale is also supported.");
                if (chart.Series[i].NormalizedTo.HasValue)
                    throw new InvalidOperationException("Histogram density cannot be normalized because rectangular area must retain the raw aggregate.");
            }
        }

        for (var i = 0; i < chart.Annotations.Count; i++) {
            if (chart.Annotations[i] == null) throw new InvalidOperationException("Chart annotations collection must not contain null entries.");
        }

        ValidateAxisLabels(chart.Options.XAxis, "X-axis");
        ValidateAxisLabels(chart.Options.YAxis, "Y-axis");
        ValidateAxisLabels(chart.Options.SecondaryYAxis, "Secondary y-axis");
    }

    private static void ValidateAxisLabels(ChartAxis axis, string axisName) {
        foreach (var label in axis.Labels) {
            if (label.Text == null) throw new InvalidOperationException(axisName + " labels must not contain null text.");
        }
    }

    private static void ValidateSeriesShape(ChartSeries series, bool preparing) {
        series.ValidateInterpolation();
        series.ValidateMarkerAndRadarOptions();
        if (ChartSeries.IsRelationshipKind(series.Kind)) series.ValidateRelationships(preparing);
        if (series.Points.Any(point => point.BreakBefore) && series.Kind != ChartSeriesKind.Line && series.Kind != ChartSeriesKind.StepLine && series.Kind != ChartSeriesKind.Area && series.Kind != ChartSeriesKind.StepArea && series.Kind != ChartSeriesKind.Scatter
            && series.Kind != ChartSeriesKind.StackedArea && series.Kind != ChartSeriesKind.RangeBand && series.Kind != ChartSeriesKind.RangeArea)
            throw new InvalidOperationException("Segment breaks are supported only for line, step-line, area, step-area, stacked-area, range-band, range-area, and scatter series.");
        if (series.HistogramBinLayout != null) ValidateHistogramSeries(series);
        // An empty tuple series is a native no-data scene; incomplete nonempty tuples
        // still pass through the same canonical shape validation below.
        if (preparing && series.Points.Count == 0) return;
        if (series.Kind == ChartSeriesKind.Bubble) {
            ValidateTupleSeries(series, 2, "Bubble");
            for (var i = 0; i + 1 < series.Points.Count; i += 2) {
                RequireSameX(series.Points[i], series.Points[i + 1], "Bubble value and size points must share the same x value.");
                if (series.Points[i + 1].Y <= 0) throw new InvalidOperationException("Bubble sizes must be positive.");
            }
        } else if (series.Kind == ChartSeriesKind.ErrorBar) {
            ValidateTupleSeries(series, 3, "Error-bar");
            for (var i = 0; i + 2 < series.Points.Count; i += 3) {
                RequireSameX(series.Points[i], series.Points[i + 1], "Error-bar point and lower bound must share the same x value.");
                RequireSameX(series.Points[i], series.Points[i + 2], "Error-bar point and upper bound must share the same x value.");
                if (series.Points[i + 1].Y > series.Points[i].Y) throw new InvalidOperationException("Error-bar lower bounds must be less than or equal to the point estimate.");
                if (series.Points[i + 2].Y < series.Points[i].Y) throw new InvalidOperationException("Error-bar upper bounds must be greater than or equal to the point estimate.");
            }
        } else if (series.Kind == ChartSeriesKind.Candlestick || series.Kind == ChartSeriesKind.Ohlc) {
            ValidateTupleSeries(series, 4, series.Kind == ChartSeriesKind.Candlestick ? "Candlestick" : "OHLC");
            for (var i = 0; i + 3 < series.Points.Count; i += 4) {
                var open = series.Points[i];
                var high = series.Points[i + 1];
                var low = series.Points[i + 2];
                var close = series.Points[i + 3];
                RequireSameX(open, high, "Financial OHLC points in one tuple must share the same x value.");
                RequireSameX(open, low, "Financial OHLC points in one tuple must share the same x value.");
                RequireSameX(open, close, "Financial OHLC points in one tuple must share the same x value.");
                if (high.Y < open.Y || high.Y < close.Y || high.Y < low.Y) throw new InvalidOperationException("Financial OHLC high values must be greater than or equal to open, low, and close values.");
                if (low.Y > open.Y || low.Y > close.Y || low.Y > high.Y) throw new InvalidOperationException("Financial OHLC low values must be less than or equal to open, high, and close values.");
            }
        } else if (series.Kind == ChartSeriesKind.RangeBand || series.Kind == ChartSeriesKind.RangeArea) {
            ValidateTupleSeries(series, 2, series.Kind == ChartSeriesKind.RangeBand ? "Range-band" : "Range-area");
            for (var i = 0; i + 1 < series.Points.Count; i += 2) {
                RequireSameX(series.Points[i], series.Points[i + 1], "Range lower and upper points must share the same x value.");
                if (series.Points[i].Y > series.Points[i + 1].Y) throw new InvalidOperationException("Range lower values must be less than or equal to upper values.");
            }
        } else if (series.Kind == ChartSeriesKind.RangeBar || series.Kind == ChartSeriesKind.Dumbbell) {
            ValidateTupleSeries(series, 2, series.Kind == ChartSeriesKind.RangeBar ? "Range-bar" : "Dumbbell");
            for (var i = 0; i + 1 < series.Points.Count; i += 2) {
                RequireSameX(series.Points[i], series.Points[i + 1], "Paired comparison points must share the same x value.");
            }
        } else if (series.Kind == ChartSeriesKind.BoxPlot) {
            ValidateTupleSeries(series, 5, "Box plot");
            for (var i = 0; i + 4 < series.Points.Count; i += 5) {
                var minimum = series.Points[i];
                var q1 = series.Points[i + 1];
                var median = series.Points[i + 2];
                var q3 = series.Points[i + 3];
                var maximum = series.Points[i + 4];
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
        if (series.Kind != ChartSeriesKind.Bar || series.Points.Count != layout.Count) {
            throw new InvalidOperationException("Histogram series must retain exactly one bar point per layout bin.");
        }

        for (var index = 0; index < series.Points.Count; index++) {
            if (series.Points[index].X != layout.GetCenter(index) || series.Points[index].Y != (series.HistogramBins[index].Value ?? 0)) {
                throw new InvalidOperationException("Histogram series points must retain their layout order, centers and raw aggregates; rebuild the histogram to change observations.");
            }
        }
    }

    private static void ValidateTupleSeries(ChartSeries series, int tupleSize, string chartName) {
        if (series.Points.Count == 0 || series.Points.Count % tupleSize != 0) {
            throw new InvalidOperationException(chartName + " series require complete " + tupleSize.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-point tuple(s).");
        }
    }

    private static void RequireSameX(ChartPoint first, ChartPoint second, string message) {
        if (Math.Abs(first.X - second.X) > 0.000001) throw new InvalidOperationException(message);
    }

    private static void ValidateSpecializedShape(Chart chart, ChartSeriesKind kind, bool preparing) {
        if (kind == ChartSeriesKind.Heatmap || kind == ChartSeriesKind.HexbinHeatmap) {
            // Fully masked rows keep their position (for example a weekday with no samples) when the column span is known.
            if (!preparing || chart.Series.Any(series => series.Points.Count > 0))
                ValidateMinimumPointCount(chart.Series.Where(series => !series.HeatmapColumnCount.HasValue).ToArray(), kind, 1);
            // A categorical matrix may list entities nothing is known about, so it can be empty; a numeric one cannot.
            if (!preparing && !chart.Series.Any(series => series.Points.Count > 0) && !chart.Series.All(series => series.IsCategoricalHeatmapRow)) throw new InvalidOperationException(kind.ToString() + " charts require at least one visible cell.");
            if (kind == ChartSeriesKind.Heatmap) ValidateHeatmapCategories(chart);
            if (chart.Options.HeatmapRelativeScale && chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) {
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
            ValidateNonNegativeValues(chart.Series[0], kind);
        }
        else if (kind == ChartSeriesKind.Gauge || kind == ChartSeriesKind.Circle) ValidateScalePair(chart.Series[0], kind.ToString());
        else if (kind == ChartSeriesKind.ProgressRing) ValidateProgressRing(chart.Series[0], preparing);
        else if (kind == ChartSeriesKind.LayeredRadial) ValidateLayeredRadial(chart.Series[0], preparing);
        else if (kind == ChartSeriesKind.Polar) {
            if (!preparing) ValidateMinimumPointCount(chart.Series, kind, 1);
            foreach (var series in chart.Series) ValidateNonNegativeValues(series, kind);
        }
        else if (kind == ChartSeriesKind.Bullet) ValidateBullets(chart.Series);
        else if (kind == ChartSeriesKind.Timeline) ValidateMinimumPointCount(chart.Series, kind, 1);
        else if (kind == ChartSeriesKind.StateTimeline) ValidateStateTimeline(chart);
        else if (kind == ChartSeriesKind.GanttLane) ValidateGanttLanes(chart);
        else if (kind == ChartSeriesKind.Gantt) ValidateGantt(chart.Series);
        else if (kind == ChartSeriesKind.Pyramid) ChartPyramidWeights.Total(chart.Series[0].Points);
        else if (kind == ChartSeriesKind.Funnel || kind == ChartSeriesKind.Pie || kind == ChartSeriesKind.Donut || kind == ChartSeriesKind.PolarArea || kind == ChartSeriesKind.Pictorial || kind == ChartSeriesKind.ProgressBar || kind == ChartSeriesKind.WordCloud) ValidateNonNegativeValues(chart.Series[0], kind);
    }

    private static void ValidateNonNegativeValues(ChartSeries series, ChartSeriesKind kind) {
        foreach (var point in series.Points) {
            if (point.Y < 0) throw new InvalidOperationException(kind.ToString() + " charts require non-negative values.");
        }
    }

    private static void ValidateScalePair(ChartSeries series, string chartName) {
        if (series.Points.Count < 2) throw new InvalidOperationException(chartName + " charts require scale minimum and maximum points.");
        if (series.Points[1].X <= series.Points[0].X) throw new InvalidOperationException(chartName + " chart maximum must be greater than minimum.");
    }

    private static void ValidateProgressRing(ChartSeries series, bool preparing) {
        if (!preparing && series.Points.Count == 0) throw new InvalidOperationException("ProgressRing charts require at least one value.");
        foreach (var point in series.Points) {
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
            if (item.Points.Count < 2) throw new InvalidOperationException("Bullet charts require value and target points.");
            if (item.Points[1].X <= item.Points[0].X) throw new InvalidOperationException("Bullet chart maximum must be greater than minimum.");
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
            if (row.HeatmapCells.Count != row.Points.Count) throw new InvalidOperationException("Categorical heatmap rows require one cell per point.");
            categorical++;
        }

        if (categorical == 0) return;
        if (categorical != chart.Series.Count) throw new InvalidOperationException("A heatmap must use either numeric rows or categorical rows, not both.");
        ValidateStateCategories(chart);
    }

    private static void ValidateGanttLanes(Chart chart) {
        if (chart.Options.XAxis.Scale != ChartScaleKind.Linear && chart.Options.XAxis.Scale != ChartScaleKind.Time)
            throw new InvalidOperationException("GanttLane charts require a linear or time x-axis to preserve elapsed-time geometry.");
        ValidateStateCategories(chart);
        var items = 0;
        foreach (var lane in chart.Series) {
            if (lane.GanttLaneItems.Count != lane.Points.Count) throw new InvalidOperationException("Gantt lanes require one item per point.");
            for (var i = 0; i < lane.Points.Count; i++) {
                var item = lane.GanttLaneItems[i];
                var point = lane.Points[i];
                if (point.X != item.Start || point.Y != (item.End ?? item.Start))
                    throw new InvalidOperationException("Gantt lane points must match their items; rebuild the lane after changing its data.");
            }
            items += lane.Points.Count;
        }

        if (items == 0) throw new InvalidOperationException("GanttLane charts require at least one item.");
    }

    private static void ValidateStateTimeline(Chart chart) {
        if (chart.Options.XAxis.Scale != ChartScaleKind.Linear && chart.Options.XAxis.Scale != ChartScaleKind.Time)
            throw new InvalidOperationException("StateTimeline charts require a linear or time x-axis to preserve elapsed-time geometry.");
        ValidateStateCategories(chart);

        var series = chart.Series;
        var segments = 0;
        foreach (var lane in series) {
            if (lane.PointLabels.Count != lane.Points.Count || lane.PointLabels.Any(string.IsNullOrWhiteSpace)) throw new InvalidOperationException("State timeline segments require a state key for every segment.");
            if (lane.Points.Any(point => point.Y <= point.X)) throw new InvalidOperationException("State timeline segment ends must be later than their starts.");
            segments += lane.Points.Count;
        }

        if (segments == 0) throw new InvalidOperationException("StateTimeline charts require at least one segment.");
    }

    private static void ValidateMinimumPointCount(IReadOnlyList<ChartSeries> series, ChartSeriesKind kind, int count) {
        foreach (var item in series) {
            if (item.Points.Count < count) throw new InvalidOperationException(kind.ToString() + " charts require at least " + count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " point(s) per series.");
        }
    }

    private static void ValidateGantt(IReadOnlyList<ChartSeries> series) {
        for (var i = 0; i < series.Count; i++) {
            var item = series[i];
            if (item.Points.Count < 3) throw new InvalidOperationException("Gantt charts require range, metadata, and type points per series.");
            if (item.Points[0].Y < item.Points[0].X) throw new InvalidOperationException("Gantt task end must be greater than or equal to start.");
            if (item.Points[1].X < 0 || item.Points[1].X > 1) throw new InvalidOperationException("Gantt progress values must be between zero and one.");
            var dependency = WholeNumberIndex(item.Points[1].Y, "Gantt dependencies");
            if (dependency < -1 || dependency >= i) throw new InvalidOperationException("Gantt dependencies must reference earlier task indexes.");
        }
    }

    private static int WholeNumberIndex(double value, string message) {
        var index = (int)Math.Round(value);
        if (Math.Abs(value - index) > 0.000001) throw new InvalidOperationException(message + " must be whole-number indexes.");
        return index;
    }

}
