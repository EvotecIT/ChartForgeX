using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1100, false)] [InlineData(1100, true)]
    public async Task Pinned12ActorModelsAndActualContoursKeepBusinessHollowSilhouetteIconAndStereotypeMeaning(int width, bool dark) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = UseCaseAdvancedActorFixture.Read(width == 390);
        var native = MermaidRenderer.Render(source, UseCaseAdvancedActorFixture.Options(width, dark)); Assert.Empty(native.Diagnostics);
        var artifact = native.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync("<style>section{overflow:auto}svg,img{display:block;max-width:none}</style><h2>Pinned source renderer</h2><section id='reference'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'>", width + 24, width == 390 ? 1250 : 900);
        var errors = new List<string>(); session.Page.PageError += (_, message) => errors.Add(message);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async ({source,dark,fontFamily}) => {
              mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,theme:dark?'dark':'default',look:'classic'});
              const diagram=await mermaid.mermaidAPI.getDiagramFromText(source),db=diagram.db;
              const facts={actors:[...db.getActors().values()].map(n=>({id:n.id,type:n.type,business:n.business,icon:n.icon??null,stereotype:n.stereotype})),
                cases:[...db.getUseCases().values()].map(n=>({id:n.id,business:n.business,shape:n.shape})),edges:db.getRelationships().map(e=>e.id)};
              const rendered=await mermaid.render('advanced-actor-reference',source);document.querySelector('#reference').innerHTML=rendered.svg;
              await document.fonts.ready;await document.querySelector('#png').decode();
              return {facts,referenceText:document.querySelector('#reference').textContent,nativeText:[...document.querySelectorAll('#native svg text')].map(e=>e.textContent).join(' '),
                hollow:document.querySelectorAll('#reference .usecase-actor-hollow-body').length,silhouette:document.querySelectorAll('#reference .usecase-actor-awesome-silhouette').length,
                business:document.querySelectorAll('#reference .usecase-actor-business-marker').length,iconFrames:document.querySelectorAll('#reference .usecase-actor-icon-frame').length,
                // The reference has no registered network icon pack. Its explicit fallback is evidence, never claimed as native Router pixel equality.
                referenceIconFallback:document.querySelectorAll('#reference .usecase-actor-icon-fallback').length,
                nativeActive:document.querySelectorAll('#native script,#native image,#native foreignObject').length};
            }
            """, new { source, dark, fontFamily = BrowserProofFonts.Family });
        var actors = observed.GetProperty("facts").GetProperty("actors");
        Assert.Equal(new[] { "normal", "hollow", "normal", "hollow", "awesome", "icon" }, actors.EnumerateArray().Select(a => a.GetProperty("type").GetString()));
        Assert.Equal(new[] { false, false, true, true, false, false }, actors.EnumerateArray().Select(a => a.GetProperty("business").GetBoolean()));
        Assert.Equal("network:router", actors[5].GetProperty("icon").GetString()); Assert.True(observed.GetProperty("facts").GetProperty("cases")[0].GetProperty("business").GetBoolean());
        Assert.Equal(2, observed.GetProperty("hollow").GetInt32()); Assert.Equal(1, observed.GetProperty("silhouette").GetInt32());
        Assert.Equal(2, observed.GetProperty("business").GetInt32()); Assert.Equal(1, observed.GetProperty("iconFrames").GetInt32()); Assert.Equal(1, observed.GetProperty("referenceIconFallback").GetInt32());
        foreach (var surface in new[] { "referenceText", "nativeText" }) foreach (var word in new[] { "Plain", "Hollow", "Business", "Silhouette", "Icon", "participant", "partner", "Submit request" }) Assert.Contains(word, observed.GetProperty(surface).GetString());
        Assert.Equal(0, observed.GetProperty("nativeActive").GetInt32()); MermaidUseCaseAdvancedActorTests.AssertPaint(svg);
        await CaptureAsync(session.Page, "usecase-advanced-actors-reference12-" + width + (dark ? "-dark" : "-light"), svg, png, artifact.ToInterchangeJson(), observed);
        Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
