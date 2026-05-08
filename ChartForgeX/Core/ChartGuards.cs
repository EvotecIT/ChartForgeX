using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

internal static class ChartGuards {
    private static readonly ChartSeriesKind[] ExclusiveSeriesKinds = {
        ChartSeriesKind.Heatmap,
        ChartSeriesKind.HexbinHeatmap,
        ChartSeriesKind.CalendarHeatmap,
        ChartSeriesKind.DottedMap,
        ChartSeriesKind.TileMap,
        ChartSeriesKind.RegionMap,
        ChartSeriesKind.Gauge,
        ChartSeriesKind.Circle,
        ChartSeriesKind.RadialBar,
        ChartSeriesKind.LayeredRadial,
        ChartSeriesKind.Bullet,
        ChartSeriesKind.Waterfall,
        ChartSeriesKind.Radar,
        ChartSeriesKind.Funnel,
        ChartSeriesKind.Treemap,
        ChartSeriesKind.Timeline,
        ChartSeriesKind.Gantt,
        ChartSeriesKind.Sankey,
        ChartSeriesKind.Tree,
        ChartSeriesKind.Sunburst,
        ChartSeriesKind.Pictorial,
        ChartSeriesKind.ProgressBar,
        ChartSeriesKind.WordCloud,
        ChartSeriesKind.Pie,
        ChartSeriesKind.Donut,
        ChartSeriesKind.PolarArea
    };

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

    public static void RenderCompatibility(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        ValidateRenderableChart(chart);
        var exclusiveKinds = chart.Series.Select(series => series.Kind).Where(kind => Array.IndexOf(ExclusiveSeriesKinds, kind) >= 0).Distinct().ToArray();
        if (exclusiveKinds.Length == 0) return;
        if (exclusiveKinds.Length > 1 || chart.Series.Any(series => series.Kind != exclusiveKinds[0])) {
            throw new InvalidOperationException("Specialized chart types cannot be mixed with other series kinds in the same chart.");
        }

        if (RequiresSingleSeries(exclusiveKinds[0]) && chart.Series.Count != 1) {
            throw new InvalidOperationException(exclusiveKinds[0].ToString() + " charts support exactly one series.");
        }

        if (RequiresPositiveValues(exclusiveKinds[0]) && !chart.Series[0].Points.Any(point => point.Y > 0)) {
            throw new InvalidOperationException(exclusiveKinds[0].ToString() + " charts require at least one positive value.");
        }

        if (exclusiveKinds[0] == ChartSeriesKind.Waterfall && chart.Series[0].Points.Count == 0) {
            throw new InvalidOperationException("Waterfall charts require at least one value.");
        }

        if (exclusiveKinds[0] == ChartSeriesKind.Radar) {
            var categoryCount = chart.Series
                .Where(series => series.Kind == ChartSeriesKind.Radar)
                .SelectMany(series => series.Points.Select(point => point.X))
                .Distinct()
                .Count();
            if (categoryCount < 3) throw new InvalidOperationException("Radar charts require at least three categories.");
        }

        ValidateSpecializedShape(chart, exclusiveKinds[0]);
    }

