using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Finite numeric dimensions keep their exact endpoints and midpoint at any magnitude.</summary>
public sealed class ColorScaleNumericTests {
    [Fact]
    public void Continuous_TinyDomain_KeepsItsMidpointAndEndpointColors() {
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White).WithValueRange(0, 2e-7);
        Assert.Equal("#808080", scale.ColorFor(1e-7, 0, 2e-7).ToHex());
        Assert.Equal(ChartColor.White, scale.ColorFor(2e-7, 0, 2e-7));
        Assert.Equal(1e-7, scale.EffectiveMidpoint(0, 2e-7));
    }

    [Fact]
    public void Continuous_HugeFiniteDomain_KeepsItsMidpointAndEndpointColors() {
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White).WithValueRange(-double.MaxValue, double.MaxValue);
        Assert.Equal(ChartColor.Black, scale.ColorFor(-double.MaxValue, -double.MaxValue, double.MaxValue));
        Assert.Equal("#808080", scale.ColorFor(0, -double.MaxValue, double.MaxValue).ToHex());
        Assert.Equal(ChartColor.White, scale.ColorFor(double.MaxValue, -double.MaxValue, double.MaxValue));
        Assert.Equal(0, scale.EffectiveMidpoint(-double.MaxValue, double.MaxValue));
    }

    [Fact]
    public void Diverging_HugeSameSignBounds_KeepTheirFiniteMidpoint() {
        var minimum = double.MaxValue * .5;
        var midpoint = double.MaxValue * .75;
        var scale = ChartColorScale.Diverging(ChartColor.Black, ChartColor.FromHex("#FF0000"), ChartColor.White).WithValueRange(minimum, double.MaxValue);
        Assert.Equal(midpoint, scale.EffectiveMidpoint(minimum, double.MaxValue));
        Assert.Equal(ChartColor.FromHex("#FF0000"), scale.ColorFor(midpoint));
        Assert.Equal(ChartColor.White, scale.ColorFor(double.MaxValue));
    }

    [Fact]
    public void Continuous_SourceDomain_MustBeFiniteAndOrderedAndFixedDomainCallsMustHaveBounds() {
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White);
        Assert.Throws<InvalidOperationException>(() => scale.ColorFor(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.ColorFor(double.NaN, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.ColorFor(1, double.NegativeInfinity, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.ColorFor(1, 0, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.ColorFor(1, 10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(1, 1));
        Assert.Throws<InvalidOperationException>(() => scale.WithMidpoint(1));
        Assert.Equal(ChartColorScaleMode.Sequential, scale.Mode);
        Assert.Empty(scale.Bands);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MapLegend_TinyDomain_PreservesObservedValuesAndStopColors(bool diverging) {
        var scale = diverging ? ChartColorScale.Diverging(ChartColor.Black, ChartColor.FromHex("#FF0000"), ChartColor.White)
            .WithLabels(null, "Center", null) : ChartColorScale.Sequential(ChartColor.Black, ChartColor.White);
        var chart = Map(scale, 0, 1e-7, 2e-7);
        var document = XDocument.Parse(chart.ToSvg());
        var legend = Role(document, "map-scale").Single();
        Assert.Equal("0", (string?)legend.Attribute("data-cfx-min-value"));
        Assert.Equal("2E-07", (string?)legend.Attribute("data-cfx-max-value"));
        Assert.Equal("1E-07", (string?)legend.Attribute("data-cfx-midpoint-value"));
        var steps = Role(document, "map-scale-step").ToArray();
        Assert.Equal("#000000", (string?)steps[0].Attribute("fill"));
        Assert.Equal(diverging ? "#FF0000" : "#808080", (string?)steps[2].Attribute("fill"));
        Assert.Equal("#FFFFFF", (string?)steps[4].Attribute("fill"));
        Assert.Contains("2E-07", string.Join(" ", Role(document, "map-scale-label").Select(element => element.Value)), StringComparison.Ordinal);
    }

    [Fact]
    public void MapLegend_ConstantDomain_UsesOneObservedValueAndTheLowPaint() {
        var chart = Map(ChartColorScale.Diverging(ChartColor.Black, ChartColor.FromHex("#FF0000"), ChartColor.White), 12, 12, 12);
        var document = XDocument.Parse(chart.ToSvg());
        var legend = Role(document, "map-scale").Single();
        foreach (var attribute in new[] { "data-cfx-min-value", "data-cfx-max-value", "data-cfx-midpoint-value" })
            Assert.Equal("12", (string?)legend.Attribute(attribute));
        Assert.All(Role(document, "map-scale-step-source"), step => Assert.Equal("12", (string?)step.Attribute("data-cfx-value")));
        Assert.All(Role(document, "map-scale-step"), step => Assert.Equal("#000000", (string?)step.Attribute("fill")));
    }

    [Fact]
    public void MapLegend_ExtremeFiniteDomain_KeepsFiniteSampledStops() {
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White).WithValueRange(-double.MaxValue, double.MaxValue);
        var document = XDocument.Parse(Map(scale, 0, 1, 2).ToSvg());
        var values = Role(document, "map-scale-step-source").Select(step => double.Parse((string)step.Attribute("data-cfx-value")!, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(-double.MaxValue, values.First());
        Assert.Equal(0, values[2]);
        Assert.Equal(double.MaxValue, values.Last());
        Assert.All(values, value => Assert.True(double.IsFinite(value)));
        Assert.Equal("#808080", (string?)Role(document, "map-scale-step").ElementAt(2).Attribute("fill"));
    }

    private static Chart Map(ChartColorScale scale, double low, double midpoint, double high) => Chart.Create()
        .WithSize(760, 420).WithMapLabels(false).WithMapColorScale(scale)
        .AddTileMap("Dimension", ChartTileMapCatalog.Get("us-states"), new[] {
            new ChartRegionMapItem("CA", low), new ChartRegionMapItem("NY", midpoint), new ChartRegionMapItem("TX", high)
        });

    private static IEnumerable<XElement> Role(XDocument document, string role) =>
        document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role);
}
