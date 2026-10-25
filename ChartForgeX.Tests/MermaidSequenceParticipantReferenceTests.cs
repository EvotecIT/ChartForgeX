using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(false, 390)]
    [InlineData(true, 1100)]
    public async Task CurrentPinnedParticipantsPreserveConfiguredTypesAliasesAndInertMenuPropertyFacts(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidSequenceParticipantTests.Source(dark);
        var rendered = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family },
            Sequence = new MermaidSequenceRenderOptions { Width = width - 32, Height = 620 }
        });
        Assert.False(rendered.HasErrors);
        var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + " / participants</h2><section id='reference'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark,
            new Dictionary<string, int> { ["sequence-participant"] = 8, ["sequence-message"] = 9, ["sequence-notation"] = 7 });
        var participants = await session.Page.EvaluateAsync<JsonElement>("""
            async source => {
                const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db;
                return {actors:[...db.getActors()].map(([id,actor])=>({id,type:actor.type,alias:actor.description})),
                    storageLinks:db.getActor('D').links,queueLinks:db.getActor('Q').links,storageProperties:db.getActor('D').properties};
            }
            """, source);
        var name = "sequence-participants-reference12-" + width + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output)
            File.WriteAllText(Path.Combine(output, name + ".participants.json"), participants.GetRawText());
        Assert.Equal(new[] { "participant", "actor", "boundary", "control", "entity", "database", "collections", "queue" },
            participants.GetProperty("actors").EnumerateArray().Select(actor => actor.GetProperty("type").GetString()));
        Assert.Equal("Operations", participants.GetProperty("storageProperties").GetProperty("owner").GetString());
        Assert.Equal(2, participants.GetProperty("storageLinks").EnumerateObject().Count());
        Assert.Equal(2, participants.GetProperty("queueLinks").EnumerateObject().Count());
        Assert.Equal("https://example.test/status", artifact.ToInterchangeEnvelope().Nodes.Single(node => node.Id == "D").Href);
        Assert.Null(artifact.ToInterchangeEnvelope().Nodes.Single(node => node.Id == "Q").Href);
        foreach (var label in new[] { "Native", "User", "Worker", "Record", "Storage", "Items", "Inbox", "Stable source facts" })
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(label, observed.GetProperty(surface).GetString());
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
        Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
