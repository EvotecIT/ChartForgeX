using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Region presentation stays immutable, clipped, source-descriptive and shared by native SVG/PNG.</summary>
public sealed class CartesianPlotRegionDepthTests {
    [Theory]
    [InlineData(360, false)] [InlineData(360, true)] [InlineData(800, false)] [InlineData(800, true)]
    public void CompleteRecipeKeepsBorderCaptionAndPivotGeometryInBothNativeExports(int width, bool dark) {
        var chart = QuadrantRegionDepthRecipe.Create(width == 360 ? "compact-pivot-quadrants" : "pivot-quadrants", dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var prepared = CartesianAxisRuleCrossingTests.Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Capture(svg, png, "quadrant-region-depth-" + width + "-" + dark);
        Assert.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "plot-region-border"));
        Assert.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "plot-region-label"));
        Assert.Equal(6, prepared.Regions.Count(region => region.Role == "point"));
        Assert.DoesNotContain(prepared.Diagnostics, item => item.Code == "cartesian.plot-region-label-overflow");
        var decoded = RasterImageDecoder.Decode(png); Assert.Equal(width, decoded.Width); Assert.Equal(440, decoded.Height);
        var document = XDocument.Parse(svg);
        Assert.Equal(4, document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "plot-region-border"));
        Assert.Equal(new[] { "topleft", "topright", "bottomleft", "bottomright" }, document.Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "plot-region").Select(node => (string?)node.Attribute("data-cfx-label-position")));
        var firstSvg = svg; var firstPng = png; chart.PlotRegions.Clear(); chart.Options.XAxis.RuleCrossAt = 0;
        Assert.Equal(firstSvg, prepared.ToSvg()); Assert.Equal(firstPng, prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartPlotRegionLabelPosition.Center, .5, .5)]
    [InlineData(ChartPlotRegionLabelPosition.TopLeft, 0, 0)] [InlineData(ChartPlotRegionLabelPosition.Top, .5, 0)] [InlineData(ChartPlotRegionLabelPosition.TopRight, 1, 0)]
    [InlineData(ChartPlotRegionLabelPosition.Left, 0, .5)] [InlineData(ChartPlotRegionLabelPosition.Right, 1, .5)]
    [InlineData(ChartPlotRegionLabelPosition.BottomLeft, 0, 1)] [InlineData(ChartPlotRegionLabelPosition.Bottom, .5, 1)] [InlineData(ChartPlotRegionLabelPosition.BottomRight, 1, 1)]
    public void ExplicitScreenAnchorsStayDeliberateAfterClippingAndReversal(ChartPlotRegionLabelPosition position, double x, double y) {
        var chart = CartesianAxisRuleCrossingTests.Fixture(false).WithAxes(false); chart.ClearSeries();
        chart.Options.XAxis.Reversed = true; chart.Options.YAxis.Reversed = true;
        chart.AddPlotRegion(new ChartPlotRegion(1, xMinimum: -10, xMaximum: 6, yMinimum: 2, yMaximum: 30, label: "Zone")
            .WithBorder(new ChartStrokeStyle(width: 3)).WithLabelPosition(position));
        var prepared = CartesianAxisRuleCrossingTests.Prepare(chart);
        var region = Assert.Single(prepared.Regions, item => item.Role == "plot-region").Bounds;
        var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "plot-region-label-backplate").Bounds;
        var inside = new ChartRect(region.Left + 3, region.Top + 3, region.Width - 6, region.Height - 6);
        Assert.Equal(inside.Left + (inside.Width - plate.Width) * x, plate.Left, 7);
        Assert.Equal(inside.Top + (inside.Height - plate.Height) * y, plate.Top, 7);
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "plot-region");
        Assert.Equal("-10", group.Metadata["data-cfx-x-minimum"]); Assert.Equal("30", group.Metadata["data-cfx-y-maximum"]);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BorderedSecondaryRegionRetainsItsSelectedLogarithmicYDomainAndReversal(bool reversed) {
        var chart = CartesianAxisRuleCrossingTests.Fixture(false).WithAxes(false);
        chart.AddLine("Secondary", new[] { new ChartPoint(2, 200), new ChartPoint(8, 8000) }); chart.Series[1].UseSecondaryValueAxis();
        chart.Options.SecondaryYAxis.WithLogarithmicScale().WithBounds(100, 10000).WithReversal(reversed);
        chart.AddPlotRegion(new ChartPlotRegion(1, xMinimum: 2, xMaximum: 8, yMinimum: 1000, yMaximum: 10000,
            yAxis: ChartAxisSide.Secondary, showLabel: false).WithBorder(new ChartStrokeStyle(width: 4)));
        var prepared = CartesianAxisRuleCrossingTests.Prepare(chart);
        var plot = Assert.Single(prepared.Regions, item => item.Role == "cartesian-plot").Bounds;
        var bounds = Assert.Single(prepared.Regions, item => item.Role == "plot-region").Bounds;
        Assert.Equal(plot.Height / 2, bounds.Height, 7); Assert.Equal(plot.Top + (reversed ? plot.Height / 2 : 0), bounds.Top, 7);
        var border = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "plot-region-border");
        Assert.Equal(bounds.Left + 2, new ChartPath(border.Commands).Bounds().Left, 7);
        var raw = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "plot-region");
        Assert.Equal("secondary", raw.Metadata["data-cfx-y-axis"]); Assert.Equal("1000", raw.Metadata["data-cfx-y-minimum"]);
    }

    [Fact]
    public void BorderOverridesUseTheNativeStrokeOwnerAndNullRestoresDefaultBytes() {
        var plain = new ChartPlotRegion(0, label: "Zone"); var dash = new[] { 5d, 2d };
        var styled = plain.WithBorder(new ChartStrokeStyle(ChartColor.FromRgb(220, 20, 30), 4, .5, dash)).WithLabelPosition(ChartPlotRegionLabelPosition.Center);
        dash[0] = 100;
        Assert.Null(plain.Border); Assert.Equal(ChartPlotRegionLabelPosition.Auto, plain.LabelPosition);
        Assert.Equal(5, styled.Border!.Dash![0]); Assert.Equal(ChartPlotRegionLabelPosition.Center, styled.LabelPosition);
        var chart = CartesianAxisRuleCrossingTests.Fixture(false).WithAxes(false); chart.ClearSeries(); chart.AddPlotRegion(styled);
        var prepared = CartesianAxisRuleCrossingTests.Prepare(chart); var border = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "plot-region-border");
        Assert.Equal(4, border.StrokeWidth); Assert.Equal(new[] { 5d, 2d }, border.Dash); Assert.Equal(128, border.Stroke!.Value.A);
        Assert.Equal(ChartColor.FromRgb(220, 20, 30).R, border.Stroke.Value.R);
        var bounds = Assert.Single(prepared.Regions, node => node.Role == "plot-region").Bounds; var painted = new ChartPath(border.Commands).Bounds();
        Assert.True(painted.Left - 2 >= bounds.Left && painted.Right + 2 <= bounds.Right);
        chart.PlotRegions[0] = plain;
        var expected = CartesianAxisRuleCrossingTests.Prepare(chart); chart.PlotRegions[0] = styled.WithBorder(null).WithLabelPosition(ChartPlotRegionLabelPosition.Auto);
        Assert.Equal(expected.ToSvg(), CartesianAxisRuleCrossingTests.Prepare(chart).ToSvg());
        Assert.Equal(expected.ToPng(), CartesianAxisRuleCrossingTests.Prepare(chart).ToPng());
        chart.PlotRegions[0] = plain.WithBorder(new ChartStrokeStyle(width: 0));
        Assert.DoesNotContain(CartesianAxisRuleCrossingTests.Prepare(chart).Scene.Nodes, node => node.Role == "plot-region-border");
        Assert.Throws<ArgumentOutOfRangeException>(() => plain.WithLabelPosition((ChartPlotRegionLabelPosition)99));
    }

    [Fact]
    public void SolidBorderChangesNativeBoundaryPixelsWithoutSpillingOutsideTheVisibleIntersection() {
        var chart = CartesianAxisRuleCrossingTests.Fixture(false).WithAxes(false); chart.ClearSeries();
        var source = new ChartPlotRegion(0, xMinimum: -2, xMaximum: 5, yMinimum: 2, yMaximum: 8, opacity: 0, showLabel: false);
        chart.AddPlotRegion(source);
        var plain = CartesianAxisRuleCrossingTests.Prepare(chart); var bounds = Assert.Single(plain.Regions, item => item.Role == "plot-region").Bounds;
        chart.PlotRegions[0] = source.WithBorder(new ChartStrokeStyle(ChartColor.FromRgb(220, 20, 30), 6));
        var bordered = CartesianAxisRuleCrossingTests.Prepare(chart);
        var control = RasterImageDecoder.Decode(plain.ToPng()); var actual = RasterImageDecoder.Decode(bordered.ToPng());
        var y = (int)(bounds.Top + bounds.Height / 2); var inside = (y * actual.Width + (int)bounds.Left + 1) * 4;
        var outside = (y * actual.Width + (int)bounds.Left - 2) * 4;
        Assert.False(control.Pixels.Skip(inside).Take(4).SequenceEqual(actual.Pixels.Skip(inside).Take(4)));
        Assert.Equal(control.Pixels.Skip(outside).Take(4), actual.Pixels.Skip(outside).Take(4));
        Assert.Equal(220, actual.Pixels[inside]); Assert.Equal(20, actual.Pixels[inside + 1]); Assert.Equal(30, actual.Pixels[inside + 2]);
        Assert.Equal(bounds, Assert.Single(bordered.Regions, item => item.Role == "plot-region").Bounds);
    }

    [Fact]
    public void EmptyRegionsRetainDescriptionsWithoutPaintingAClampedBorder() {
        var chart = CartesianAxisRuleCrossingTests.Fixture(false).WithAxes(false); chart.ClearSeries();
        chart.AddPlotRegion(new ChartPlotRegion(0, xMinimum: 20, xMaximum: 30, label: "Outside").WithBorder(new ChartStrokeStyle(width: 4)));
        var empty = CartesianAxisRuleCrossingTests.Prepare(chart);
        Assert.DoesNotContain(empty.Scene.Nodes, node => node.Role is "plot-region-border" or "plot-region-fill" or "plot-region-label");
        Assert.Contains("Outside", empty.ToSvg());
    }

    [Fact]
    public void ExplicitAnchorDoesNotRelocateAcrossAConflictingMarkWhileAutoCanUseAnotherCandidate() {
        var chart = CartesianAxisRuleCrossingTests.Fixture(false).WithAxes(false); chart.ClearSeries();
        chart.AddScatter("Corner", new[] { new ChartPoint(0, 10) }); chart.Series[0].WithMarkerRadius(40);
        var region = new ChartPlotRegion(1, label: "Caption");
        chart.AddPlotRegion(region.WithLabelPosition(ChartPlotRegionLabelPosition.TopLeft));
        var explicitAnchor = CartesianAxisRuleCrossingTests.Prepare(chart);
        Assert.DoesNotContain(explicitAnchor.Scene.Nodes, node => node.Role == "plot-region-label");
        Assert.Contains(explicitAnchor.Diagnostics, diagnostic => diagnostic.Code == "cartesian.plot-region-label-overflow");
        chart.PlotRegions[0] = region;
        Assert.Single(CartesianAxisRuleCrossingTests.Prepare(chart).Scene.Nodes, node => node.Role == "plot-region-label");
    }

    [Fact]
    public void MermaidAdapterOptsIntoSharedRulesBordersAndCaptionsWithoutChangingPointFacts() {
        var document = new MermaidParser().ParseQuadrant("quadrantChart\nquadrant-1 Plan\nObserved: [0.2, 0.8]").Document!;
        var chart = document.ToChart(new MermaidQuadrantRenderOptions { ShowPivotRules = true,
            RegionBorder = new ChartStrokeStyle(width: 2), RegionLabelPosition = ChartPlotRegionLabelPosition.Top });
        Assert.Equal(.5, chart.Options.XAxis.RuleCrossAt); Assert.Equal(.5, chart.Options.YAxis.RuleCrossAt);
        Assert.All(chart.PlotRegions, region => { Assert.Equal(ChartPlotRegionLabelPosition.Top, region.LabelPosition); Assert.Equal(2, region.Border!.Width); });
        Assert.Equal(new ChartPoint(.2, .8), Assert.Single(chart.Series[0].Points));
        Assert.Equal(4, CartesianAxisRuleCrossingTests.Prepare(chart).Scene.Nodes.Count(node => node.Role == "plot-region-border"));
    }

    private static void Capture(string svg, byte[] png, string name) {
        var path = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(path)) return;
        Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(path, name + ".png"), png);
    }
}
