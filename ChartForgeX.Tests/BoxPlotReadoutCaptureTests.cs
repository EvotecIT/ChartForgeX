using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the formatter owner of raw distribution facts and independently selected outliers.</summary>
public sealed class BoxPlotReadoutCaptureTests {
    [Theory]
    [InlineData("primary")]
    [InlineData("horizontal")]
    [InlineData("secondary")]
    public void DistributionAndOutlierReadoutsShareTheCapturedValueAxisWithoutChangingRawFacts(string axisKind) {
        var series = ChartSeries.CreateBoxPlot(new ChartSeriesId("latency"), "Latency", 1,
            new[] { .1, 1d, 2, 3, 4, 5, 6, 7, 8, 100.123 })
            .ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        var chart = Chart.Create().WithSize(540, 360).WithAxes(false).WithHeader(false).WithLegend(false)
            .WithDataLabels(false).WithXLabels("API").AddSeries(series);
        var axis = chart.Options.YAxis;
        if (axisKind == "horizontal") {
            chart.ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal);
            axis = chart.Options.XAxis;
        } else if (axisKind == "secondary") {
            series.YAxis = ChartAxisSide.Secondary;
            chart.AddLine("Primary", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) });
            chart.Options.YAxis.LabelFormatter = _ => "Primary units must not leak";
            axis = chart.Options.SecondaryYAxis;
        }
        var calls = new Dictionary<double, int>();
        string Caption(double value) => value == 2.25 ? string.Empty : value.ToString("0.00", CultureInfo.GetCultureInfo("de-DE")) + " ms";
        axis.LabelFormatter = value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return Caption(value);
        };
        var prepared = chart.Prepare(new VisualRenderContext());
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "point"
            && node.Metadata.TryGetValue("data-cfx-series", out var ordinal) && ordinal == "0").ToArray();
        var summary = Assert.Single(points, node => node.Metadata.ContainsKey("data-cfx-derived"));
        var outlier = Assert.Single(points, node => node.Metadata.TryGetValue("data-cfx-kind", out var kind) && kind == "BoxPlotOutlier");
        Assert.Equal("4,50 ms", summary.Metadata["data-cfx-formatted-value"]);
        Assert.Equal(summary.Metadata["data-cfx-formatted-median"], summary.Metadata["data-cfx-formatted-value"]);
        Assert.Equal("100,12 ms", outlier.Metadata["data-cfx-formatted-value"]);
        Assert.Equal(100.123, Raw(outlier, "value"));
        foreach (var member in new[] { "minimum", "q1", "median", "q3", "maximum", "source-minimum", "source-maximum", "inlier-minimum", "inlier-maximum" }) {
            var number = Raw(summary, member);
            Assert.Equal(Caption(number), summary.Metadata["data-cfx-formatted-" + member]);
            Assert.Equal(1, calls[number]);
        }
        Assert.Equal(string.Empty, summary.Metadata["data-cfx-formatted-q1"]);
        Assert.Equal(.1, Raw(summary, "source-minimum"));
        Assert.Equal(100.123, Raw(summary, "source-maximum"));
        Assert.Equal("1.5", summary.Metadata["data-cfx-formatted-iqr-multiplier"]);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        Assert.Contains("Source minimum=0,10 ms", summary.Metadata["aria-label"]);
        Assert.Contains("Source maximum=100,12 ms", summary.Metadata["aria-label"]);
        Assert.Contains("Inlier minimum=0,10 ms", summary.Metadata["aria-label"]);
        Assert.Contains("Inlier maximum=8,00 ms", summary.Metadata["aria-label"]);
        Assert.Equal("Latency: API 100,12 ms", outlier.Metadata["aria-label"]);
        Assert.All(points, point => Assert.Equal(point.Metadata["aria-label"],
            Assert.Single(prepared.Regions, region => region.Id == point.Id && region.Role == "point").Label));
        var svg = prepared.ToSvg();
        axis.LabelFormatter = _ => throw new InvalidOperationException("Export must not rerun a captured measurement formatter.");
        Assert.Equal(svg, prepared.ToSvg());
        Assert.NotEmpty(prepared.ToPng());
        Assert.All(calls.Values, count => Assert.Equal(1, count));
    }

    private static double Raw(VisualSceneGroup node, string member) =>
        double.Parse(node.Metadata["data-cfx-" + member], CultureInfo.InvariantCulture);
}
