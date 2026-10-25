using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(false, 390)] [InlineData(true, 1100)]
    public async Task CurrentPinnedOptionalCaptionsRemainAbsentAndBuiltinsRemainSourceNames(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidArchitectureCaptionTests.Fixture(); var rendered = MermaidRenderer.Render(source, MermaidArchitectureNativeDepthTests.Options(dark, width - 32));
        Assert.Empty(rendered.Diagnostics); var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + " / optional captions</h2><section id='reference'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark, new Dictionary<string, int> { ["topology-node"] = 4, ["topology-group"] = 3, ["topology-edge"] = 3 });
        var facts = await session.Page.EvaluateAsync<JsonElement>("""
            async source => {
                const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db;
                return {groups:db.getGroups().map(group=>({id:group.id,title:group.title??null,icon:group.icon??null})),
                    services:db.getServices().map(node=>({id:node.id,title:node.title??null,icon:node.icon??null})),
                    text:[...document.querySelectorAll('#reference svg text')].map(text=>text.textContent)};
            }
            """, source);
        var name = "architecture-captions-reference12-" + width + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) File.WriteAllText(Path.Combine(output, name + ".captions.json"), facts.GetRawText());
        Assert.Equal(JsonValueKind.Null, facts.GetProperty("groups")[0].GetProperty("title").ValueKind);
        Assert.Equal(JsonValueKind.Null, facts.GetProperty("groups")[1].GetProperty("title").ValueKind);
        Assert.Equal(JsonValueKind.Null, facts.GetProperty("services")[0].GetProperty("title").ValueKind);
        Assert.Equal("cloud", facts.GetProperty("groups")[0].GetProperty("icon").GetString()); Assert.Equal("server", facts.GetProperty("services")[0].GetProperty("icon").GetString());
        var native = artifact.ToInterchangeEnvelope(); Assert.False(native.Groups[0].Topology!.ShowLabel); Assert.False(native.Nodes[0].Topology!.ShowLabel);
        Assert.Equal("common:server", native.Nodes[0].IconId); Assert.Equal("cloud:cloud", native.Groups[0].Topology!.IconId);
        foreach (var caption in new[] { "Facts", "Orders", "Plain" }) foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(caption, observed.GetProperty(surface).GetString());
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
