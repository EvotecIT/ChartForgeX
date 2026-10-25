using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidUseCaseAdvancedActorBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1100, false)] [InlineData(1100, true)]
    public async Task WholeFiniteActorFamilyShowsVisibleContoursStereotypesRoutesAndDetachedStaticExports(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = UseCaseAdvancedActorFixture.Read(width == 390);
        var result = MermaidRenderer.Render(source, UseCaseAdvancedActorFixture.Options(width, dark)); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; var nativePng = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width + 24, width == 390 ? 1220 : 800);
        var errors = new List<string>(); session.Page.PageError += (_, message) => errors.Add(message);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
              const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect();
              const box=e=>{const b=e.getBoundingClientRect();return {left:b.left,top:b.top,right:b.right,bottom:b.bottom,width:b.width,height:b.height}};
              const nodes=[...svg.querySelectorAll('[data-cfx-role=topology-node]')].map(node=>({id:node.getAttribute('data-node-id'),shape:node.getAttribute('data-node-shape'),
                glyph:node.getAttribute('data-node-icon-shape'),text:[...node.querySelectorAll('text')].map(e=>e.textContent).join(' '),bounds:box(node),
                surfaces:[...node.querySelectorAll('[data-cfx-role=topology-node-surface]')].map(p=>({fill:p.getAttribute('fill'),d:p.getAttribute('d')}))}));
              const texts=[...svg.querySelectorAll('text')];
              const captions=[...svg.querySelectorAll('[data-cfx-role=topology-node-label],[data-cfx-role=topology-node-subtitle]')].map(e=>({id:e.closest('[data-node-id]')?.getAttribute('data-node-id'),bounds:box(e)}));
              const overlaps=[];for(let i=0;i<captions.length;i++)for(let j=i+1;j<captions.length;j++){const a=captions[i],b=captions[j],x=a.bounds,y=b.bounds;
                if(a.id!==b.id&&Math.min(x.right,y.right)-Math.max(x.left,y.left)>.5&&Math.min(x.bottom,y.bottom)-Math.max(x.top,y.top)>.5)overlaps.push([a.id,b.id]);}
              return {nodes,overlaps,edges:[...svg.querySelectorAll('[data-cfx-role=topology-edge]')].map(e=>e.getAttribute('data-edge-id')),
                clipped:texts.filter(e=>{const b=e.getBoundingClientRect();return b.left<frame.left-1||b.top<frame.top-1||b.right>frame.right+1||b.bottom>frame.bottom+1}).map(e=>e.textContent),
                note:svg.querySelector('[data-cfx-role=topology-annotation]').getAttribute('data-target-id'),
                text:texts.map(e=>e.textContent).join(' '),active:svg.querySelectorAll('script,image,foreignObject').length};
            }
            """);
        Assert.Equal(new[] { "Plain", "Hollow", "Business", "BusinessHollow", "Silhouette", "Icon", "Submit" }, observed.GetProperty("nodes").EnumerateArray().Select(n => n.GetProperty("id").GetString()));
        Assert.Equal(6, observed.GetProperty("edges").GetArrayLength()); Assert.Equal("Submit", observed.GetProperty("note").GetString());
        foreach (var word in new[] { "Plain", "Hollow", "Business", "BusinessHollow", "Silhouette", "Icon", "participant", "external", "business", "partner", "operator", "system", "Submit request", "business action" }) Assert.Contains(word, observed.GetProperty("text").GetString());
        var nodes = observed.GetProperty("nodes"); Assert.Equal("Router", nodes[5].GetProperty("glyph").GetString());
        Assert.Equal(2, nodes[1].GetProperty("surfaces").GetArrayLength()); Assert.All(nodes[1].GetProperty("surfaces").EnumerateArray(), p => Assert.Equal("none", p.GetProperty("fill").GetString()));
        Assert.Equal(2, nodes[4].GetProperty("surfaces").GetArrayLength()); Assert.All(nodes[4].GetProperty("surfaces").EnumerateArray(), p => Assert.NotEqual("none", p.GetProperty("fill").GetString()));
        Assert.Empty(observed.GetProperty("clipped").EnumerateArray()); Assert.Empty(observed.GetProperty("overlaps").EnumerateArray()); Assert.Equal(0, observed.GetProperty("active").GetInt32()); Assert.Empty(errors);
        Assert.NotEmpty(nativePng); MermaidUseCaseAdvancedActorTests.AssertPaint(artifact.ToSvg());
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "usecase-advanced-actors-" + width + (dark ? "-dark" : "-light"));
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            File.WriteAllText(stem + ".observed.json", JsonSerializer.Serialize(new { observed, errors, session.ConsoleLog }));
            File.WriteAllText(stem + ".source.mmd", source); File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".native.png", nativePng); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
