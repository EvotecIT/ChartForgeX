using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects radial value geometry, source identity and measured labels before serialization.</summary>
public sealed class V2RadialTests {
    [Fact]
    public void Donut_WeightsZeroValuesAndOffsets_PreserveGeometryAndIdentity() {
        var chart = Donut(0, 75, 25).WithXLabels("Zero", "Pass", "Fail");
        var red = ChartColor.FromHex("#BB3311");
        chart.Series[0].WithPointColor(1, red).WithPointSliceOffset(1, 0.2);
        var scene = Compile(chart);
        var slices = scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        Assert.Equal(2, slices.Length);
        Assert.Equal("series-0-point-1", slices[0].Id);
        Assert.Equal(Math.PI * 1.5, slices[0].Sweep, 10);
        Assert.Equal(Math.PI * 0.5, slices[1].Sweep, 10);
        Assert.Equal(red, slices[0].Fill);
        Assert.Equal(slices[0].Outer * chart.Options.DonutInnerRadiusRatio, slices[0].Inner, 10);
        Assert.NotEqual(slices[1].Cx, slices[0].Cx);
        Assert.NotEqual(slices[1].Cy, slices[0].Cy);
        var metadata = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "radial-point").ToArray();
        Assert.Equal("1", metadata[0].Metadata["data-cfx-source-points"]);
        Assert.Equal("0.75", metadata[0].Metadata["data-cfx-percent"]);
        Assert.Equal(3, VisualRadialCompiler.LegendEntries(chart, new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light)).Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SinglePositiveValue_ProducesAFullCircleOrRing(bool donut) {
        var chart = donut ? Donut(12) : Chart.Create().AddPie("Total", Points(12));
        chart.WithDonutCenterLabel(false);
        var scene = Compile(chart);
        var slice = Assert.Single(scene.Nodes.OfType<VisualSceneSlice>());
        Assert.Equal(Math.PI * 2, slice.Sweep, 10);
        Assert.Equal(-Math.PI / 2, slice.Start, 10);
        Assert.Equal(donut ? slice.Outer * chart.Options.DonutInnerRadiusRatio : 0, slice.Inner, 10);
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        var path = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == (donut ? "donut-slice" : "pie-slice"));
        Assert.Equal("evenodd", (string?)path.Attribute("fill-rule"));
        var image = VisualSceneRasterRenderer.Render(scene);
        var center = ((int)slice.Cy * image.Width + (int)slice.Cx) * 4;
        Assert.Equal(donut ? 0 : 255, image.Pixels[center + 3]);
        var ringX = (int)(slice.Cx + (slice.Inner + slice.Outer) / 2);
        var ring = ((int)slice.Cy * image.Width + ringX) * 4;
        Assert.Equal(slice.Fill!.Value.R, image.Pixels[ring]);
        Assert.Equal(slice.Fill.Value.G, image.Pixels[ring + 1]);
        Assert.Equal(slice.Fill.Value.B, image.Pixels[ring + 2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyOrAllZeroValues_ProduceNoDataDiagnosticWithoutInvalidAngles(bool empty) {
        var chart = empty ? Donut() : Donut(0, 0, 0);
        var scene = Compile(chart);
        Assert.Empty(scene.Nodes.OfType<VisualSceneSlice>());
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.no-data");
        Assert.Contains(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "no-data");
    }

    [Fact]
    public void Aggregation_IsIndependentOfLegacyThemeAndRetainsEveryContributingSource() {
        var chart = Donut(1, 9, 2, 8, 3).WithXLabels("A", "B", "C", "D", "E");
        chart.Options.MaximumPieSlices = 3;
        var light = Compile(chart);
        chart.WithTheme(ChartTheme.GraphiteLight());
        var graphite = Compile(chart);
        var expected = new[] { "series-0-point-1", "series-0-point-3", "series-0-point-other" };
        Assert.Equal(expected, light.Nodes.OfType<VisualSceneSlice>().Select(slice => slice.Id));
        Assert.Equal(expected, graphite.Nodes.OfType<VisualSceneSlice>().Select(slice => slice.Id));
        var other = light.Nodes.OfType<VisualSceneGroup>().Single(group => group.Metadata.TryGetValue("data-cfx-point", out var point) && point == "-1");
        Assert.Equal("0,2,4", other.Metadata["data-cfx-source-points"]);
        Assert.Equal("6", other.Metadata["data-cfx-value"]);
        Assert.Equal(Math.PI * 2, light.Nodes.OfType<VisualSceneSlice>().Sum(slice => slice.Sweep), 10);
    }

    [Fact]
    public void Formatter_ReceivesRealAggregateValuesAndSourceSentinel() {
        var contexts = new List<ChartPieSliceLabelContext>();
        var chart = Donut(6, 3, 1).WithDataLabels();
        chart.Options.MaximumPieSlices = 2;
        chart.Options.ValueFormatter = value => value.ToString("0.0", CultureInfo.InvariantCulture) + " units";
        chart.WithPieSliceLabelFormatter(value => { contexts.Add(value); return value.FormattedValue; });
        Compile(chart);
        Assert.Equal(2, contexts.Count);
        var other = contexts.Single(value => value.PointIndex == -1);
        Assert.Equal("Other", other.Label);
        Assert.Equal(4, other.Value);
        Assert.Equal(0.4, other.Percent, 10);
        Assert.Equal("4.0 units", other.FormattedValue);
    }

    [Fact]
    public void LongCenterText_IsMeasuredWithinTheDonutHole() {
        var chart = Donut(40, 60).WithDonutCenterText("A very long headline total with units", "A long descriptive caption");
        var scene = Compile(chart);
        var inner = scene.Nodes.OfType<VisualSceneSlice>().First().Inner;
        var lines = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role is "donut-total-label" or "donut-title").ToArray();
        Assert.Equal(2, lines.Length);
        Assert.All(lines, text => Assert.True(text.Text.Metrics.Width <= inner * 1.6 + 0.001));
        var valueBottom = lines[0].Baseline - lines[0].Text.Ascent + lines[0].Text.Metrics.Height;
        var captionTop = lines[1].Baseline - lines[1].Text.Ascent;
        Assert.True(valueBottom <= captionTop);
    }

    [Fact]
    public void OutsideLabelOverflow_IsReportedAndKeptWithinAvailableHeight() {
        var chart = Donut(1, 1, 1, 1, 1, 1, 1, 1).WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.Label);
        chart.Options.MaximumPieSlices = 10;
        chart.Options.DataLabelPlacement = ChartDataLabelPlacement.Right;
        var scene = Compile(chart, new ChartRect(0, 0, 500, 80));
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.label-overflow");
        var labels = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label").OrderBy(text => text.Baseline).ToArray();
        Assert.NotEmpty(labels);
        for (var index = 1; index < labels.Length; index++) {
            var previousBottom = labels[index - 1].Baseline - labels[index - 1].Text.Ascent + labels[index - 1].Text.Metrics.Height;
            Assert.True(previousBottom <= labels[index].Baseline - labels[index].Text.Ascent + 0.001);
        }
    }

    private static VisualScene Compile(Chart chart, ChartRect? bounds = null) {
        var context = new VisualRenderContext();
        var plot = bounds ?? new ChartRect(0, 0, 420, 320);
        var builder = new VisualSceneBuilder(new VisualSize(Math.Max(1, plot.Right), Math.Max(1, plot.Bottom)), context.Font);
        VisualRadialCompiler.Build(chart, context, builder, plot);
        return builder.Build();
    }

    private static Chart Donut(params double[] values) => Chart.Create().AddDonut("Total", Points(values));
    private static IEnumerable<ChartPoint> Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value));
}
