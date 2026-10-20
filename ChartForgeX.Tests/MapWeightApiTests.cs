using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the map authoring boundary between optional nonnegative weight and independent signed color.</summary>
public sealed class MapWeightApiTests {
    [Fact]
    public void NamedWeightAndGeoJsonWeightMappingRetainZeroMissingAndSignedColorThroughNativePreparation() {
        const string input = """
            {"type":"FeatureCollection","features":[
              {"type":"Feature","properties":{"name":"Zero","load":0,"delay":-8},"geometry":{"type":"Point","coordinates":[0,50]}},
              {"type":"Feature","properties":{"name":"Missing weight","delay":12},"geometry":{"type":"Point","coordinates":[5,52]}},
              {"type":"Feature","properties":{"name":"Missing color","load":25,"delay":null},"geometry":{"type":"Point","coordinates":[10,54]}}
            ]}
            """;
        var options = new ChartMapGeoJsonOptions { WeightPropertyName = " load ", ColorValuePropertyName = "delay" };
        var points = ChartMapGeoJson.ToMapPoints(input, options);
        Assert.Equal("load", options.WeightPropertyName);
        Assert.Equal(new double?[] { 0, null, 25 }, points.Select(point => point.Weight));
        Assert.Equal(new double?[] { -8, 12, null }, points.Select(point => point.ColorValue));
        var authored = new ChartMapPoint("Zero", 0, 50, weight: 0).WithColorValue(-8);
        Assert.Equal((authored.Weight, authored.ColorValue), (points[0].Weight, points[0].ColorValue));
        var invalid = Assert.Throws<ArgumentOutOfRangeException>(() => new ChartMapPoint("Invalid", 0, 50, weight: -1));
        Assert.Equal("weight", invalid.ParamName);

        var chart = Chart.Create().WithSize(640, 420).WithLegend(false).WithMapViewport(ChartMapViewport.Europe())
            .AddDottedMap("Traffic", points);
        var series = chart.Series[0];
        series.WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#2255CC"), ChartColor.FromHex("#22AA66")).WithValueRange(-8, 12));
        series.ConfigureMapPointSize(size => { size.WithSizeDomain(0, 25); size.MinimumRadius = 2; size.MaximumRadius = 10; });
        var prepared = Prepare(chart);
        var source = Sources(prepared);
        Assert.Equal(new[] { "0", "", "25" }, source.Select(node => node.Metadata["data-cfx-value"]));
        Assert.Equal("-8", source[0].Metadata["data-cfx-color-value"]);
        Assert.Equal("12", source[1].Metadata["data-cfx-color-value"]);
        Assert.Equal("true", source[2].Metadata["data-cfx-color-missing"]);
        var originalMarks = Marks(prepared);
        Assert.Equal(2, originalMarks[0].Rx);
        Assert.Equal(10, originalMarks[2].Rx);
        var svg = prepared.ToSvg();
        var png = prepared.ToPng();
        var image = RasterImageDecoder.Decode(png);
        Assert.Equal(640, image.Width);
        Assert.Equal(420, image.Height);

        series.UpdateMapPoint(0, new ChartMapPoint("Zero", 0, 50, weight: 25).WithColorValue(-8));
        var updated = Prepare(chart);
        Assert.Equal(25, series.MapPoints[0].Weight);
        Assert.Equal(-8, series.MapPoints[0].ColorValue);
        Assert.Equal("25", Sources(updated)[0].Metadata["data-cfx-value"]);
        Assert.Equal("-8", Sources(updated)[0].Metadata["data-cfx-color-value"]);
        Assert.Equal(10, Marks(updated)[0].Rx);
        Assert.Equal(originalMarks[0].Fill, Marks(updated)[0].Fill);
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
    }

    private static VisualSceneGroup[] Sources(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>()
        .Where(node => node.Role == "dotted-map-point-source").ToArray();
    private static VisualSceneEllipse[] Marks(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneEllipse>()
        .Where(node => node.Role == "dotted-map-point").ToArray();
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
