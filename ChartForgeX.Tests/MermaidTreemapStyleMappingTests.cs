using ChartForgeX.Primitives;
using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Source styles and native numeric presentation must not rewrite hierarchy identity, source values or output owners.</summary>
public sealed class MermaidTreemapStyleMappingTests {
    internal static string GallerySource => File.ReadAllText(Path.Combine(TestRepository.Root,
        "ChartForgeX.Examples", "Assets", "Mermaid", "treemap-native-styles.mmd"));

    internal static MermaidRenderOptions Options(int width, bool dark) => new() { Treemap = new MermaidTreemapRenderOptions {
        Width = width, Height = 620, Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family }
    } };

    [Theory]
    [InlineData(390, false)]
    [InlineData(390, true)]
    [InlineData(1100, false)]
    [InlineData(1100, true)]
    public void AuthoredGallerySourceKeepsClassPaintMagnitudesAndWholeDirectFenceExports(int width, bool dark) {
        var source = GallerySource; var options = Options(width, dark);
        var parsed = new MermaidParser().ParseTreemap(source);
        Assert.Empty(parsed.Diagnostics); var document = parsed.Document!;
        Assert.Equal(new[] { "default", "group", "primary", "primary", "secondary" }, document.ClassDefinitions.Select(item => item.Name));
        Assert.Equal(new[] { "Allocation", "Operations", "Shared", "Reserve", "Research", "Shared" }, document.Nodes.Select(item => item.Label));
        Assert.Equal(new[] { "group", "group", "primary", "secondary", "group", "secondary" }, document.Nodes.Select(item => item.ClassName));
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var chart = Assert.IsType<Chart>(artifact.Model); var series = chart.Series[0];
        Assert.Equal(new double?[] { null, null, 1250.5, 0, null, 499.5 }, series.HierarchyItems.Select(item => item.Value));
        Assert.Equal(new string?[] { null, "node-0", "node-1", "node-1", "node-0", "node-4" }, series.HierarchyItems.Select(item => item.ParentId));
        Assert.Equal(ChartColor.FromHex("#1D4ED8"), series.PointColors[2]);
        Assert.Equal(ChartColor.FromHex("#B45309"), series.PointColors[5]);
        Assert.All(series.PointStrokeColors, color => Assert.Equal(ChartColor.FromHex("#14532D"), color));
        var svg = artifact.ToSvg(); var xml = XDocument.Parse(svg);
        var shared = Node(xml, "node-2");
        Assert.Equal("1250.5", (string?)shared.Attribute("data-cfx-value"));
        Assert.Equal("$1,250.50", (string?)shared.Attribute("data-cfx-formatted-value"));
        var mark = Assert.Single(shared.Descendants(), item => (string?)item.Attribute("data-cfx-role") == "treemap-tile-mark");
        Assert.Equal("#1D4ED8", ((string?)mark.Attribute("fill"))?.ToUpperInvariant());
        Assert.Equal("#14532D", ((string?)mark.Attribute("stroke"))?.ToUpperInvariant());
        Assert.Equal("zero", (string?)Node(xml, "node-3").Attribute("data-cfx-geometry-status"));
        Assert.DoesNotContain(Node(xml, "node-3").Descendants(), item => (string?)item.Attribute("data-cfx-role") == "treemap-tile-mark");
        var png = artifact.ToPng(); Assert.Equal((width, 620), (PngReader.Decode(png).Width, PngReader.Decode(png).Height));
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 620)),
            themeMode: dark ? VisualThemeMode.Dark : VisualThemeMode.Light));
        var pixels = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var bounds = prepared.Regions.Single(item => item.Id == "series-0-node-node-2").Bounds;
        Assert.True(ContainsPixel(pixels, bounds, ChartColor.FromHex("#1D4ED8")));
        Assert.True(ContainsPixel(pixels, bounds, ChartColor.FromHex("#14532D")));
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics); var fence = Assert.Single(fenced.Artifacts);
        Assert.Equal(svg, fence.ToSvg()); Assert.Equal(png, fence.ToPng());
        var portable = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal("primary", portable.Extensions["mermaid.treemap.node.2.class"]);
        Assert.Equal("#1D4ED8", portable.Extensions["mermaid.treemap.class.3.style.fill"]);
        AssertDirectAndFencedFacts(artifact, fence);
        Capture("treemap-style-" + width + "-" + (dark ? "dark" : "light"), svg, png);
    }

    [Fact]
    public void DefinitionsDoNotChangeHierarchyIndentationAndUnusedOrUnknownClassesDoNotChangeNativePaint() {
        const string body = "treemap-beta\n\"Root\"\n  \"Same\": 3\n  \"Same\": 2\n";
        var plain = new MermaidParser().ParseTreemap(body).Document!;
        var defined = new MermaidParser().ParseTreemap(body.Replace("  \"Same\": 2", "classDef unused fill:#ff0000\n  \"Same\": 2:::unknown")).Document!;
        Assert.Equal(plain.ToSvg(), defined.ToSvg()); Assert.Equal(plain.ToPng(), defined.ToPng());
        Assert.Equal("Root", defined.Nodes[2].Parent!.Label); Assert.Equal(2, defined.Nodes[2].Indent);
    }

    [Fact]
    public void TreemapDefaultIsAnExplicitClassNameAndDoesNotBecomeAnImplicitFamilyCascade() {
        const string body = "treemap-beta\n\"Leaf\": 2";
        var plain = MermaidRenderer.Render(body).Artifact!;
        var unselected = MermaidRenderer.Render(body + "\nclassDef default fill:#F90000");
        Assert.Empty(unselected.Diagnostics); Assert.Equal(plain.ToSvg(), unselected.Artifact!.ToSvg());
        var selected = MermaidRenderer.Render(body.Replace(": 2", ": 2:::default") + "\nclassDef default fill:#F90000");
        Assert.Empty(selected.Diagnostics);
        Assert.Equal(ChartColor.FromHex("#F90000"), Assert.IsType<Chart>(selected.Artifact!.Model).Series[0].PointColors[0]);
    }

    [Theory]
    [InlineData("classDef", 2)]
    [InlineData("classDef marked", 2)]
    [InlineData("classDef first,second fill:#123456", 2)]
    public void IncompleteDefinitionsProduceALocatedSourceError(string definition, int line) {
        var result = MermaidRenderer.Render("treemap-beta\n" + definition + "\n\"Leaf\": 1");
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error && item.Span.Line == line);
    }

    [Theory]
    [InlineData("opacity:0.3")]
    [InlineData("fill:var(--surface,#ff0000)")]
    [InlineData("stroke-width:4px")]
    public void UnsupportedCssRemainsInertLocatedAndStrictWarningPolicyRejectsIt(string declaration) {
        var source = "treemap-beta\n\"Leaf\": 1:::marked\nclassDef marked " + declaration;
        var parsed = new MermaidParser().ParseTreemap(source); Assert.NotNull(parsed.Document);
        Assert.Contains(parsed.Diagnostics, item => item.Span.Line == 3 && item.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        var direct = MermaidRenderer.Render(source); Assert.NotNull(direct.Artifact);
        Assert.Equal(declaration.Substring(declaration.IndexOf(':') + 1), direct.Artifact!.Metadata["mermaid.treemap.class.0.style." + declaration.Substring(0, declaration.IndexOf(':'))]);
        var strict = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject };
        Assert.Null(MermaidRenderer.Render(source, strict).Artifact);
        Assert.Empty(new MermaidVisualMarkupParser(strict).Parse("```mermaid\n" + source + "\n```").Artifacts);
    }

    [Fact]
    public void SharedNamedStyleExpansionRetainsCapturedLimitsAndCancellation() {
        var source = "treemap-beta\nclassDef first fill:#123456,stroke:#654321,color:#ffffff\n\"Leaf\": 1:::first";
        var options = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 2 } };
        var parsed = new MermaidParser().Parse(source, options);
        Assert.Null(parsed.Document);
        Assert.Contains(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line == 2);
        var host = new MermaidRenderOptions { ParseOptions = options };
        Assert.Null(MermaidRenderer.Render(source, host).Artifact);
        Assert.Empty(new MermaidVisualMarkupParser(host).Parse("```mermaid\n" + source + "\n```").Artifacts);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new MermaidParser().Parse(source, options, cancellation.Token));
    }

    internal static void AssertDirectAndFencedFacts(VisualArtifact direct, VisualArtifact fenced) {
        var directFacts = direct.ToInterchangeEnvelope(); var fencedFacts = fenced.ToInterchangeEnvelope();
        Assert.Equal("mermaid", fencedFacts.Extensions["fence"]);
        foreach (var key in new[] { "fence", "sourceLine", "payloadStartLine", "payloadEndLine" }) {
            Assert.False(directFacts.Extensions.ContainsKey(key));
            Assert.Equal(fenced.Metadata[key], fencedFacts.Extensions[key]);
            if (key != "fence") Assert.True(int.Parse(fencedFacts.Extensions[key]) > 0);
            Assert.True(fencedFacts.Extensions.Remove(key));
        }
        Assert.Equal(directFacts.ToJson(), fencedFacts.ToJson());
        Assert.Equal("mermaid", fenced.Metadata["fence"]);
    }

    internal static XElement Node(XDocument xml, string id) => Assert.Single(xml.Descendants(), item => (string?)item.Attribute("data-cfx-target-id") == id);
    internal static void Capture(string name, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(directory, name + ".png"), png);
    }
    private static bool ContainsPixel(RgbaImage image, ChartRect bounds, ChartColor expected) {
        for (var y = Math.Max(0, (int)bounds.Top); y < Math.Min(image.Height, Math.Ceiling(bounds.Bottom)); y++)
            for (var x = Math.Max(0, (int)bounds.Left); x < Math.Min(image.Width, Math.Ceiling(bounds.Right)); x++) {
                var offset = (y * image.Width + x) * 4;
                if (image.Pixels[offset] == expected.R && image.Pixels[offset + 1] == expected.G
                    && image.Pixels[offset + 2] == expected.B && image.Pixels[offset + 3] == expected.A) return true;
            }
        return false;
    }
}
