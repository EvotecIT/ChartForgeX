using System.Globalization;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WardleyOverlayTests {
    [Theory]
    [InlineData("build")] [InlineData("buy")] [InlineData("outsource")] [InlineData("market")]
    public void SourcingStrategiesHaveDistinctNativeMarksAndInertiaClearsTheirOuterFootprint(string strategy) {
        var map = WardleyMapBlock.Create().WithSize(600, 400); var node = map.AddNode("api", "API", .6, .4); node.Strategy = strategy; node.Inertia = true;
        var svg = map.ToSvg(); var png = map.ToPng(); Capture(map, "wardley-strategy-" + strategy); var xml = XDocument.Parse(svg);
        Assert.Single(Roles(xml, "wardley-sourcing-" + strategy));
        Assert.Equal(strategy == "market" ? 3 : 0, Roles(xml, "wardley-market-member").Length);
        var outer = Assert.Single(Roles(xml, "wardley-strategy")); var inertia = Assert.Single(Roles(xml, "wardley-inertia"));
        Assert.True(N(inertia, "x1") - N(inertia, "stroke-width") / 2 > N(outer, "cx") + N(outer, "r"));
        var caption = xml.Descendants().Single(e => e.Name.LocalName == "text" && e.Value == "API");
        Assert.True(N(caption, "x") > N(inertia, "x1") + N(inertia, "stroke-width") / 2);
        Assert.NotEmpty(png); Assert.Equal(strategy, node.Strategy); Assert.Equal("api", node.Id);
    }

    [Fact]
    public void AnnotationBoxKeepsEveryRecordAndStableNumericOrderWithoutTinyFontOrClippedText() {
        var map = WardleyMapBlock.Create().WithSize(640, 480).WithAnnotationsBox(0, 1);
        map.AddNode("api", "API", .8, .2);
        map.AddAnnotation(7, "Second authored fact", .8, .6); map.AddAnnotation(2, "First authored fact", .6, .3); map.AddAnnotation(7, "Third authored fact", .7, .7);
        var svg = map.ToSvg(); var png = map.ToPng(); Capture(map, "wardley-box-order"); var xml = XDocument.Parse(svg);
        var legend = Assert.Single(Roles(xml, "wardley-annotation-box")); var rect = legend.Elements().Single(e => e.Name.LocalName == "rect");
        var plot = Assert.Single(Roles(xml, "wardley-plot")); var entries = Roles(xml, "wardley-annotation-legend-entry");
        Assert.Equal(new[] { "2", "7", "7" }, entries.Select(e => (string?)e.Attribute("data-annotation-number")));
        Assert.Equal(new[] { "First authored fact", "Second authored fact", "Third authored fact" }, entries.Select(e => e.Elements().Single(t => t.Name.LocalName == "title").Value));
        Assert.Equal(3, Roles(xml, "wardley-annotation").Length); Assert.Equal(new[] { 7, 2, 7 }, map.Annotations.Select(a => a.Number));
        Assert.True(N(rect, "x") >= N(plot, "x")); Assert.True(N(rect, "y") >= N(plot, "y"));
        Assert.True(N(rect, "x") + N(rect, "width") <= N(plot, "x") + N(plot, "width") + .001);
        Assert.True(N(rect, "y") + N(rect, "height") <= N(plot, "y") + N(plot, "height") + .001);
        Assert.All(Roles(xml, "wardley-annotation-legend-text"), text => Assert.True(N(text, "font-size") >= 10));
        Assert.NotEmpty(png); Assert.Equal(.0, map.AnnotationsBox!.Visibility); Assert.Equal(1, map.AnnotationsBox.Evolution);
    }

    [Fact]
    public void AnnotationOverflowFailsExplicitlyAndClearingOptionsRestoresOrdinaryBytes() {
        var map = WardleyMapBlock.Create().WithSize(500, 320); map.AddNode("api", "API", .8, .2); map.AddAnnotation(1, "Ordinary note", .5, .5);
        var svg = map.ToSvg(); var png = map.ToPng();
        map.WithStyle(new WardleyMapStyle(annotationFill: ChartColor.Parse("#D1FAE5"))).WithAnnotationsBox(.2, .2);
        Assert.NotEqual(svg, map.ToSvg()); map.WithoutAnnotationsBox().WithStyle(null);
        Assert.Equal(svg, map.ToSvg()); Assert.Equal(png, map.ToPng());
        for (var index = 0; index < 50; index++) map.AddAnnotation(index + 2, "A complete retained annotation requires space", .5, .5);
        map.WithAnnotationsBox(.2, .2); Assert.Contains("increase", Assert.Throws<InvalidOperationException>(() => map.ToSvg()).Message, StringComparison.Ordinal);
        Assert.Equal(51, map.Annotations.Count);
    }

    [Fact]
    public void PipelineOutlineUsesExistingImmutableStrokeAndOffsetsWithoutChangingLegacyDashes() {
        var map = WardleyMapBlock.Create().WithSize(640, 480); map.AddNode("parent", "Parent", .7, .4);
        var pipeline = map.AddPipeline("parent"); pipeline.AddComponent("First", .2); pipeline.AddComponent("Second", .6);
        var old = map.ToSvg(); Assert.Equal("4 4", (string?)Assert.Single(Roles(XDocument.Parse(old), "wardley-pipeline")).Attribute("stroke-dasharray"));
        var dash = new[] { 6d, 2d }; pipeline.Outline = new ChartStrokeStyle(ChartColor.Parse("#7C3AED"), 2, dash: dash); dash[0] = 99;
        pipeline.ShowEvolutionLinks = true; var second = XDocument.Parse(map.ToSvg());
        Assert.Equal("6 2", (string?)Assert.Single(Roles(second, "wardley-pipeline")).Attribute("stroke-dasharray")); Assert.Single(Roles(second, "wardley-pipeline-evolution"));
        pipeline.Outline = null; pipeline.ShowEvolutionLinks = false; Assert.Equal(old, map.ToSvg());
    }

    [Theory]
    [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(-.1)] [InlineData(1.1)]
    public void InvalidAnnotationOriginsAndEndpointCoordinatesAreRejectedBeforeDrawing(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WardleyMapAnnotationBox(value, .5));
        var map = WardleyMapBlock.Create(); map.AddNode("api", "API", .5, .5); map.AddPipeline("api").AddComponent("child", "Child", value);
        Assert.Throws<InvalidOperationException>(() => map.ToSvg());
    }

    [Fact]
    public void StageBoundaryInputDetachesAtomicallyAndSourceLabelsUseAuthoredDomains() {
        var map = WardleyMapBlock.Create().WithSize(900, 600); map.AddNode("api", "API", .5, .5);
        map.SetStages(new[] { "Genesis / Concept", "Product" }); var edges = new[] { .3, 1d }; map.WithStageBoundaries(edges); edges[0] = .8;
        var xml = XDocument.Parse(map.ToSvg()); var plot = Assert.Single(Roles(xml, "wardley-plot")); var boundary = Assert.Single(Roles(xml, "wardley-stage-boundary"));
        Assert.Equal(Math.Round(N(plot, "x") + N(plot, "width") * .3, 3), N(boundary, "x1"));
        Assert.Equal(.3, map.StageBoundaries[0]); var old = map.ToSvg();
        Assert.Throws<ArgumentOutOfRangeException>(() => map.WithStageBoundaries(new[] { .7, .6 })); Assert.Equal(old, map.ToSvg());
        map.WithStageBoundaries(null); Assert.Equal(3, Roles(XDocument.Parse(map.ToSvg()), "wardley-stage-boundary").Length);
    }

    internal static XElement[] Roles(XDocument xml, string role) => xml.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == role).ToArray();
    internal static double N(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    internal static void Capture(WardleyMapBlock map, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), map.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".png"), map.ToPng()); File.WriteAllText(Path.Combine(output, name + ".html"), map.ToHtmlPage());
    }
}
