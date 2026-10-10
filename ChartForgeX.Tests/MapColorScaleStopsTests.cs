using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Map colour scales with more than three stops keep every step on the map and in the legend.</summary>
public sealed class MapColorScaleStopsTests {
    private static readonly ChartColor[] Low = { ChartColor.FromHex("#7F1D1D"), ChartColor.FromHex("#DC2626"), ChartColor.FromHex("#FCA5A5") };
    private static readonly ChartColor Neutral = ChartColor.FromHex("#F1F5F9");
    private static readonly ChartColor[] High = { ChartColor.FromHex("#93C5FD"), ChartColor.FromHex("#2563EB"), ChartColor.FromHex("#1E3A8A") };

    [Fact]
    public void Diverging_WithArms_DrawsEveryStopAtItsValueAroundAnOffCentreMidpoint() {
        var scale = ChartColorScale.Diverging(Low, Neutral, High, midpointValue: 2).WithValueRange(-4, 8);
        Assert.Equal(7, scale.Colors.Count);
        Assert.Equal(Low[0], scale.LowColor);
        Assert.Equal(Neutral, scale.MidpointColor);
        Assert.Equal(High[2], scale.HighColor);

        // Three low stops share -4..2 (a step of 2); three high stops share 2..8 (a step of 2).
        Assert.Equal("#7F1D1D", scale.ColorFor(-4, 0, 1).ToHex());
        Assert.Equal("#DC2626", scale.ColorFor(-2, 0, 1).ToHex());
        Assert.Equal("#FCA5A5", scale.ColorFor(0, 0, 1).ToHex());
        Assert.Equal("#F1F5F9", scale.ColorFor(2, 0, 1).ToHex());
        Assert.Equal("#93C5FD", scale.ColorFor(4, 0, 1).ToHex());
        Assert.Equal("#2563EB", scale.ColorFor(6, 0, 1).ToHex());
        Assert.Equal("#1E3A8A", scale.ColorFor(8, 0, 1).ToHex());
        Assert.Equal("#1E3A8A", scale.ColorFor(99, 0, 1).ToHex());
    }

    [Fact]
    public void ThreeColourDiverging_MatchesTheArmOverloadWithOneColourPerSide() {
        var classic = ChartColorScale.Diverging(Low[0], Neutral, High[2], 3);
        var arms = ChartColorScale.Diverging(new[] { Low[0] }, Neutral, new[] { High[2] }, 3);
        for (var value = 0.0; value <= 10; value += 0.5) Assert.Equal(classic.ColorFor(value, 0, 10), arms.ColorFor(value, 0, 10));
    }

    [Fact]
    public void ThreeColourDiverging_HighArm_RoundsLikeADirectBlend() {
        // 5 * 0.1 is 0.5 and rounds to 0; measuring the high arm from the first stop (1.1 - 1) would give 0.5000000000000004 and round to 1.
        var scale = ChartColorScale.Diverging(ChartColor.FromHex("#000000"), ChartColor.FromHex("#000000"), ChartColor.FromHex("#050000"), 0);
        Assert.Equal("#000000", scale.ColorFor(1, 0, 10).ToHex());
    }

