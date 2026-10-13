using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Tile codes remain complete and readable inside their native hexagons or remain descriptive facts only.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TileMapLabelFittingTests : IDisposable {
    private const string Family = "CFX Tile Label Carlito";

    public TileMapLabelFittingTests() {
        FontRegistry.Register(Family, Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        FontRegistry.Register("CFX Proof Carlito", Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"), 400);
        FontRegistry.Register("CFX Proof Carlito", Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Bold.ttf"), 700);
    }

    [Theory]
    [InlineData(800, 440, VisualThemeMode.Light)]
    [InlineData(800, 440, VisualThemeMode.Dark)]
    [InlineData(360, 360, VisualThemeMode.Light)]
    [InlineData(360, 360, VisualThemeMode.Dark)]
    public void CatalogCodesFitTheirHexagonsAtReadableSizesAndNeverBecomePrefixes(int width, int height, VisualThemeMode mode) {
        var variant = width == 360 ? "compact-color-dimension" : "color-dimension";
        var chart = V2GalleryModels.Create(ChartSeriesKind.TileMap, variant, mode);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode,
            new VisualFrame("Temperature by state", "Temperature captions use their own format and culture"), FontSpec.FromFamily(Family));
        var prepared = chart.Prepare(context); var scene = prepared.Scene;
        var sources = scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "tile-map-region-source").ToArray();
        var labels = scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "tile-map-label").ToDictionary(node => node.Id!);
        Assert.Equal(51, sources.Length);
        Assert.NotEmpty(labels);
        var minimumStyle = new TextStyle { Font = context.Font, FontSize = 10 };
        var measure = new VisualSceneBuilder(new VisualSize(width, height), context.Font);
        foreach (var source in sources) {
            var code = source.Metadata["data-cfx-region"];
            var tile = scene.Regions.Single(region => region.Id == source.Id && region.Role == "tile-map-region").Bounds;
            var labelId = source.Id + "-label";
            Assert.Equal(code, scene.Regions.Single(region => region.Id == labelId).Label);
            var description = scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Id == labelId + "-source");
            Assert.Equal(code, description.Metadata["aria-label"]);
            if (!labels.TryGetValue(labelId, out var label)) {
                Assert.False(FitsHex(tile, measure.MeasureText(code, minimumStyle), chart.Options.MapRegionStrokeWidth));
                continue;
            }
            Assert.Equal(code, string.Join("\n", label.Text.Lines.Select(line => line.Text)));
            Assert.InRange(label.Text.Size, 10, context.Theme.Typography.AxisSize);
            Assert.True(FitsHex(tile, label.Text.Metrics, chart.Options.MapRegionStrokeWidth), code + " must clear every slanted edge and its stroke.");
            Assert.Equal(tile.Left + tile.Width / 2, label.X, 6);
            Assert.Equal(tile.Top + (tile.Height - label.Text.Metrics.Height) / 2, label.Baseline - label.Text.Ascent, 6);
        }
        var svg = XDocument.Parse(prepared.ToSvg());
        var svgLabels = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "tile-map-label").ToArray();
        Assert.Equal(labels.Count, svgLabels.Length);
        Assert.All(svgLabels, node => Assert.Equal(labels[node.Attribute("data-cfx-source-id")!.Value].Text.Lines[0].Text, node.Value));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void CatalogThumbnailCodesKeepTheirRequestedSizeBeforeCssScaling() {
        var chart = V2GalleryModels.Create(ChartSeriesKind.TileMap, "wide", VisualThemeMode.Light);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 280), 14),
            VisualTheme.Graphite().WithTypography(new VisualTypography(axisSize: 13.5)),
            frame: new VisualFrame("", "", showLegend: false), font: FontSpec.FromFamily(Family));
        var prepared = chart.Prepare(context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "tile-map-label").ToArray();
        Assert.Equal(51, labels.Length);
        Assert.All(labels, label => Assert.Equal(13.5, label.Text.Size));
        Assert.Equal(51, prepared.Regions.Count(region => region.Role == "tile-map-region"));
        Assert.Equal(51, prepared.Regions.Count(region => region.Role == "tile-map-label"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData("cartesian")]
    [InlineData("chart-grid")]
    [InlineData("histogram")]
    [InlineData("slope")]
    [InlineData("radar")]
    [InlineData("polar")]
    public void GeneratedThumbnailAxesRetainReadableTypeAcrossCrowdedFamilies(string family) {
        IVisualRenderable model = family switch {
            "cartesian" => V2Examples.Create("cartesian", "wide"),
            "chart-grid" => ChartGrid.Create().Add(Chart.Create().AddLine("Requests", new[] { new ChartPoint(1, 5), new ChartPoint(2, 10) })),
            "histogram" => V2GalleryModels.CreateHistogram("wide"),
            _ => V2GalleryModels.Create(Enum.Parse<ChartSeriesKind>(family, true), "wide", VisualThemeMode.Light)
        };
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-thumbnail-readability-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var write = typeof(V2Examples).GetMethod("WriteThumbnail", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            write.Invoke(null, new object?[] { output, model, "readable", "Readable overview", family, VisualThemeMode.Light, family != "histogram" });
            var svg = XDocument.Load(Path.Combine(output, "readable.thumbnail.svg"));
            var labels = svg.Descendants().Where(node => node.Name.LocalName == "text" && node.Ancestors().Any(owner =>
                ((string?)owner.Attribute("data-cfx-role")) is "axis-y-label" or "radar-ring-label" or "polar-radius-label")).ToArray();
            Assert.True(labels.Length >= 2, "The overview must retain useful value-axis captions.");
            Assert.All(labels, label => Assert.True(double.Parse(label.Attribute("font-size")!.Value, System.Globalization.CultureInfo.InvariantCulture) >= 12,
                "Overview axis type must remain readable when its 400 px SVG shrinks into a compact catalog tile."));
            Assert.True(new FileInfo(Path.Combine(output, "readable.thumbnail.png")).Length > 0);
        } finally { Directory.Delete(output, true); }
    }

    [Fact]
    public void LongAndTinyCodesAreOmittedWithoutLosingTheirRegionIdentityAndVisibilityControl() {
        var longCode = new string('W', 256);
        var definition = new ChartTileMapDefinition("identities", "Identities", new[] {
            new ChartTileMapRegion(longCode, "Long name", 0, 0), new ChartTileMapRegion("OK", "Other name", 1, 0) });
        var chart = Chart.Create().AddTileMap("Samples", definition, new[] { new ChartRegionMapItem(longCode, 0) })
            .ConfigureMapColorDimension(options => options.ShowLegend = false).WithMapSurface(false);
        var scene = Compile(chart, 120, 70);
        Assert.DoesNotContain(scene.Nodes.OfType<VisualSceneText>(), text => text.Id == "series-0-point-0-label");
        Assert.Contains(scene.Nodes.OfType<VisualSceneText>(), text => text.Id == "tile-map-empty-OK-label" && text.Text.Lines[0].Text == "OK");
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "map.label-overflow");
        var source = scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Id == "series-0-point-0");
        Assert.Equal(longCode, source.Metadata["data-cfx-region"]);
        Assert.Equal("0", source.Metadata["data-cfx-value"]);
        Assert.Contains("Long name (" + longCode + "): 0", source.Metadata["aria-label"]);
        Assert.Contains(scene.Regions, region => region.Id == "series-0-point-0-label" && region.Label == longCode);
        var tiny = Compile(chart, 5, 5);
        Assert.DoesNotContain(tiny.Nodes, node => node is VisualSceneText && node.Role == "tile-map-label");
        Assert.Equal(2, tiny.Diagnostics.Count(diagnostic => diagnostic.Code == "map.label-overflow"));
        chart.WithMapLabels(false);
        var hidden = Compile(chart, 120, 70);
        Assert.DoesNotContain(hidden.Nodes, node => node.Role is "tile-map-label" or "tile-map-label-source");
        Assert.Equal(2, hidden.Nodes.Count(node => node.Role == "tile-map-region"));
        Assert.Equal(source.Metadata, hidden.Nodes.OfType<VisualSceneGroup>().Single(node => node.Id == source.Id).Metadata);
        Assert.DoesNotContain(hidden.Diagnostics, diagnostic => diagnostic.Code == "map.label-overflow");
    }

    [Fact]
    public void AuthoredSmallTypeAndExplicitInkArePreservedWithoutChangingSharedTickStyle() {
        var chart = Chart.Create().AddTileMap("Samples", new ChartTileMapDefinition("small", "Small", new[] {
            new ChartTileMapRegion("OK", "Other name", 0, 0) }), new[] { new ChartRegionMapItem("OK", 10) })
            .ConfigureMapColorDimension(options => options.ShowLegend = false).WithMapSurface(false);
        var ink = ChartColor.FromHex("#1234AB");
        chart.Options.TickLabelStyle.FontSize = 6;
        chart.Options.TickLabelStyle.Color = ink;
        var scene = Compile(chart, 40, 40);
        var label = Assert.Single(scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "tile-map-label");
        Assert.Equal(6, label.Text.Size); Assert.Equal(ink, label.Color);
        Assert.Equal(6, chart.Options.TickLabelStyle.FontSize);
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        Assert.Contains(svg.Descendants(), node => node.Name.LocalName == "text" && (string?)node.Attribute("fill") == ink.ToCss() && node.Value == "OK");
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void OversizedAuthoredCodesFitWithoutCrossingTheReadableFloorOrChangingTheRequest() {
        var chart = Chart.Create().AddTileMap("Samples", new ChartTileMapDefinition("large", "Large", new[] {
            new ChartTileMapRegion("OK", "Other name", 0, 0) }), new[] { new ChartRegionMapItem("OK", 10) })
            .ConfigureMapColorDimension(options => options.ShowLegend = false).WithMapSurface(false);
        chart.Options.TickLabelStyle.FontSize = 40;
        var scene = Compile(chart, 30, 30);
        var label = Assert.Single(scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "tile-map-label");
        Assert.InRange(label.Text.Size, 10, 39.99);
        Assert.Equal("OK", Assert.Single(label.Text.Lines).Text);
        Assert.Equal(40, chart.Options.TickLabelStyle.FontSize);
        Assert.DoesNotContain(scene.Diagnostics, diagnostic => diagnostic.Code == "map.label-overflow");
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    private static bool FitsHex(ChartRect tile, TextMetrics text, double stroke) => text.Height + stroke <= tile.Height + .000001
        && text.Width + stroke + .44 * (text.Height + stroke) <= tile.Width + .000001;

    private static VisualScene Compile(Chart chart, int width, int height) {
        var context = new VisualRenderContext(font: FontSpec.FromFamily(Family));
        var builder = new VisualSceneBuilder(new VisualSize(width, height), context.Font);
        VisualMapCompiler.Build(chart, context, builder, new ChartRect(0, 0, width, height));
        return builder.Build();
    }

    public void Dispose() => FontRegistry.Clear();
}
