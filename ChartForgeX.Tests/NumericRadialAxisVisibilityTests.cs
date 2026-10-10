using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NumericRadialAxisVisibilityTests {
    [Theory]
    [InlineData(true, "category")]
    [InlineData(false, "category")]
    [InlineData(true, "primary")]
    [InlineData(false, "primary")]
    [InlineData(true, "secondary")]
    [InlineData(false, "secondary")]
    public void HiddenAxisTextCannotReducePaintedRadiusWhileGridRemainsVisible(bool bars, string hidden) {
        var shortLabels = Scene(bars, hidden, false); var longLabels = Scene(bars, hidden, true);
        var expected = NumericRadialSeriesTests.Marks(shortLabels); var actual = NumericRadialSeriesTests.Marks(longLabels);
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < actual.Length; index++) {
            Assert.Equal(expected[index].Inner, actual[index].Inner);
            Assert.Equal(expected[index].Outer, actual[index].Outer);
        }
        Assert.Contains(longLabels.Nodes, node => node.Role == "radial-value-grid");
        Assert.Equal(shortLabels.Nodes.Count(node => node.Role == "radial-value-grid"), longLabels.Nodes.Count(node => node.Role == "radial-value-grid"));
        Assert.Contains(longLabels.Nodes.OfType<VisualSceneText>(), text => text.Role == "radial-value-label");
    }

    private static VisualScene Scene(bool bars, string hidden, bool longText) {
        var text = longText ? "Hidden caption that must not reserve any plotted radius" : "x";
        var chart = NumericRadialSeriesTests.Add(Chart.Create().WithSize(460, 460).WithLegend(false)
            .WithFontFamily("Carlito").WithXLabels(hidden == "category" ? text : "North", "South"), bars, "Requests", new(1, 1200), new(2, 800));
        NumericRadialSeriesTests.Add(chart, bars, "Follow-ups", new(1, 600), new(2, 400));
        chart.Series[1].UseSecondaryYAxis();
        chart.WithYAxisBounds(0, 1500); chart.Options.SecondaryYAxis.WithBounds(0, 1000);
        chart.Options.PngFontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        chart.Options.XAxis.Visible = hidden != "category";
        chart.Options.YAxis.Visible = hidden != "primary";
        chart.Options.SecondaryYAxis.Visible = hidden != "secondary";
        if (hidden == "primary") chart.Options.YAxis.WithLabelFormatter(_ => text);
        if (hidden == "secondary") chart.Options.SecondaryYAxis.WithLabelFormatter(_ => text);
        var scene = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene;
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var stem = Path.Combine(capture, $"axis-{(bars ? "bar" : "column")}-{hidden}-{(longText ? "long" : "short")}");
            File.WriteAllText(stem + ".svg", chart.ToSvg());
            File.WriteAllBytes(stem + "-native.png", chart.ToPng());
        }
        return scene;
    }
}
