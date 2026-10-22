using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidC4NativeSemanticsTests {
    public const string Source = "C4Dynamic\ntitle Checkout\nEnterprise_Boundary(company, \"Company\") {\nSystem_Boundary(shop, \"Store\") {\nContainer(web, \"Web app\", \"Browser\", \"Checkout interface\", $link=\"https://example.test/web\")\nContainerDb(db, \"Orders\", \"SQL\", \"Stores orders\")\n}\n}\nPerson(customer, \"Customer\")\nRelIndex(99, customer, web, \"Checkout\")\nRel_D(web, db, \"Writes\")\nUpdateElementStyle(web, $bgColor=\"#123456\", $fontColor=\"#ffffff\", $borderColor=\"#ef6543\")\nUpdateRelStyle(web, db, $lineColor=\"#aabbcc\", $textColor=\"#ccbb99\", $offsetX=\"6\", $offsetY=\"-4\")";

    [Fact]
    public void C4BoundariesAreRealParentsDynamicIndexesFollowSourceAndStylesReachNativeOwners() {
        var parsed = new MermaidParser().ParseC4(Source); Assert.False(parsed.HasErrors);
        Assert.All(parsed.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.ApproximateMapping, item.Code));
        var document = parsed.Document!; var chart = document.ToTopologyChart();
        Assert.Equal("company", chart.Groups.Single(group => group.Id == "shop").ParentGroupId);
        Assert.Equal("shop", chart.Nodes.Single(node => node.Id == "web").GroupId);
        var web = chart.Nodes.Single(node => node.Id == "web");
        Assert.Equal("#123456", web.BackgroundColor); Assert.Equal("#ffffff", web.LabelColor); Assert.Equal("#ef6543", web.Color);
        Assert.Equal("https://example.test/web", web.Href);
        Assert.Equal("99", document.Relationships[0].AuthoredIndex);
        Assert.Equal("1: Checkout", chart.Edges[0].Label); Assert.Equal("2: Writes", chart.Edges[1].Label);
        Assert.Equal(TopologyEdgePort.Bottom, chart.Edges[1].SourcePort); Assert.Equal(TopologyEdgePort.Top, chart.Edges[1].TargetPort);
        Assert.Equal("#aabbcc", chart.Edges[1].Color); Assert.Equal("#ccbb99", chart.Edges[1].LabelColor);
        Assert.Equal(6, chart.Edges[1].LabelOffsetX); Assert.Equal(-4, chart.Edges[1].LabelOffsetY);
        var facts = chart.Prepare().ToInterchangeEnvelope();
        var company = facts.Groups.Single(group => group.Id == "company"); var shop = facts.Groups.Single(group => group.Id == "shop");
        Assert.True(company.X <= shop.X && company.Y <= shop.Y && company.X + company.Width >= shop.X + shop.Width && company.Y + company.Height >= shop.Y + shop.Height);
        Assert.Equal(chart.Prepare().ToSvg(), chart.Prepare().ToSvg());
    }

    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void NestedC4DirectFencedSvgPngRetainShellsLinksAndNativePaint(bool dark, int width) {
        var options = new MermaidRenderOptions { Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family }, C4 = new MermaidTopologyRenderOptions { Width = width, Height = 700 } };
        var direct = MermaidRenderer.Render(Source, options); Assert.False(direct.HasErrors);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact); var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + Source + "\n```"); Assert.False(fence.HasErrors);
        var fenced = Assert.Single(fence.Artifacts); var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
        var xml = XDocument.Parse(svg);
        Assert.Contains(xml.Descendants(), item => (string?)item.Attribute("data-group-id") == "company");
        Assert.Contains(xml.Descendants(), item => (string?)item.Attribute("data-group-id") == "shop");
        Assert.Contains(xml.Descendants(), item => item.Attributes().Any(attribute => attribute.Name.LocalName == "href" && attribute.Value == "https://example.test/web"));
        Assert.Contains("#123456", svg, StringComparison.OrdinalIgnoreCase); Assert.Contains("#aabbcc", svg, StringComparison.OrdinalIgnoreCase);
        var raster = RasterImageDecoder.Decode(png); var size = artifact.MeasureRasterSize(); Assert.Equal((int)size.Width, raster.Width); Assert.Equal((int)size.Height, raster.Height);
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root); var path = Path.Combine(root, "c4-nested-" + width + "-" + (dark ? "dark" : "light")); File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png);
    }

    [Fact]
    public void C4LinksKeepTheExistingNativeSafeLinkBoundary() {
        const string source = "C4Context\nPerson(user, \"User\", $link=\"javascript:alert(1)\")";
        var parsed = new MermaidParser().ParseC4(source); Assert.False(parsed.HasErrors);
        var xml = XDocument.Parse(parsed.Document!.ToSvg());
        Assert.DoesNotContain(xml.Descendants().SelectMany(item => item.Attributes()), attribute => attribute.Name.LocalName == "href" && attribute.Value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("C4Context\nBoundary(shared, \"Scope\") {\nPerson(shared, \"Collision\")\n}")]
    [InlineData("C4Context\nPerson(user, \"User\")\nUpdateElementStyle(missing, $bgColor=\"red\")")]
    [InlineData("C4Context\nPerson(user, \"User\")\nUpdateElementStyle(user, $bgColor=\"not-a-color\")")]
    [InlineData("C4Context\nPerson(a, \"A\")\nPerson(b, \"B\")\nRel(a,b,\"Uses\")\nUpdateRelStyle(a,b,$offsetX=\"NaN\")")]
    public void C4InvalidNativeUpdatesAreLocatedErrors(string source) {
        var parsed = new MermaidParser().ParseC4(source); Assert.True(parsed.HasErrors);
        Assert.Contains(parsed.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error && item.Span.Line > 1);
    }
}
