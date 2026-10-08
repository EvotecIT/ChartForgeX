using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteRenderingTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void InsideBarLabelsHaveReadablePairedInkAndRespectExplicitColours(bool dark, bool semantic) {
        var tokens = dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight();
        var chart = Chart.Create().WithDesignTokens(tokens).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside)
            .AddBar("Counts", new[] { new ChartPoint(1, 80), new ChartPoint(2, 60) });
        if (semantic) chart.WithSeriesState("Counts", ChartSeriesState.Danger);
        var prepared = Prepare(chart);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        var bars = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").ToArray();
        Assert.Equal(2, labels.Length); Assert.Equal(2, bars.Length);
        for (var index = 0; index < labels.Length; index++)
            Assert.True(ChartColorMath.ContrastRatio(labels[index].Color, bars[index].Fill!.Value) >= 4.5);
        var pixels = chart.ToPng(); chart.WithSvgColorVariables(tokens.ToSvgColorVariables());
        Assert.Contains("ink", chart.ToSvg()); Assert.Equal(pixels, chart.ToPng());
        chart.WithSvgColorVariables(null); chart.Series[0].DataLabelStyle.Color = ChartColor.FromHex("#C2418A");
        Assert.All(Prepare(chart).Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label"),
            node => Assert.Equal(ChartColor.FromHex("#C2418A"), node.Color));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    public void AxisFontStacksSurviveXmlEncodingOnce(double rotation) {
        const string family = "Calibri, \"Segoe UI\", \"Sample & Family\", sans-serif";
        var chart = Chart.Create().WithSize(600, 360).WithXLabels("One", "Two").WithDataLabels()
            .AddBar("Counts", new[] { new ChartPoint(1, 12), new ChartPoint(2, 24) });
        chart.Options.Theme.FontFamily = family; chart.Options.XAxis.LabelAngle = rotation;
        var nodes = XDocument.Parse(chart.ToSvg()).Descendants().ToArray();
        Assert.Contains(nodes, node => (string?)node.Attribute("data-cfx-role") == "axis-x-label");
        Assert.Contains(nodes, node => (string?)node.Attribute("data-cfx-role") == "axis-y-label");
        Assert.All(nodes.Where(node => node.Name.LocalName == "text"), node => Assert.Equal(family, (string?)node.Attribute("font-family")));
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(220)]
    [InlineData(260)]
    [InlineData(300)]
    public void CompactInlineLegendsRetainShortSeriesNames(int width) {
        var chart = Chart.Create().WithSize(width, 320).WithTitle("Panel").WithSubtitle("Current status")
            .AddLine("Passed", new[] { new ChartPoint(1, 100), new ChartPoint(2, 110) })
            .AddLine("Warnings", new[] { new ChartPoint(1, 10), new ChartPoint(2, 12) })
            .AddLine("Failed", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) });
        var prepared = Prepare(chart);
        Assert.Equal(chart.Series.Select(series => series.Name), prepared.Regions.Where(region => region.Role == "legend").Select(region => region.Label));
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "legend-label"),
            node => Assert.Equal(chart.Options.Theme.LegendFontSize, node.Text.Size));
    }

    [Fact]
    public void DefaultFrameIsFlatAndTheHostCanOwnItsPresentation() {
        var chart = Chart.Create().WithTitle("Readiness").AddBar("Checks", new[] { new ChartPoint(0, 8), new ChartPoint(1, 12) });
        var prepared = Prepare(chart);
        Assert.Single(prepared.Scene.Nodes, node => node.Role == "frame-card");
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node is VisualSceneGradient);
        chart.WithHostFrame(); var hosted = Prepare(chart);
        Assert.DoesNotContain(hosted.Scene.Nodes, node => node.Role is "frame-card" or "content-surface" or "frame-heading" or "background");
        Assert.Equal(2, hosted.Regions.Count(region => region.Role == "point"));
    }

    [Fact]
    public void StateLinesUseSemanticPaintAndHealthySeriesIsUnderneath() {
        var chart = Chart.Create().AddLine("Failures", new[] { new ChartPoint(0, 3), new ChartPoint(1, 4), new ChartPoint(2, 2) })
            .AddLine("Healthy", new[] { new ChartPoint(0, 8), new ChartPoint(1, 9), new ChartPoint(2, 10) })
            .WithSeriesState("Failures", ChartSeriesState.Danger).WithSeriesState("Healthy", ChartSeriesState.Quiet);
        var prepared = Prepare(chart);
        var series = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "series").ToArray();
        Assert.Equal("1", series[0].Metadata["data-cfx-series"]);
        Assert.Equal(chart.Options.Theme.Positive, prepared.Scene.Nodes.OfType<VisualScenePath>().First(path => path.Role == "line").Stroke!.Value);
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "marker"));
        chart.WithLineMarkers(ChartLineMarkerMode.All);
        Assert.Equal(6, Prepare(chart).Scene.Nodes.Count(node => node.Role == "marker"));
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
