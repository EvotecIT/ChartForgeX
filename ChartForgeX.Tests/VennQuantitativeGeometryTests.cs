using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VennQuantitativeGeometryTests {
    [Theory]
    [InlineData(100, 25, 0)]
    [InlineData(100, 25, 10)]
    [InlineData(100, 25, 25)]
    [InlineData(10, 10, 10)]
    [InlineData(1e300, 1e298, 1e297)]
    public void TwoSetsPreserveAreaRatiosAndDeclaredOverlapIncludingContainmentAndDisjointCases(double first, double second, double overlap) {
        var block = VennDiagramBlock.Create().WithSize(800, 500).AddSet("A", "First", first).AddSet("B", "Second", second).AddIntersection(new[] { "A", "B" }, "Shared", overlap);
        var layout = block.MeasureLayout(); Assert.True(layout.IsAreaAccurate);
        var a = layout.Circles[0]; var b = layout.Circles[1]; var region = Assert.Single(layout.Regions);
        Assert.InRange(Math.Abs(a.Radius * a.Radius / (b.Radius * b.Radius) - first / second), 0, 1e-8);
        var distance = Math.Sqrt(Math.Pow(a.Center.X - b.Center.X, 2) + Math.Pow(a.Center.Y - b.Center.Y, 2));
        var actualFraction = CircleOverlap(a.Radius, b.Radius, distance) / (Math.PI * a.Radius * a.Radius);
        Assert.InRange(Math.Abs(actualFraction - overlap / first), 0, 1e-7);
        Assert.InRange(Math.Abs(region.ActualArea / first - overlap / first), 0, 1e-7);
        if (overlap == 0) Assert.Empty(region.Path); else { Assert.Contains("A", region.Path, StringComparison.Ordinal); AssertInside(region.LabelAnchor, a, 1e-6); AssertInside(region.LabelAnchor, b, 1e-6); }
        Assert.Equal(block.ToSvg(), block.ToSvg());
    }

    [Fact]
    public void ThreeCirclesExposeTheResidualWhenValidSetCardinalitiesCannotBeRepresentedExactly() {
        const string source = "venn-beta\nset A: 10\nset B: 10\nset C: 10\nunion A,B: 5\nunion A,C: 5\nunion B,C: 5\nunion A,B,C: 5";
        var parsed = new MermaidParser().ParseVenn(source); Assert.False(parsed.HasErrors);
        var diagnostic = Assert.Single(parsed.Diagnostics); Assert.Equal(MermaidDiagnosticCodes.ApproximateMapping, diagnostic.Code);
        var block = parsed.Document!.ToVennDiagramBlock(); var layout = block.MeasureLayout();
        Assert.False(layout.IsAreaAccurate); Assert.True(layout.MaximumRelativeAreaError > .005);
        Assert.Equal(4, layout.Regions.Count); Assert.Contains(layout.Regions, region => Math.Abs(region.ActualArea - region.RequestedArea) > .05);
        var xml = XDocument.Parse(block.ToSvg()); var group = Assert.Single(xml.Descendants(), item => (string?)item.Attribute("data-cfx-role") == "venn-layout");
        Assert.Equal("false", group.Attribute("data-area-accurate")!.Value);
        Assert.Equal(layout.MaximumRelativeAreaError, double.Parse(group.Attribute("data-max-relative-area-error")!.Value, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("set A: 1\nset B: 2\nunion A,B: 2", 4)]
    [InlineData("set A: 10\nset B: 10\nset C: 10\nunion A,B: 9\nunion A,C: 9\nunion B,C: 1", 7)]
    [InlineData("set A: 10\nset B: 10\nunion A,B: 1\nunion B,A: 2", 5)]
    [InlineData("set A: 0\nset B: 1\nunion A,B: 1", 4)]
    public void ImpossibleOrDuplicateMembershipsAreRejectedAtTheAuthoredUnion(string body, int line) {
        var parsed = new MermaidParser().ParseVenn("venn-beta\n" + body); Assert.True(parsed.HasErrors);
        var error = Assert.Single(parsed.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code); Assert.Equal(line, error.Span.Line);
        Assert.Null(MermaidRenderer.Render("venn-beta\n" + body).Artifact);
    }

    [Fact]
    public void ZeroAreaSetsRemainFiniteAndAFeasibleNestedThreeSetLayoutCanBeExact() {
        var empty = VennDiagramBlock.Create().AddSet("A", "Empty", 0).AddSet("B", "Also empty", 0).AddIntersection(new[] { "A", "B" }, "", 0);
        var zero = empty.MeasureLayout(); Assert.All(zero.Circles, circle => { Assert.Equal(0, circle.Radius); Assert.True(double.IsFinite(circle.Center.X)); Assert.True(double.IsFinite(circle.Center.Y)); });
        Assert.Empty(Assert.Single(zero.Regions).Path); Assert.NotEmpty(empty.ToPng());
        var nested = VennDiagramBlock.Create().AddSet("A", "A", 100).AddSet("B", "B", 60).AddSet("C", "C", 20)
            .AddIntersection(new[] { "A", "B" }, "", 60).AddIntersection(new[] { "A", "C" }, "", 20)
            .AddIntersection(new[] { "B", "C" }, "", 20).AddIntersection(new[] { "A", "B", "C" }, "", 20);
        Assert.True(nested.MeasureLayout().IsAreaAccurate);
    }

    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void RealStyledOverlapUsesCircularArcGeometryAndTheSameDirectFencedSvgPng(bool dark, int width) {
        const string source = "venn-beta\nset A[First]: 100\nset B[Second]: 60\nunion A,B[Shared]: 25\nstyle A fill:#ee4455,fill-opacity:0.25,stroke-width:3\nstyle A,B fill:#2233ff,fill-opacity:1,stroke:#112233,stroke-width:2";
        var options = new MermaidRenderOptions { Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family }, Venn = new MermaidVennRenderOptions { Width = width, Height = 500 } };
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics); var artifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fence.Diagnostics); var fenced = Assert.Single(fence.Artifacts);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
        var xml = XDocument.Parse(svg); var intersection = Assert.Single(xml.Descendants(), item => (string?)item.Attribute("data-cfx-role") == "venn-intersection");
        Assert.Equal("path", intersection.Name.LocalName); Assert.Contains("A", intersection.Attribute("d")!.Value, StringComparison.Ordinal);
        Assert.Equal("#2233FF", intersection.Attribute("fill")!.Value.ToUpperInvariant()); Assert.Equal("2", intersection.Attribute("stroke-width")!.Value);
        var block = Assert.IsType<VennDiagramBlock>(artifact.Model); var layout = block.MeasureLayout(); var image = RasterImageDecoder.Decode(png);
        Assert.Equal(width, image.Width); Assert.Equal(500, image.Height);
        var bluePixels = 0;
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) {
            var offset = (y * image.Width + x) * 4;
            if (image.Pixels[offset] != 0x22 || image.Pixels[offset + 1] != 0x33 || image.Pixels[offset + 2] != 0xff || image.Pixels[offset + 3] != 0xff) continue;
            bluePixels++; Assert.All(layout.Circles, circle => AssertInside(new ChartPoint(x + .5, y + .5), circle, 1.5));
        }
        Assert.True(bluePixels > 100);
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root); var path = Path.Combine(root, "venn-real-overlap-" + width + "-" + (dark ? "dark" : "light")); File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png);
    }

    private static void AssertInside(ChartPoint point, VennCirclePlacement circle, double tolerance) => Assert.True(Math.Pow(point.X - circle.Center.X, 2) + Math.Pow(point.Y - circle.Center.Y, 2) <= Math.Pow(circle.Radius + tolerance, 2));
    private static double CircleOverlap(double a, double b, double distance) {
        if (distance >= a + b) return 0;
        if (distance <= Math.Abs(a - b)) return Math.PI * Math.Pow(Math.Min(a, b), 2);
        return a * a * Math.Acos((distance * distance + a * a - b * b) / (2 * distance * a)) + b * b * Math.Acos((distance * distance + b * b - a * a) / (2 * distance * b)) - .5 * Math.Sqrt((-distance + a + b) * (distance + a - b) * (distance - a + b) * (distance + a + b));
    }
}
