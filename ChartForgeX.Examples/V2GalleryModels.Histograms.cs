using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

public static partial class V2GalleryModels {
    /// <summary>Creates count, density, sum and mean examples over the same unequal measurement intervals.</summary>
    public static Chart CreateHistogram(string variant) {
        var mode = variant.StartsWith("compact-", StringComparison.Ordinal) ? variant.Substring(8) : variant;
        if (mode == "compact") mode = "wide";
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1, 3, 6, 10 });
        var chart = Chart.Create().WithXAxis("Measurement").WithDataLabels();
        chart.Options.XAxis.WithBounds(0, 10);
        if (mode is "wide" or "density") {
            chart.AddHistogram("Observations", new[] { .2, .4, .8, 1.2, 1.8, 2.5, 6.1, 7.5, 9 }, layout,
                mode == "density" ? ChartHistogramEncoding.Density : ChartHistogramEncoding.Value);
            chart.WithYAxis(mode == "density" ? "Observations per unit" : "Observation count");
            foreach (var bin in chart.Series[0].HistogramBins)
                chart.Series[0].WithPointLabel(bin.Index, bin.Count + " obs");
        } else if (mode is "sum" or "mean") {
            chart.AddHistogram("Quantity", new[] { new ChartPoint(.2, 10), new ChartPoint(.4, -4),
                new ChartPoint(1.2, 8), new ChartPoint(2.5, 12), new ChartPoint(6.1, -4), new ChartPoint(9, -8) }, layout,
                mode == "sum" ? ChartHistogramAggregation.Sum : ChartHistogramAggregation.Mean);
            chart.WithYAxis(mode == "sum" ? "Total quantity" : "Mean quantity");
        } else throw new ArgumentOutOfRangeException(nameof(variant));
        return chart;
    }
}
