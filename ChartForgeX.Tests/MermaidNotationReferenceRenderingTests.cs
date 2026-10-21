using System.Text.Json;
using System.Text.Json.Nodes;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Observes shared notation sources in pinned upstream browsers and native SVG/PNG.</summary>
public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData("10", 640, false)]
    [InlineData("11", 1100, false)]
    [InlineData("12", 640, false)]
    [InlineData("12", 1100, true)]
    public async Task PinnedNotationAndNativeOutputPreserveSharedVisibleContracts(string id, int width, bool dark) {
        Assert.True(InteractiveChartBrowser.Enabled);
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty(id);
        var package = reference.GetProperty("package").GetString()!;
        var version = reference.GetProperty("version").GetString()!;
        var bundleRoot = Path.Combine(corpus, "node_modules", package);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundleRoot, "package.json")));
        Assert.Equal(version, installed.RootElement.GetProperty("version").GetString());
        var projection = File.ReadAllText(Path.Combine(corpus, "reference-notation.mjs")).Replace("export function ", "function ", StringComparison.Ordinal);
        foreach (var fixture in new[] { "class-native-notes-generics", "class-two-way", "state-controls-notes", "state-nested-directions",
            "er-alias-attributes", "sequence-lifetimes-boxes", "flowchart-nested-directions" }) {
            var contract = manifest.RootElement.GetProperty("fixtures").GetProperty("fixtures/" + fixture + ".mmd");
            Assert.Equal("accepted", contract.GetProperty("syntax").GetProperty(id).GetString());
            var expectedName = contract.TryGetProperty("expectedByReference", out var variants) && variants.TryGetProperty(id, out var variant)
                ? variant.GetString()! : fixture + ".expected.json";
            var expectedPath = Path.Combine(corpus, "fixtures", expectedName);
            using var expected = JsonDocument.Parse(File.Exists(expectedPath) ? File.ReadAllText(expectedPath) : "{}");
            var source = File.ReadAllText(Path.Combine(corpus, "fixtures", fixture + ".mmd"));
            var key = fixture.StartsWith("class", StringComparison.Ordinal) ? "classFacts" : fixture.StartsWith("state", StringComparison.Ordinal) ? "stateFacts"
                : fixture.StartsWith("er-", StringComparison.Ordinal) ? "erFacts" : fixture.StartsWith("sequence", StringComparison.Ordinal) ? "sequenceLifetimes" : "";
            var labels = expected.RootElement.TryGetProperty("peerVisibleText", out var text) ? text.EnumerateArray().Select(item => item.GetString()!).ToArray()
                : new[] { "Delivery", "Intake", "Review", "Receive request", "Validate request", "Record receipt", "Approve request", "Publish result", "Done", "Hand off" };
            if (key == "sequenceLifetimes") labels = labels.Append("Delivery lifecycle").ToArray();
            var options = new MermaidRenderOptions { Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family },
                Flowchart = new MermaidFlowchartRenderOptions { Width = width - 32, Height = 620 },
                Class = new MermaidTopologyRenderOptions { Width = width - 32, Height = 620 },
                State = new MermaidTopologyRenderOptions { Width = width - 32, Height = 620 },
                EntityRelationship = new MermaidTopologyRenderOptions { Width = width - 32, Height = 620 },
                Sequence = new MermaidSequenceRenderOptions { Width = width - 32, Height = 620 } };
            var rendered = MermaidRenderer.Render((dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + source, options);
            Assert.Empty(rendered.Diagnostics);
            var artifact = rendered.Artifact!;
            var svg = artifact.ToSvg(); var png = artifact.ToPng();
            var native = artifact.ToInterchangeEnvelope();
            if (key is "classFacts" or "erFacts") {
                var relations = expected.RootElement.GetProperty(key).GetProperty("relationships").EnumerateArray().ToArray();
                Assert.Equal(relations.Length, native.Edges.Count);
                for (var index = 0; index < relations.Length; index++) {
                    Assert.Equal(relations[index][0].GetString(), native.Edges[index].SourceId);
                    Assert.Equal(relations[index][1].GetString(), native.Edges[index].TargetId);
                    Assert.Equal(relations[index][2].GetString(), native.Edges[index].Topology!.SourceMarker.ToString());
                    Assert.Equal(relations[index][3].GetString(), native.Edges[index].Topology!.TargetMarker.ToString());
                }
            }
            var background = dark ? "#171b23" : "#fff"; var color = dark ? "#edf2f7" : "#182235";
            var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + background + ";color:" + color
                + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
                + "<h2>Mermaid " + version + " / " + fixture + "</h2><section id='reference'></section>"
                + "<h2>ChartForgeX SVG</h2><section id='native'>" + svg + "</section>"
                + "<h2>ChartForgeX PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
            await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
            var errors = new List<string>(); session.Page.PageError += (_, error) => errors.Add(error);
            await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundleRoot, "dist", "mermaid.min.js") });
            await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = "(()=>{" + projection + ";window.cfxReferenceFacts={classFacts,stateFacts,erFacts,sequenceLifetimes:sequenceLifetimeFacts};})();" });
            var observed = await ObserveAsync(session.Page, source, key, dark, Regions(fixture));
            await CaptureAsync(session.Page, "notation-reference-" + id + "-" + fixture + "-" + width + (dark ? "-dark" : "-light"),
                svg, png, native.ToJson(), observed);
            Assert.True(observed.GetProperty("referencePaintElements").GetInt32() > 0);
            if (key.Length > 0) Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected.RootElement.GetProperty(key).GetRawText()),
                JsonNode.Parse(observed.GetProperty("facts").GetRawText())), fixture + ": pinned browser semantic facts changed.");
            foreach (var label in labels) foreach (var surface in new[] { "referenceText", "nativeText" })
                Assert.Contains(label, observed.GetProperty(surface).GetString()!, StringComparison.Ordinal);
            Assert.Empty(observed.GetProperty("clip").EnumerateArray());
            foreach (var (role, count) in Regions(fixture)) Assert.Equal(count, observed.GetProperty("nativeRegions").GetProperty(role).GetInt32());
            Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
            Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32());
            Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }

    private static Dictionary<string, int> Regions(string fixture) => fixture switch {
        "class-native-notes-generics" => new() { ["topology-node"] = 2, ["topology-edge"] = 1, ["topology-annotation"] = 2 },
        "class-two-way" or "er-alias-attributes" => new() { ["topology-node"] = 2, ["topology-edge"] = 1 },
        "state-controls-notes" => new() { ["topology-node"] = 7, ["topology-edge"] = 7, ["topology-annotation"] = 1 },
        "state-nested-directions" => new() { ["topology-node"] = 3, ["topology-group"] = 2, ["topology-edge"] = 2, ["topology-annotation"] = 1 },
        "sequence-lifetimes-boxes" => new() { ["sequence-participant"] = 4, ["sequence-message"] = 6, ["sequence-participant-group"] = 2, ["sequence-destruction"] = 1 },
        _ => new() { ["topology-node"] = 6, ["topology-group"] = 3, ["topology-edge"] = 5 }
    };
}
