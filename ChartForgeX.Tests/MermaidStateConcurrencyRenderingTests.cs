using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects concurrent-region geometry and the shared SVG/PNG and Markdown output contracts.</summary>
public sealed class MermaidStateConcurrencyRenderingTests {
    [Theory]
    [InlineData("TB", false, 1)]
    [InlineData("BT", false, -1)]
    [InlineData("LR", true, 1)]
    [InlineData("RL", true, -1)]
    public void ConcurrentCompartmentsKeepSourceOrderAcrossTheFlowAndIndependentDirectedRanks(string direction, bool horizontal, int sign) {
        var source = "stateDiagram-v2\ndirection " + direction + "\nstate Active {\ndirection " + direction
            + "\nA --> B\n--\ndirection " + direction + "\nC --> D\n}\nActive --> Done";
        var parsed = new MermaidParser().ParseState(source);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart();
        var prepared = chart.Prepare();
        var facts = prepared.ToInterchangeEnvelope();
        var compartments = facts.Groups.Where(group => group.Topology!.Presentation == TopologyGroupPresentation.Compartment).ToArray();
        Assert.Equal(2, compartments.Length);
        if (horizontal) Assert.True(compartments[1].Y > compartments[0].Y + compartments[0].Height);
        else Assert.True(compartments[1].X > compartments[0].X + compartments[0].Width);
        foreach (var pair in new[] { ("A", "B"), ("C", "D") }) {
            var start = facts.Nodes.Single(node => node.Id == pair.Item1);
            var end = facts.Nodes.Single(node => node.Id == pair.Item2);
            Assert.True(((horizontal ? end.X - start.X : end.Y - start.Y) * sign) > 0);
        }
        Assert.All(prepared.Analyze().Edges, edge => { Assert.True(edge.SourceAttached, edge.Id); Assert.True(edge.TargetAttached, edge.Id); });
        var separator = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element => Role(element) == "topology-compartment-separator");
        Assert.Equal(horizontal, Number(separator, "y1") == Number(separator, "y2"));
        Assert.Equal(!horizontal, Number(separator, "x1") == Number(separator, "x2"));
        Assert.Equal(prepared.ToSvg(), chart.Prepare().ToSvg());
    }

    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void NestedConcurrentDirectAndFencedExportsRetainNotesShellTransitionsSeparatorsAndNativeInk(bool dark, int width) {
        var source = MermaidStateConcurrencyTests.Source;
        var options = new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light,
                FontFamily = BrowserProofFonts.Family, FontSize = 12 },
            State = new MermaidTopologyRenderOptions { Width = width, Height = 760 }
        };
        var direct = MermaidRenderer.Render(source, options);
        Assert.Empty(direct.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fenceArtifact = Assert.Single(fenced.Artifacts);
        var svg = artifact.ToSvg();
        var png = artifact.ToPng();
        Assert.Equal(svg, fenceArtifact.ToSvg());
        Assert.Equal(png, fenceArtifact.ToPng());
        var facts = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        var compartments = facts.Groups.Where(group => group.Topology!.Presentation == TopologyGroupPresentation.Compartment).ToArray();
        Assert.Equal(2, compartments.Length);
        Assert.All(compartments, group => Assert.Equal("Active", group.Topology!.ParentGroupId));
        Assert.Equal(TopologyLayoutDirection.LeftToRight, compartments[1].Topology!.LayoutDirection);
        Assert.Equal(TopologyLayoutDirection.BottomToTop, facts.Groups.Single(group => group.Id == "Waiting").Topology!.LayoutDirection);
        Assert.Contains(facts.Edges, edge => edge.SourceId == "Active" && edge.TargetId == "Done");
        Assert.Equal("On", Assert.Single(Assert.Single(facts.Annotations).TargetIds));
        var xml = XDocument.Parse(svg);
        var separator = Assert.Single(xml.Descendants(), element => Role(element) == "topology-compartment-separator");
        Assert.NotNull(separator.Attribute("stroke-dasharray"));
        Assert.DoesNotContain(xml.Descendants(), element => Role(element) == "topology-group-label" && element.Value.StartsWith("Region ", StringComparison.Ordinal));
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject");
        var raster = RasterImageDecoder.Decode(png);
        var measured = artifact.MeasureRasterSize();
        Assert.Equal((int)measured.Width, raster.Width);
        Assert.Equal((int)measured.Height, raster.Height);
        AssertSeparatorInk(xml.Root!, separator, raster.Pixels, raster.Width, raster.Height);
        var model = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal(model.Prepare().ToSvg(), model.Prepare().ToSvg());
        SaveArtifacts(svg, png, facts, dark, width);
    }

    private static void AssertSeparatorInk(XElement root, XElement separator, byte[] pixels, int width, int height) {
        var sx = width / Number(root, "width");
        var sy = height / Number(root, "height");
        var x = (int)Math.Round(Number(separator, "x1") * sx);
        var from = (int)Math.Ceiling(Number(separator, "y1") * sy) + 2;
        var to = (int)Math.Floor(Number(separator, "y2") * sy) - 2;
        Assert.InRange(x, 1, width - 6);
        Assert.InRange(from, 0, height - 1);
        Assert.InRange(to, from, height - 1);
        var painted = false;
        for (var y = from; y <= to; y++) {
            var mark = (y * width + x) * 4;
            var background = (y * width + x + 4) * 4;
            if (Enumerable.Range(0, 3).Sum(channel => Math.Abs(pixels[mark + channel] - pixels[background + channel])) > 8) {
                painted = true;
                break;
            }
        }
        Assert.True(painted, "The native PNG must paint the same region separator retained in SVG.");
    }

    private static void SaveArtifacts(string svg, byte[] png, VisualArtifactInterchangeEnvelope facts, bool dark, int width) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var path = Path.Combine(output, "state-concurrent-" + width + (dark ? "-dark" : "-light"));
        File.WriteAllText(path + ".svg", svg);
        File.WriteAllBytes(path + ".native.png", png);
        File.WriteAllText(path + ".interchange.json", facts.ToJson());
        File.WriteAllText(path + ".receipt.json", JsonSerializer.Serialize(new { dark, width,
            directSvgEqualsFence = true, directPngEqualsFence = true, nativeSeparatorInk = true }));
    }

    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
}
