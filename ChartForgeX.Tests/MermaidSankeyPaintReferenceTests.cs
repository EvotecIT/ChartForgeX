using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData("gradient", "outlined", false, 390)] [InlineData("gradient", "outlined", true, 1100)]
    [InlineData("source", "legacy", false, 1100)] [InlineData("target", "outlined", true, 390)]
    [InlineData("#7954AA", "legacy", false, 390)]
    public async Task CurrentPinnedSankeyObservesEndpointLiteralNodeObjectAndLabelPaint(string policy, string labelStyle, bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidSankeyPaintTests.Source(policy, labelStyle); var options = MermaidSankeyPaintTests.Options(width, dark);
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics); var artifact = direct.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var body = "<style>body{padding:12px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:16px}section{max-width:100%;overflow:auto;margin-bottom:16px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + "</h2><section id='reference'></section><h2>Native SVG</h2><section id='native'>" + svg
            + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(body, width + 24, 1000); var errors = new List<string>(); session.Page.PageError += (_, message) => errors.Add(message);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async ({source,dark,fontFamily}) => {
              mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,theme:dark?'dark':'default',look:'classic'});
              const result=await mermaid.render('sankey-paint-reference',source);document.querySelector('#reference').innerHTML=result.svg;
              const reference=document.querySelector('#reference svg'),native=document.querySelector('#native svg');
              for(const svg of [reference,native]){const v=svg.viewBox.baseVal;svg.style.width=v.width+'px';svg.style.height=v.height+'px';svg.style.maxWidth='none'}
              await document.fonts.ready;await document.querySelector('#png').decode();
              const nodes=[...reference.querySelectorAll('.nodes .node')].map(n=>({id:n.__data__.id,fill:n.querySelector('rect').getAttribute('fill')}));
              return {nodes,links:[...reference.querySelectorAll('.links .link > path')].map(n=>({stroke:n.getAttribute('stroke'),value:n.__data__.value})),
                gradients:reference.querySelectorAll('.links linearGradient').length,outlined:reference.querySelectorAll('.sankey-label-bg').length,
                labels:[...reference.querySelectorAll('.node-labels text')].map(n=>n.textContent),
                nativeNodes:[...native.querySelectorAll('[data-cfx-role="sankey-node"]')].map(n=>n.getAttribute('data-cfx-target-id')),
                nativeWeights:[...native.querySelectorAll('[data-cfx-role="sankey-link"]')].map(n=>Number(n.getAttribute('data-cfx-value'))),
                nativeActive:native.querySelectorAll('script,image,foreignObject').length};
            }
            """, new { source, dark, fontFamily = BrowserProofFonts.Family });
        await CaptureAsync(session.Page, "sankey-reference12-" + (policy[0] == '#' ? "literal" : policy) + "-" + labelStyle + "-" + width + (dark ? "-dark" : "-light"), svg, png, artifact.ToInterchangeJson(), observed);
        var nodes = observed.GetProperty("nodes").EnumerateArray().ToArray();
        Assert.Equal(new[] { "Intake.eu[1]", "Review", "Ready 東京" }, nodes.Select(node => node.GetProperty("id").GetString()));
        Assert.Equal(new[] { "#2864B4", "#BB6428", "#279473" }.Select(value => value.ToLowerInvariant()), nodes.Select(node => node.GetProperty("fill").GetString()!.ToLowerInvariant()));
        Assert.Equal(new[] { 6d, 2d, 8d }, observed.GetProperty("links").EnumerateArray().Select(link => link.GetProperty("value").GetDouble()));
        Assert.Equal(policy == "gradient" ? 3 : 0, observed.GetProperty("gradients").GetInt32());
        Assert.Equal(labelStyle == "outlined" ? 3 : 0, observed.GetProperty("outlined").GetInt32());
        foreach (var node in nodes) Assert.Contains(node.GetProperty("id").GetString(), observed.GetProperty("labels").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(nodes.Select(node => node.GetProperty("id").GetString()), observed.GetProperty("nativeNodes").EnumerateArray().Select(node => node.GetString()));
        Assert.Equal(new[] { 6d, 2d, 8d }, observed.GetProperty("nativeWeights").EnumerateArray().Select(link => link.GetDouble()));
        Assert.Equal(0, observed.GetProperty("nativeActive").GetInt32()); Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
