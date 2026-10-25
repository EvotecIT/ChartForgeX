using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData("LR", false, false, 390)] [InlineData("LR", true, true, 1100)]
    [InlineData("TB", false, true, 390)] [InlineData("TB", true, false, 1100)]
    [InlineData("BT", false, false, 390)] [InlineData("BT", true, true, 1100)]
    public async Task CurrentPinnedGitGraphObservesBranchConfigurationDirectionAndParallelParentDepth(string direction, bool parallel, bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidGitGraphDepthTests.Source(direction, parallel); var options = MermaidGitGraphDepthTests.Options(width, dark);
        options.GitGraph!.Presentation!.FontFamily = BrowserProofFonts.Family;
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics); var artifact = direct.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var body = "<style>body{padding:12px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:16px}section{max-width:100%;overflow:auto;margin-bottom:16px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + "</h2><section id='reference'></section><h2>Native SVG</h2><section id='native'>" + svg
            + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(body, width + 24, 1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async ({source,dark,fontFamily}) => {
              mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,theme:dark?'dark':'default',look:'classic'});
              const result=await mermaid.render('gitgraph-reference',source);document.querySelector('#reference').innerHTML=result.svg;
              const reference=document.querySelector('#reference svg'),native=document.querySelector('#native svg');
              for(const svg of [reference,native]){const view=svg.viewBox.baseVal;svg.style.width=view.width+'px';svg.style.height=view.height+'px';svg.style.maxWidth='none'}
              await document.fonts.ready;await document.querySelector('#png').decode();
              const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db,commits=[...db.getCommits().values()].sort((a,b)=>a.seq-b.seq);
              const positions=Object.fromEntries(['base','feature-1','main-next','joined','consumer-1'].map(id=>{const n=reference.querySelector('.commit.'+CSS.escape(id));if(!n)throw Error('Missing reference commit '+id);const b=n.getBBox();return [id,{x:b.x+b.width/2,y:b.y+b.height/2}]}));
              const config=db.getConfig();return {direction:db.getDirection(),branches:db.getBranchesAsObjArray().map(b=>b.name),
                commits:commits.map((c,i)=>({id:i===5?null:c.id,branch:c.branch,parents:c.parents,type:c.customType??c.type,tags:c.tags})),positions,
                config:{main:config.mainBranchName,order:config.mainBranchOrder,parallel:config.parallelCommits,labels:config.showCommitLabel,branches:config.showBranches,rotate:config.rotateCommitLabel},
                nativeCommits:native.querySelectorAll('[data-cfx-role=git-commit]').length,nativeEdges:native.querySelectorAll('[data-cfx-role=git-commit-edge]').length,
                nativeActive:native.querySelectorAll('script,image,foreignObject').length};
            }
            """, new { source, dark, fontFamily = BrowserProofFonts.Family });
        await CaptureAsync(session.Page, "gitgraph-reference12-" + direction + "-" + parallel + "-" + width + (dark ? "-dark" : "-light"), svg, png, artifact.ToInterchangeJson(), observed);
        Assert.Equal(direction, observed.GetProperty("direction").GetString());
        Assert.Equal(new[] { "consumer", "feature", "trunk" }, observed.GetProperty("branches").EnumerateArray().Select(item => item.GetString()));
        var commits = observed.GetProperty("commits").EnumerateArray().ToArray(); Assert.Equal(6, commits.Length);
        Assert.Equal(new[] { "main-next", "feature-1" }, commits[3].GetProperty("parents").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(new[] { "consumer-1", "joined" }, commits[5].GetProperty("parents").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("consumer", commits[5].GetProperty("branch").GetString()); Assert.Equal(4, commits[5].GetProperty("type").GetInt32());
        var config = observed.GetProperty("config"); Assert.Equal("trunk", config.GetProperty("main").GetString()); Assert.Equal(3, config.GetProperty("order").GetInt32());
        Assert.Equal(parallel, config.GetProperty("parallel").GetBoolean()); Assert.True(config.GetProperty("labels").GetBoolean()); Assert.False(config.GetProperty("rotate").GetBoolean());
        var time = direction == "LR" ? "x" : "y"; var positions = observed.GetProperty("positions");
        var featureTime = positions.GetProperty("feature-1").GetProperty(time).GetDouble(); var mainTime = positions.GetProperty("main-next").GetProperty(time).GetDouble();
        if (parallel) Assert.Equal(featureTime, mainTime, 5); else Assert.True(direction == "BT" ? mainTime < featureTime : mainTime > featureTime);
        Assert.Equal(6, observed.GetProperty("nativeCommits").GetInt32()); Assert.Equal(7, observed.GetProperty("nativeEdges").GetInt32()); Assert.Equal(0, observed.GetProperty("nativeActive").GetInt32());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
