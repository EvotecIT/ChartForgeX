using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>External annotation panels retain the native frame's breathing room in both source hosts.</summary>
public sealed class MermaidAnnotationMarginTests {
    [Theory]
    [InlineData("class", false)]
    [InlineData("class", true)]
    [InlineData("state", false)]
    [InlineData("state", true)]
    public void GalleryNotationInputsKeepHealthyDirectAndFencedNativePngMargins(string family, bool dark) {
        // These are the public gallery inputs mermaid-class-native-notes and mermaid-state-synchronization.
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + (family == "class" ? Class : State);
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fenceArtifact = Assert.Single(fenced.Artifacts);
        var svg = artifact.ToSvg();
        var png = artifact.ToPng();
        Assert.Equal(svg, fenceArtifact.ToSvg());
        Assert.Equal(png, fenceArtifact.ToPng());
        var model = Assert.IsType<TopologyChart>(artifact.Model);
        AssertAnnotationMargins(svg, model.Viewport.Padding);
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-mermaid-note-margin-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            foreach (var (name, item) in new[] { ("direct", artifact), ("fenced", fenceArtifact) }) {
                File.WriteAllText(Path.Combine(output, name + ".svg"), item.ToSvg());
                File.WriteAllBytes(Path.Combine(output, name + ".png"), item.ToPng());
                File.WriteAllText(Path.Combine(output, name + ".html"), item.ToHtmlPage());
            }
            GalleryWriter.Write(output);
            var manifestText = File.ReadAllText(Path.Combine(output, "svg-png-comparison.json"));
            using var manifest = JsonDocument.Parse(manifestText);
            var pairs = manifest.RootElement.GetProperty("charts").EnumerateArray().ToArray();
            Assert.Equal(2, pairs.Length);
            Assert.All(pairs, pair => {
                var health = pair.GetProperty("png");
                Assert.True(health.GetProperty("healthy").GetBoolean(), health.GetRawText());
                Assert.Equal(0, health.GetProperty("edgeInkPixels").GetInt64());
                Assert.True(pair.GetProperty("dimensionsMatch").GetBoolean());
            });
            var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(captures)) {
                Directory.CreateDirectory(captures);
                var name = Path.Combine(captures, "annotation-margin-" + family + (dark ? "-dark" : "-light"));
                File.WriteAllText(name + ".svg", svg);
                File.WriteAllBytes(name + ".native.png", png);
                File.WriteAllText(name + ".health.json", manifestText);
            }
        } finally {
            Directory.Delete(output, recursive: true);
        }
    }

    [Theory]
    [InlineData(TopologyAnnotationSide.Left)]
    [InlineData(TopologyAnnotationSide.Right)]
    public void NativeAnnotationsRetainFramePaddingOnEitherSide(TopologyAnnotationSide side) {
        var chart = TopologyChart.Create().WithViewport(640, 300, 24).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("a", "Source", 80, 100).AddNode("b", "Other", 420, 100);
        chart.Annotations.Add(new TopologyAnnotation("note", "An independent note", "a", side));
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false });
        AssertAnnotationMargins(prepared.ToSvg(), chart.Viewport.Padding);
        Assert.NotEmpty(prepared.ToPng());
    }

    private static void AssertAnnotationMargins(string svg, double padding) {
        var xml = XDocument.Parse(svg);
        var width = Number(xml.Root!, "width");
        var notes = xml.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-annotation-surface").ToArray();
        Assert.NotEmpty(notes);
        Assert.All(notes, note => {
            var halfStroke = Number(note, "stroke-width") / 2;
            Assert.True(Number(note, "x") - halfStroke >= padding, "The annotation's full left stroke retains the native frame padding.");
            Assert.True(Number(note, "x") + Number(note, "width") + halfStroke <= width - padding,
                "The annotation's full right stroke retains the native frame padding.");
        });
        static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
    }

    private const string Class = "classDiagram\nclass Repository~Item~ {\n  +List~Item~ items\n  +find() Item\n}\nRepository \"1\" <|--|> Store : contracts\nnote for Repository \"One stable identity\\nTyped generic members\"";
    private const string State = "stateDiagram-v2\nstate Fork <<fork>>\nstate Join <<join>>\nstate Choice <<choice>>\n[*] --> Fork\nFork --> Ready\nFork --> Waiting\nReady --> Join\nWaiting --> Join\nJoin --> Choice\nChoice --> Complete : approved\nnote right of Ready : Two independent paths";
}
