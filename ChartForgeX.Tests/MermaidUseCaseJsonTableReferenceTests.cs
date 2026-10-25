using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1000, false)] [InlineData(1000, true)]
    public async Task Pinned12UseCaseJsonRowsPreserveOrderedValuesAndRemainPartOfTheSameLinkedGraph(int width, bool dark) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = UseCaseJsonTableFixture.Read(width == 390);
        var direct = MermaidRenderer.Render(source, MermaidUseCaseJsonTableTests.Options(width, dark)); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync("<style>section{overflow:auto}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned source renderer</h2><section id='reference'></section><h2>Native SVG</h2><section id='native'>" + svg
            + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>", width + 24, 1100);
        var errors = new List<string>(); session.Page.PageError += (_, message) => errors.Add(message);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async ({source,dark,fontFamily}) => {
              mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,theme:dark?'dark':'default',look:'classic'});
              const diagram=await mermaid.mermaidAPI.getDiagramFromText(source),db=diagram.db;
              const table=db.getData().nodes.find(node=>node.id==='Details');
              const facts={jsonIds:[...db.getJsonNodes().keys()],rows:table.jsonRows,
                edges:db.getRelationships().map(edge=>({id:edge.id,source:edge.source,target:edge.target})),
                notes:[...db.getNotes().values()].map(note=>({target:note.target,label:note.label}))};
              const rendered=await mermaid.render('usecase-source-reference',source);document.querySelector('#reference').innerHTML=rendered.svg;
              await document.fonts.ready;await document.querySelector('#png').decode();
              return {facts,referenceText:document.querySelector('#reference').textContent,
                nativeText:[...document.querySelectorAll('#native svg text')].map(n=>n.textContent).join(' '),
                referencePaint:document.querySelectorAll('#reference path,#reference rect,#reference ellipse').length,
                nativeActive:document.querySelectorAll('#native script,#native image,#native foreignObject').length};
            }
            """, new { source, dark, fontFamily = BrowserProofFonts.Family });
        var facts = observed.GetProperty("facts"); Assert.Equal("Details", Assert.Single(facts.GetProperty("jsonIds").EnumerateArray()).GetString());
        Assert.Equal(9, facts.GetProperty("rows").GetArrayLength());
        // The pinned engine normalizes JSON numbers for display; native source facts also retain the authored 1.00 spelling.
        Assert.Equal(new[] { "1", "false", "", "Aster", "null", "ready", "signed", "kept", "[]" }, facts.GetProperty("rows").EnumerateArray().Select(row => row.GetProperty("value").GetString()));
        Assert.Equal("", facts.GetProperty("rows")[6].GetProperty("key").GetString()); Assert.Equal("tags", facts.GetProperty("rows")[6].GetProperty("accessibleKey").GetString());
        Assert.Equal("Details", Assert.Single(facts.GetProperty("notes").EnumerateArray()).GetProperty("target").GetString()); Assert.Equal(2, facts.GetProperty("edges").GetArrayLength());
        foreach (var word in new[] { "Aster", "false", "null", "ready", "signed", "kept", "Captured", "linked" })
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(word, observed.GetProperty(surface).GetString());
        Assert.True(observed.GetProperty("referencePaint").GetInt32() > 0); Assert.Equal(0, observed.GetProperty("nativeActive").GetInt32());
        await CaptureAsync(session.Page, "usecase-json-reference12-" + width + (dark ? "-dark" : "-light"), svg, png, artifact.ToInterchangeJson(), observed);
        Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
