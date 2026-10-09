using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static string HistogramPointDescription(ChartSeries series, int pointIndex, string displayedValue) {
        var bin = series.HistogramBins[pointIndex];
        var interval = "[" + Number(bin.LowerBound) + ", " + Number(bin.UpperBound) + (pointIndex == bin.Layout.Count - 1 ? "]" : ")");
        var label = series.Name + ": " + interval + ", " + series.HistogramAggregation + "=" + displayedValue + ", observations=" + Number(bin.Count);
        return series.IsHistogramDensity ? label + ", density=" + Number(bin.RenderedValue) : label;
    }

    private static void AddHistogramMetadata(IDictionary<string, string> metadata, ChartSeries series, int pointIndex) {
        var bin = series.HistogramBins[pointIndex];
        metadata.Remove("data-cfx-source-point");
        if (!bin.Value.HasValue) metadata.Remove("data-cfx-y");
        metadata["data-cfx-source-points"] = string.Join(",", bin.SourceIndices.Select(index => Number(index)));
        metadata["data-cfx-bin-index"] = Number(pointIndex);
        metadata["data-cfx-bin-lower"] = Number(bin.LowerBound);
        metadata["data-cfx-bin-upper"] = Number(bin.UpperBound);
        metadata["data-cfx-bin-width"] = Number(bin.Width);
        metadata["data-cfx-bin-count"] = Number(bin.Count);
        metadata["data-cfx-bin-has-value"] = bin.Value.HasValue ? "true" : "false";
        metadata["data-cfx-histogram-aggregation"] = series.HistogramAggregation!.Value.ToString().ToLowerInvariant();
        metadata["data-cfx-histogram-encoding"] = series.HistogramEncoding!.Value.ToString().ToLowerInvariant();
        metadata["data-cfx-derived"] = "histogram-bin";
        if (bin.Value.HasValue) metadata["data-cfx-bin-value"] = Number(bin.Value.Value);
        if (series.IsHistogramDensity) metadata["data-cfx-rendered-y"] = Number(bin.RenderedValue);
    }

    private static void DrawDensityHistogramSurface(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series,
        int index, int pointIndex, ChartRect bounds, VisualThemeColors colors) {
        var color = PointColor(series, index, pointIndex, colors);
        builder.Rect(bounds, color, role: "bar", paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color, pointIndex)));
        var pattern = pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
            ? series.PointFillPatterns[pointIndex]!.Value : series.FillPattern;
        DrawPattern(builder, RectanglePath(bounds), pattern, color, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "bar-pattern");
    }
}