    [Theory]
    [InlineData(110, 6)]
    [InlineData(60, 0)]
    public void Legend_WhenTheMidpointSitsAtAnEnd_SpacesSwatchesEvenlyAndLabelsThatEnd(double midpoint, int labelledStep) {
        var chart = Map("tile-map", ChartColorScale.Diverging(Low, Neutral, High, midpoint).WithValueRange(60, 110).WithLabels("60", "Target", "110"));
        Assert.Equal(midpoint == 60 ? Neutral : Low[0], chart.Options.MapColorScale!.ColorFor(0, 0, 10));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions()));
        var steps = ByRole(svg, "map-scale-step");
        Assert.Equal(7, steps.Length);
        Assert.Equal(7, ByRole(svg, "map-scale-step-source").Select(step => (string?)step.Attribute("data-cfx-value")).Distinct().Count());
        var step = steps[labelledStep];
        var caption = Assert.Single(prepared.Regions, region => region.Role == "map-scale-midpoint-label").Bounds;
        Assert.InRange(Number(step, "x") + Number(step, "width") / 2, caption.Left, caption.Right);
        Assert.Equal("#F1F5F9", (string?)step.Attribute("fill"));
    }

    [Theory]
    [InlineData(0, -10, 30, 1, false)]
    [InlineData(5, -10, 30, 1.5, false)]
    [InlineData(-20, 0, 40, 0, false)]
    [InlineData(60, 0, 40, 4, false)]
    [InlineData(0, -10, 30, 2.5, true)]
    public void SampledLegend_MidpointCaptionTracksItsValueBetweenSwatchCenters(double midpoint, double min, double max,
        double midpointStep, bool longRamp) {
        var scale = longRamp ? ChartColorScale.Diverging(Low, Neutral, Enumerable.Repeat(High[2], 9), midpoint)
            : ChartColorScale.Diverging(Low[0], Neutral, High[2], midpoint);
        var chart = Map("tile-map", scale.WithValueRange(min, max).WithLabels(null, "Target", null));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var svg = XDocument.Parse(prepared.ToSvg());
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "map-scale-step").ToArray();
        Assert.Equal(longRamp ? 11 : 5, swatches.Length);
        var center = swatches[0].Bounds.Left + (midpointStep + .5) * swatches[0].Bounds.Width;
        var caption = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == "map-scale-midpoint");
        Assert.Equal(center, caption.X, 6);
        var captionRegion = prepared.Regions.Single(region => region.Role == "map-scale-midpoint-label");
        Assert.Contains("Target", captionRegion.Label, StringComparison.Ordinal);
        var values = ByRole(svg, "map-scale-step-source").Select(step => Number(step, "data-cfx-value")).ToArray();
        var image = ChartForgeX.Raster.RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        for (var index = 0; index < swatches.Length; index++) {
            var box = swatches[index].Bounds;
            var pixel = ((int)(box.Top + box.Height / 2) * image.Width + (int)(box.Left + box.Width / 2)) * 4;
            Assert.Equal(scale.ColorFor(values[index], min, max), ChartColor.FromRgba(image.Pixels[pixel], image.Pixels[pixel + 1], image.Pixels[pixel + 2], image.Pixels[pixel + 3]));
        }
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(10, 4, false)]
    [InlineData(0, 0, true)]
    [InlineData(10, 4, true)]
    public void CompactEndpointCaption_ElidesWithinTheKeyWithoutMovingItsSwatchAnchor(double midpoint, int midpointStep, bool multiStop) {
        const string family = "CFX Map Edge Carlito";
        FontRegistry.Register(family, Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        var scale = (multiStop ? ChartColorScale.Diverging(new[] { Low[0] }, Neutral, High, midpoint)
            : ChartColorScale.Diverging(Low[0], Neutral, High[2], midpoint)).WithValueRange(0, 10).WithLabels(null, "Target temperature", null);
        var chart = Map("tile-map", scale).WithSize(360, 360).WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Bottom);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 360)),
            VisualTheme.Graphite(), VisualThemeMode.Light, new VisualFrame(), FontSpec.FromFamily(family)));
        var document = XDocument.Parse(prepared.ToSvg());
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "map-scale-step").ToArray();
        var target = swatches[midpointStep].Bounds;
        var center = target.Left + target.Width / 2;
        var caption = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == "map-scale-midpoint");
        var region = prepared.Regions.Single(item => item.Id == caption.Id);
        var svgCaption = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-source-id") == caption.Id)
            .Descendants().Single(element => element.Name.LocalName == "text");
        var full = "Target temperature " + midpoint.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(full, region.Label);
        Assert.NotEmpty(svgCaption.Value);
        Assert.NotEqual(full, svgCaption.Value);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        Assert.Equal(center, caption.X, 6);
        Assert.InRange(Math.Abs(Number(svgCaption, "x") + caption.Text.Metrics.Width / 2 - center), 0, .001);
        Assert.True(region.Bounds.Left >= swatches[0].Bounds.Left - .000001);
        Assert.True(region.Bounds.Right <= swatches[swatches.Length - 1].Bounds.Right + .000001);
        Assert.True(caption.Text.Metrics.Width <= region.Bounds.Width);
        Assert.True(Number(svgCaption, "x") >= region.Bounds.Left - .001);
        Assert.True(Number(svgCaption, "x") + caption.Text.Metrics.Width <= region.Bounds.Right + .001);
        var values = ByRole(document, "map-scale-step-source").Select(step => Number(step, "data-cfx-value")).ToArray();
        var image = ChartForgeX.Raster.RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        for (var index = 0; index < swatches.Length; index++) {
            var box = swatches[index].Bounds;
            var pixel = ((int)(box.Top + box.Height / 2) * image.Width + (int)(box.Left + box.Width / 2)) * 4;
            Assert.Equal(scale.ColorFor(values[index], 0, 10), ChartColor.FromRgba(image.Pixels[pixel], image.Pixels[pixel + 1], image.Pixels[pixel + 2], image.Pixels[pixel + 3]));
        }
    }

    [Fact]
    public void MidpointAnchorsKeepConstantSequentialAndHugeFiniteDomainContracts() {
        var three = ChartColorScale.Diverging(Low[0], Neutral, High[2], double.MaxValue / 2);
        Assert.Equal(3, ChartColorScaleLegend.MidpointStep(three, -double.MaxValue, double.MaxValue, 5), 6);
        Assert.Equal(2, ChartColorScaleLegend.MidpointStep(three, 7, 7, 5));
        Assert.Equal(0, ChartColorScaleLegend.MidpointStep(ChartColorScale.Diverging(Low, Neutral, High), 7, 7, 7));
        Assert.Equal(2, ChartColorScaleLegend.MidpointStep(ChartColorScale.Sequential(new[] { Low[0], Low[1], High[0], High[1] }), 0, 10, 4));
    }

    [Fact]
    public void Sequential_WithStops_SpreadsThemEvenly() {
        var scale = ChartColorScale.Sequential(new[] { ChartColor.FromHex("#000000"), ChartColor.FromHex("#FF0000"), ChartColor.FromHex("#FFFFFF") });
        Assert.Null(scale.MidpointColor);
        Assert.Equal("#FF0000", scale.ColorFor(5, 0, 10).ToHex());
        Assert.Equal("#800000", scale.ColorFor(2.5, 0, 10).ToHex());
        Assert.Equal(ChartColorScale.Sequential(ChartColor.White, ChartColor.Black).ColorFor(4, 0, 10), ChartColorScale.Sequential(new[] { ChartColor.White, ChartColor.Black }).ColorFor(4, 0, 10));
    }

    [Fact]
    public void Stops_AreValidated() {
        Assert.Throws<ArgumentException>(() => ChartColorScale.Sequential(new[] { ChartColor.White }));
        Assert.Throws<ArgumentNullException>(() => ChartColorScale.Sequential((IEnumerable<ChartColor>)null!));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Diverging(Array.Empty<ChartColor>(), Neutral, High));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Diverging(Low, Neutral, Array.Empty<ChartColor>()));
        var source = new List<ChartColor>(Low);
        var scale = ChartColorScale.Sequential(source);
        source.Clear();
        Assert.Equal(3, scale.Colors.Count);
    }

    [Theory]
    [InlineData("tile-map")]
    [InlineData("region-map")]
    public void Legend_ShowsOneSwatchPerStopAndPutsTheMidpointLabelUnderTheMidpointStop(string role) {
        var chart = Map(role, ChartColorScale.Diverging(Low, Neutral, High, 2).WithValueRange(-4, 8).WithLabels("-4", "2", "8"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions()));
        var steps = ByRole(svg, "map-scale-step");
        Assert.Equal(new[] { "#7F1D1D", "#DC2626", "#FCA5A5", "#F1F5F9", "#93C5FD", "#2563EB", "#1E3A8A" }, steps.Select(step => (string?)step.Attribute("fill")).ToArray());
        Assert.Equal(new[] { "-4", "-2", "0", "2", "4", "6", "8" }, ByRole(svg, "map-scale-step-source").Select(step => (string?)step.Attribute("data-cfx-value")).ToArray());

        var midpointStep = steps[3];
        var centre = Number(midpointStep, "x") + Number(midpointStep, "width") / 2;
        var caption = Assert.Single(prepared.Regions, region => region.Role == "map-scale-midpoint-label").Bounds;
        Assert.InRange(centre, caption.Left, caption.Right);
        Assert.NotEqual(Map(role, ChartColorScale.Diverging(Low[0], Neutral, High[2], 2).WithValueRange(-4, 8)).ToPng(), chart.ToPng());
    }

    [Fact]
    public void Legend_KeepsFiveSwatchesForThreeColoursAndSamplesVeryLongRamps() {
        Assert.Equal(5, ByRole(Literal(Map("tile-map", ChartColorScale.Diverging(Low[0], Neutral, High[2]))), "map-scale-step").Length);
        var ramp = Enumerable.Range(0, 15).Select(i => ChartColor.FromRgb((byte)(i * 17), 40, 120)).ToArray();
        var steps = ByRole(Literal(Map("tile-map", ChartColorScale.Sequential(ramp))), "map-scale-step");
        Assert.Equal(11, steps.Length);
        Assert.Equal(ramp[0].ToHex(), (string?)steps[0].Attribute("fill"));
        Assert.Equal(ramp[14].ToHex(), (string?)steps[10].Attribute("fill"));
    }

    private static Chart Map(string role, ChartColorScale scale) {
        var regions = new[] { new ChartRegionMapItem("CA", 0), new ChartRegionMapItem("NY", 2), new ChartRegionMapItem("TX", 8) };
        var chart = Chart.Create().WithSize(760, 420).WithMapLabels(false).WithMapColorScale(scale);
        return role == "tile-map"
            ? chart.AddTileMap("Change", ChartTileMapCatalog.Get("us-states"), regions)
            : chart.AddRegionMap("Change", ChartMapCatalog.Get("us-states"), regions);
    }

    private static double Number(XElement element, string attribute) => double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);
    private static XDocument Literal(Chart chart) => XDocument.Parse(chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg(new VisualSvgOptions()));

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
