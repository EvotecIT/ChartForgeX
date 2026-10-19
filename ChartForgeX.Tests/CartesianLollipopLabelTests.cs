using System.Text.Json;
using System.Globalization;
using System.Xml.Linq;
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

    [Theory]
    [InlineData(390, false)]
    [InlineData(760, false)]
    [InlineData(390, true)]
    [InlineData(760, true)]
    public void DenseAutomaticCaptionsStayBesideTheirOwnValueEndpoints(int width, bool reversed) {
        var xs = Enumerable.Range(1, 24).Where(x => x is not (3 or 9 or 15 or 21)).ToArray();
        var chart = Chart.Create().WithSize(width, 420).WithTitle("Regional changes").WithLegend(false).WithDataLabels()
            .WithXLabels(Enumerable.Range(1, 24).Select(x => $"Region {x}: international distribution").ToArray())
            .AddLollipop("Observed regional change", xs.Select((x, i) => new ChartPoint(x,
                i is 4 or 14 ? 0 : (i % 3 == 0 ? -1 : 1) * (5 + i % 8 * 3))));
        chart.Options.YAxis.WithReversal(reversed);
        chart.Series[0].WithObservationIds(xs.Select(x => new ChartObservationId("region-" + x)));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var document = XDocument.Parse(prepared.ToSvg());
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        Assert.NotEmpty(labels);
        foreach (var label in labels) {
            var pointId = label.Id!.Substring(0, label.Id.Length - "-label".Length);
            var point = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-source-id") == pointId);
            var marker = point.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "lollipop-marker");
            double Number(string name) => double.Parse(marker.Attribute(name)!.Value, CultureInfo.InvariantCulture);
            var bounds = CartesianReversalLabelTests.Bounds(label);
            var margin = 2 * chart.Options.Theme.Spacing + Math.Max(Number("rx"), Number("ry"));
            Assert.True(bounds.Left <= Number("cx") + margin && bounds.Right >= Number("cx") - margin
                && bounds.Top <= Number("cy") + margin && bounds.Bottom >= Number("cy") - margin,
                "A value caption must stay near its own endpoint or be omitted; a distant stem edge needs an explicit association.");
            Assert.NotNull(point.Attribute("data-cfx-observation-id"));
            Assert.Single(point.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "lollipop-stem");
        }
        Assert.NotEmpty(prepared.ToPng());
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
