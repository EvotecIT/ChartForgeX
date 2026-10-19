using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LegendNumericLayoutTests {
    [Theory]
    [InlineData(false, 320, ChartLegendPosition.TopLeft)]
    [InlineData(true, 640, ChartLegendPosition.TopLeft)]
    [InlineData(false, 900, ChartLegendPosition.Right)]
    public void OverflowBoundsRetainCompleteExponentsAndSeparateLegendColumns(bool donut, int width, ChartLegendPosition position) {
        var chart = Create(donut, width, position, 1e308, 1e308, 1e308);
        chart.Options.MaximumPieSlices = 2;
        var prepared = Prepare(chart);
        AssertColumns(prepared);
        var value = prepared.Scene.Nodes.OfType<VisualSceneText>().Last(node => node.Role == "legend-value");
        var caption = Assert.Single(value.Text.Lines).Text;
        Assert.StartsWith(">", caption);
        Assert.EndsWith("308", caption);
        Assert.DoesNotContain("…", caption);
        var document = XDocument.Parse(prepared.ToSvg());
        var aggregate = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "legend-entry"
            && (string?)element.Attribute("data-cfx-value-overflow") == "true");
        Assert.Equal("> 1.7976931348623157E+308", (string?)aggregate.Attribute("data-cfx-formatted-value"));
        Assert.Contains("> 1.7976931348623157E+308", (string?)aggregate.Attribute("aria-label"));
        Assert.Equal("1,2", (string?)aggregate.Attribute("data-cfx-source-points"));
        Assert.Null(aggregate.Attribute("data-cfx-value"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void ALongAuthoredValueCannotEraseItsCategoryAndKeepsItsCompleteDescription() {
        const string authored = "123456789012345678901234567890 authored units";
        var chart = Create(false, 900, ChartLegendPosition.Right, 3, 7)
            .WithXLabels("International category", "Second category")
            .ConfigureLegendStyle(style => style.WithFontSize(16).WithFontFamily("serif").WithTextCase(TextCaseTransform.Uppercase));
        chart.Options.ValueFormat = ChartValueFormat.Custom(_ => authored);
        var prepared = Prepare(chart);
        AssertColumns(prepared);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role is "legend-label" or "legend-value"), node => {
            Assert.Equal(16, node.Text.Size);
            Assert.Equal("serif", node.Text.Style.Font.Family);
            Assert.NotEmpty(Assert.Single(node.Text.Lines).Text);
        });
        Assert.All(prepared.Regions.Where(region => region.Role == "legend"), region => Assert.Contains(authored, region.Label));
        Assert.All(XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "legend-entry"),
            element => Assert.Contains(authored, (string?)element.Attribute("aria-label")));
        Assert.Contains(prepared.Scene.Diagnostics, diagnostic => diagnostic.Code == "frame.text-truncated");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void OrdinaryFiniteLegendValuesKeepTheirCompleteCaptionsAndSpacing() {
        var prepared = Prepare(Create(false, 640, ChartLegendPosition.TopLeft, 3, 7));
        AssertColumns(prepared);
        var values = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "legend-value").ToArray();
        var percentages = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "legend-percentage").ToArray();
        Assert.Equal(new[] { "7", "3" }, values.Select(node => Assert.Single(node.Text.Lines).Text));
        for (var index = 0; index < values.Length; index++)
            Assert.Equal(8, Left(percentages[index]) - Right(values[index]), 7);
        Assert.DoesNotContain(prepared.Scene.Diagnostics, diagnostic => diagnostic.Code is "frame.legend-value-compacted" or "frame.text-truncated");
    }

    private static void AssertColumns(PreparedVisual prepared) {
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "legend-label").ToArray();
        var values = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "legend-value").ToArray();
        var percentages = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "legend-percentage").ToArray();
        var regions = prepared.Regions.Where(region => region.Role == "legend" && region.Bounds.Height > 0).ToArray();
        Assert.Equal(2, labels.Length); Assert.Equal(labels.Length, values.Length); Assert.Equal(values.Length, percentages.Length);
        for (var index = 0; index < labels.Length; index++) {
            Assert.NotEmpty(Assert.Single(labels[index].Text.Lines).Text);
            Assert.True(Right(labels[index]) < Left(values[index]), "The category and value must occupy separate columns.");
            Assert.True(Right(values[index]) < Left(percentages[index]), "The value and percentage must occupy separate columns.");
            Assert.InRange(Left(labels[index]), regions[index].Bounds.Left, regions[index].Bounds.Right);
            Assert.InRange(Right(percentages[index]), regions[index].Bounds.Left, regions[index].Bounds.Right + .000001);
        }
    }

    private static double Left(VisualSceneText text) => text.LineLeft(Assert.Single(text.Text.Lines));
    private static double Right(VisualSceneText text) => Left(text) + Assert.Single(text.Text.Lines).Width;
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static Chart Create(bool donut, int width, ChartLegendPosition position, params double[] values) {
        var chart = Chart.Create().WithSize(width, 400).WithXLabels("First", "Second", "Third").WithLegendPosition(position);
        var points = values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
        return donut ? chart.AddDonut("Parts", points) : chart.AddPie("Parts", points);
    }
}