    private static void ValidateRenderableChart(Chart chart) {
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i] == null) throw new InvalidOperationException("Chart series collection must not contain null entries.");
            ValidateSeriesShape(chart.Series[i]);
        }

        for (var i = 0; i < chart.Annotations.Count; i++) {
            if (chart.Annotations[i] == null) throw new InvalidOperationException("Chart annotations collection must not contain null entries.");
        }

        foreach (var label in chart.Options.XAxisLabels) {
            if (label.Text == null) throw new InvalidOperationException("X-axis labels must not contain null text.");
        }
    }

    private static void ValidateSeriesShape(ChartSeries series) {
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

    private static void ValidateTupleSeries(ChartSeries series, int tupleSize, string chartName) {
        if (series.Points.Count == 0 || series.Points.Count % tupleSize != 0) {
            throw new InvalidOperationException(chartName + " series require complete " + tupleSize.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-point tuple(s).");
        }
    }

    private static void RequireSameX(ChartPoint first, ChartPoint second, string message) {
        if (Math.Abs(first.X - second.X) > 0.000001) throw new InvalidOperationException(message);
    }

    private static bool RequiresSingleSeries(ChartSeriesKind kind) {
        return kind == ChartSeriesKind.Gauge ||
            kind == ChartSeriesKind.CalendarHeatmap ||
            kind == ChartSeriesKind.DottedMap ||
            kind == ChartSeriesKind.TileMap ||
            kind == ChartSeriesKind.RegionMap ||
            kind == ChartSeriesKind.Circle ||
            kind == ChartSeriesKind.RadialBar ||
            kind == ChartSeriesKind.LayeredRadial ||
            kind == ChartSeriesKind.Waterfall ||
            kind == ChartSeriesKind.Funnel ||
            kind == ChartSeriesKind.Treemap ||
            kind == ChartSeriesKind.Sankey ||
            kind == ChartSeriesKind.Tree ||
            kind == ChartSeriesKind.Sunburst ||
            kind == ChartSeriesKind.Pictorial ||
            kind == ChartSeriesKind.ProgressBar ||
            kind == ChartSeriesKind.WordCloud ||
            kind == ChartSeriesKind.Pie ||
            kind == ChartSeriesKind.Donut ||
            kind == ChartSeriesKind.PolarArea;
    }

    private static bool RequiresPositiveValues(ChartSeriesKind kind) {
        return kind == ChartSeriesKind.Funnel ||
            kind == ChartSeriesKind.Treemap ||
            kind == ChartSeriesKind.Pie ||
            kind == ChartSeriesKind.Donut ||
            kind == ChartSeriesKind.Pictorial ||
            kind == ChartSeriesKind.ProgressBar ||
            kind == ChartSeriesKind.WordCloud ||
            kind == ChartSeriesKind.PolarArea;
    }

    private static void ValidateSpecializedShape(Chart chart, ChartSeriesKind kind) {
        if (kind == ChartSeriesKind.Heatmap || kind == ChartSeriesKind.HexbinHeatmap) ValidateMinimumPointCount(chart.Series, kind, 1);
        else if (kind == ChartSeriesKind.CalendarHeatmap) {
            ValidateMinimumPointCount(chart.Series, kind, 1);
            ValidateNonNegativeValues(chart.Series[0], kind);
        }
        else if (kind == ChartSeriesKind.DottedMap) ValidateMinimumPointCount(chart.Series, kind, 1);
        else if (kind == ChartSeriesKind.TileMap || kind == ChartSeriesKind.RegionMap) {
            ValidateMinimumPointCount(chart.Series, kind, 1);
            ValidateNonNegativeValues(chart.Series[0], kind);
        }
        else if (kind == ChartSeriesKind.Gauge || kind == ChartSeriesKind.Circle) ValidateScalePair(chart.Series[0], kind.ToString());
        else if (kind == ChartSeriesKind.RadialBar) ValidateRadialBar(chart.Series[0]);
        else if (kind == ChartSeriesKind.LayeredRadial) ValidateLayeredRadial(chart.Series[0]);
        else if (kind == ChartSeriesKind.Bullet) ValidateBullets(chart.Series);
        else if (kind == ChartSeriesKind.Timeline) ValidateMinimumPointCount(chart.Series, kind, 1);
        else if (kind == ChartSeriesKind.Gantt) ValidateGantt(chart.Series);
        else if (kind == ChartSeriesKind.Sankey) ValidateSankey(chart.Series[0]);
        else if (kind == ChartSeriesKind.Tree) ValidateTree(chart.Series[0]);
        else if (kind == ChartSeriesKind.Sunburst) ValidateTree(chart.Series[0]);
        else if (kind == ChartSeriesKind.Funnel || kind == ChartSeriesKind.Treemap || kind == ChartSeriesKind.Pie || kind == ChartSeriesKind.Donut || kind == ChartSeriesKind.PolarArea || kind == ChartSeriesKind.Pictorial || kind == ChartSeriesKind.ProgressBar || kind == ChartSeriesKind.WordCloud) ValidateNonNegativeValues(chart.Series[0], kind);
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

    private static void ValidateRadialBar(ChartSeries series) {
        if (series.Points.Count == 0) throw new InvalidOperationException("RadialBar charts require at least one value.");
        foreach (var point in series.Points) {
            if (point.Y < 0 || point.Y > 100) throw new InvalidOperationException("RadialBar chart values must be between zero and 100.");
        }
    }

    private static void ValidateLayeredRadial(ChartSeries series) {
        if (series.RadialLayers.Count == 0) throw new InvalidOperationException("LayeredRadial charts require at least one layer.");
        foreach (var layer in series.RadialLayers) {
            if (layer.Maximum <= layer.Minimum) throw new InvalidOperationException("LayeredRadial layer maximum must be greater than minimum.");
        }
    }

    private static void ValidateBullets(IReadOnlyList<ChartSeries> series) {
        foreach (var item in series) {
            if (item.Points.Count < 2) throw new InvalidOperationException("Bullet charts require value and target points.");
            if (item.Points[1].X <= item.Points[0].X) throw new InvalidOperationException("Bullet chart maximum must be greater than minimum.");
        }
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

    private static void ValidateSankey(ChartSeries series) {
        if (series.Points.Count < 2 || series.Points.Count % 2 != 0) throw new InvalidOperationException("Sankey charts require complete source-target/value point pairs.");
        var nodeCount = 0;
        var outgoing = new Dictionary<int, List<int>>();
        for (var i = 0; i + 1 < series.Points.Count; i += 2) {
            var source = RoundedNonNegativeIndex(series.Points[i].X, "Sankey source indexes");
            var target = RoundedNonNegativeIndex(series.Points[i].Y, "Sankey target indexes");
            if (source == target) throw new InvalidOperationException("Sankey links must connect distinct source and target nodes.");
            if (series.Points[i + 1].Y <= 0) throw new InvalidOperationException("Sankey link values must be positive.");
            nodeCount = Math.Max(nodeCount, Math.Max(source, target) + 1);
            AddEdge(outgoing, source, target);
        }

        ValidateAcyclic(outgoing, nodeCount, "Sankey links must not contain cycles.");
    }

    private static void ValidateTree(ChartSeries series) {
        if (series.Points.Count < 2 || series.Points.Count % 2 != 0) throw new InvalidOperationException("Tree charts require complete parent-child/value point pairs.");
        var nodeCount = 0;
        var outgoing = new Dictionary<int, List<int>>();
        var parents = new Dictionary<int, int>();
        for (var i = 0; i + 1 < series.Points.Count; i += 2) {
            var parent = RoundedNonNegativeIndex(series.Points[i].X, "Tree parent indexes");
            var child = RoundedNonNegativeIndex(series.Points[i].Y, "Tree child indexes");
            if (parent == child) throw new InvalidOperationException("Tree links must connect distinct parent and child nodes.");
            if (series.Points[i + 1].Y <= 0) throw new InvalidOperationException("Tree link values must be positive.");
            if (parents.ContainsKey(child)) throw new InvalidOperationException("Tree child nodes can only have one parent.");
            parents[child] = parent;
            nodeCount = Math.Max(nodeCount, Math.Max(parent, child) + 1);
            AddEdge(outgoing, parent, child);
        }

        var roots = Enumerable.Range(0, nodeCount).Where(index => !parents.ContainsKey(index)).ToArray();
        if (roots.Length != 1) throw new InvalidOperationException("Tree charts require exactly one root node.");
        ValidateAcyclic(outgoing, nodeCount, "Tree links must not contain cycles.");
        var visited = new bool[nodeCount];
        VisitConnected(roots[0]);
        if (visited.Any(value => !value)) throw new InvalidOperationException("Tree links must form one connected hierarchy.");

        void VisitConnected(int node) {
            if (visited[node]) return;
            visited[node] = true;
            if (outgoing.TryGetValue(node, out var children)) foreach (var child in children) VisitConnected(child);
        }
    }

    private static int RoundedNonNegativeIndex(double value, string message) {
        var index = WholeNumberIndex(value, message);
        if (index < 0) throw new InvalidOperationException(message + " must be non-negative.");
        return index;
    }

    private static int WholeNumberIndex(double value, string message) {
        var index = (int)Math.Round(value);
        if (Math.Abs(value - index) > 0.000001) throw new InvalidOperationException(message + " must be whole-number indexes.");
        return index;
    }

    private static void AddEdge(Dictionary<int, List<int>> outgoing, int source, int target) {
        if (!outgoing.TryGetValue(source, out var targets)) {
            targets = new List<int>();
            outgoing[source] = targets;
        }

        targets.Add(target);
    }

    private static void ValidateAcyclic(Dictionary<int, List<int>> outgoing, int nodeCount, string message) {
        var state = new int[nodeCount];
        for (var i = 0; i < state.Length; i++) Visit(i);

        void Visit(int node) {
            if (state[node] == 1) throw new InvalidOperationException(message);
            if (state[node] == 2) return;
            state[node] = 1;
            if (outgoing.TryGetValue(node, out var targets)) foreach (var target in targets) Visit(target);
            state[node] = 2;
        }
    }
}
