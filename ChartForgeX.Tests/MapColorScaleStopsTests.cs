using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Map colour scales with more than three stops keep every step on the map and in the legend.</summary>
public sealed class MapColorScaleStopsTests {
    private static readonly ChartColor[] Low = { ChartColor.FromHex("#7F1D1D"), ChartColor.FromHex("#DC2626"), ChartColor.FromHex("#FCA5A5") };
    private static readonly ChartColor Neutral = ChartColor.FromHex("#F1F5F9");
    private static readonly ChartColor[] High = { ChartColor.FromHex("#93C5FD"), ChartColor.FromHex("#2563EB"), ChartColor.FromHex("#1E3A8A") };

    [Fact]
    public void Diverging_WithArms_DrawsEveryStopAtItsValueAroundAnOffCentreMidpoint() {
        var scale = ChartMapColorScale.Diverging(Low, Neutral, High, midpointValue: 2).WithValueRange(-4, 8);
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
        var classic = ChartMapColorScale.Diverging(Low[0], Neutral, High[2], 3);
        var arms = ChartMapColorScale.Diverging(new[] { Low[0] }, Neutral, new[] { High[2] }, 3);
        for (var value = 0.0; value <= 10; value += 0.5) Assert.Equal(classic.ColorFor(value, 0, 10), arms.ColorFor(value, 0, 10));
    }

    [Fact]
    public void ThreeColourDiverging_HighArm_RoundsLikeADirectBlend() {
        // 5 * 0.1 is 0.5 and rounds to 0; measuring the high arm from the first stop (1.1 - 1) would give 0.5000000000000004 and round to 1.
        var scale = ChartMapColorScale.Diverging(ChartColor.FromHex("#000000"), ChartColor.FromHex("#000000"), ChartColor.FromHex("#050000"), 0);
        Assert.Equal("#000000", scale.ColorFor(1, 0, 10).ToHex());
    }

    [Theory]
    [InlineData(110, 6)]
    [InlineData(60, 0)]
    public void Legend_WhenTheMidpointSitsAtAnEnd_SpacesSwatchesEvenlyAndLabelsThatEnd(double midpoint, int labelledStep) {
        var chart = Map("tile-map", ChartMapColorScale.Diverging(Low, Neutral, High, midpoint).WithValueRange(60, 110).WithLabels("60", "Target", "110"));
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

    [Fact]
    public void Sequential_WithStops_SpreadsThemEvenly() {
        var scale = ChartMapColorScale.Sequential(new[] { ChartColor.FromHex("#000000"), ChartColor.FromHex("#FF0000"), ChartColor.FromHex("#FFFFFF") });
        Assert.Null(scale.MidpointColor);
        Assert.Equal("#FF0000", scale.ColorFor(5, 0, 10).ToHex());
        Assert.Equal("#800000", scale.ColorFor(2.5, 0, 10).ToHex());
        Assert.Equal(ChartMapColorScale.Sequential(ChartColor.White, ChartColor.Black).ColorFor(4, 0, 10), ChartMapColorScale.Sequential(new[] { ChartColor.White, ChartColor.Black }).ColorFor(4, 0, 10));
    }

    [Fact]
    public void Stops_AreValidated() {
        Assert.Throws<ArgumentException>(() => ChartMapColorScale.Sequential(new[] { ChartColor.White }));
        Assert.Throws<ArgumentNullException>(() => ChartMapColorScale.Sequential((IEnumerable<ChartColor>)null!));
        Assert.Throws<ArgumentException>(() => ChartMapColorScale.Diverging(Array.Empty<ChartColor>(), Neutral, High));
        Assert.Throws<ArgumentException>(() => ChartMapColorScale.Diverging(Low, Neutral, Array.Empty<ChartColor>()));
        var source = new List<ChartColor>(Low);
        var scale = ChartMapColorScale.Sequential(source);
        source.Clear();
        Assert.Equal(3, scale.Colors.Count);
    }

    [Theory]
    [InlineData("tile-map")]
    [InlineData("region-map")]
    public void Legend_ShowsOneSwatchPerStopAndPutsTheMidpointLabelUnderTheMidpointStop(string role) {
        var chart = Map(role, ChartMapColorScale.Diverging(Low, Neutral, High, 2).WithValueRange(-4, 8).WithLabels("-4", "2", "8"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions()));
        var steps = ByRole(svg, "map-scale-step");
        Assert.Equal(new[] { "#7F1D1D", "#DC2626", "#FCA5A5", "#F1F5F9", "#93C5FD", "#2563EB", "#1E3A8A" }, steps.Select(step => (string?)step.Attribute("fill")).ToArray());
        Assert.Equal(new[] { "-4", "-2", "0", "2", "4", "6", "8" }, ByRole(svg, "map-scale-step-source").Select(step => (string?)step.Attribute("data-cfx-value")).ToArray());

        var midpointStep = steps[3];
        var centre = Number(midpointStep, "x") + Number(midpointStep, "width") / 2;
        var caption = Assert.Single(prepared.Regions, region => region.Role == "map-scale-midpoint-label").Bounds;
        Assert.InRange(centre, caption.Left, caption.Right);
        Assert.NotEqual(Map(role, ChartMapColorScale.Diverging(Low[0], Neutral, High[2], 2).WithValueRange(-4, 8)).ToPng(), chart.ToPng());
    }

    [Fact]
    public void Legend_KeepsFiveSwatchesForThreeColoursAndSamplesVeryLongRamps() {
        Assert.Equal(5, ByRole(Literal(Map("tile-map", ChartMapColorScale.Diverging(Low[0], Neutral, High[2]))), "map-scale-step").Length);
        var ramp = Enumerable.Range(0, 15).Select(i => ChartColor.FromRgb((byte)(i * 17), 40, 120)).ToArray();
        var steps = ByRole(Literal(Map("tile-map", ChartMapColorScale.Sequential(ramp))), "map-scale-step");
        Assert.Equal(11, steps.Length);
        Assert.Equal(ramp[0].ToHex(), (string?)steps[0].Attribute("fill"));
        Assert.Equal(ramp[14].ToHex(), (string?)steps[10].Attribute("fill"));
    }

    private static Chart Map(string role, ChartMapColorScale scale) {
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
