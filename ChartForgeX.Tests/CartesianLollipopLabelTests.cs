using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianLollipopLabelTests {
    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(false, false, -1)]
    [InlineData(false, true, -1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(true, false, -1)]
    [InlineData(true, true, -1)]
    public async Task AutomaticLollipopLabelsStayBeyondTheirMappedValueEndpoint(bool secondary, bool reversed, int sign) {
        var chart = Create(secondary, reversed, sign);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var marks = prepared.Regions.Where(region => region.Role == "point").ToArray();
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var name = $"lollipop-{(secondary ? "secondary" : "primary")}-{(reversed ? "reversed" : "normal")}-{sign}";
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new {
                secondary, reversed, sign, source = chart.Series[0].Points.Select(point => point.Y),
                marks = marks.Select(mark => mark.Bounds), labels = labels.Select(CartesianReversalLabelTests.Bounds)
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        Assert.Equal(2, labels.Length);
        for (var index = 0; index < labels.Length; index++) {
            var label = CartesianReversalLabelTests.Bounds(labels[index]);
            Assert.True(sign > 0 != reversed ? label.Bottom < marks[index].Bounds.Top : label.Top > marks[index].Bounds.Bottom,
                "The automatic caption must follow the mapped endpoint rather than enter the stem.");
        }
    }

    internal static Chart Create(bool secondary, bool reversed, int sign) {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels().AddLollipop("Observations", new[] { new ChartPoint(1, 40 * sign), new ChartPoint(2, 60 * sign) });
        if (secondary) chart.Series[0].UseSecondaryYAxis();
        chart.Options.YAxis.WithBounds(-100, 100).WithReversal(secondary ? false : reversed);
        chart.Options.SecondaryYAxis.WithBounds(-100, 100).WithReversal(secondary ? reversed : false);
        return chart;
    }
}
