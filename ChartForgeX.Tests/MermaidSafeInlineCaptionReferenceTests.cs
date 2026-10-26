using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData("flowchart", false, 390)] [InlineData("flowchart", true, 1100)]
    [InlineData("requirement", false, 390)] [InlineData("requirement", true, 1100)]
    [InlineData("mindmap", false, 390)] [InlineData("mindmap", true, 1100)]
    public async Task CurrentPinnedSafeEmphasisIsObservedBesideSelfContainedNativeInk(string family, bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json"))); Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidSafeInlineCaptionTests.Source(family, "Normal **bold** and *italic*<br>next");
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions { Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family } });
        Assert.Empty(result.Diagnostics); var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box}section{overflow:auto;max-width:100%;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned safe emphasis</h2><section id='reference'></section><h2>Native SVG</h2><section id='native'>" + svg
            + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark, new Dictionary<string, int> { ["topology-node"] = 1 });
        var caption = await session.Page.EvaluateReferenceJsonAsync("""
            () => {
              const reference=document.querySelector('#reference svg'),native=document.querySelector('#native svg'),frame=native.getBoundingClientRect();
              const runs=[...native.querySelectorAll('[data-cfx-native-text-ink]')].map(node=>{const r=node.getBoundingClientRect();return {text:node.getAttribute('aria-label'),width:r.width,height:r.height,
                inside:r.left>=frame.left-1&&r.right<=frame.right+1&&r.top>=frame.top-1&&r.bottom<=frame.bottom+1}});
              const marked=[...reference.querySelectorAll('strong,em,b,i,tspan,span')].map(node=>({tag:node.tagName.toLowerCase(),text:node.textContent,
                weight:getComputedStyle(node).fontWeight,style:getComputedStyle(node).fontStyle,width:node.getBoundingClientRect().width}));
              return {runs,marked,nativeCaptions:[...native.querySelectorAll('[data-cfx-caption]')].map(node=>node.dataset.cfxCaption),resources:native.querySelectorAll('[data-cfx-native-text-resources] path').length};
            }
            """);
        var name = "safe-native-emphasis-reference12-" + family + "-" + width + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) File.WriteAllText(Path.Combine(output, name + ".emphasis.json"), caption.GetRawText());
        Assert.Contains(caption.GetProperty("marked").EnumerateArray(), item => item.GetProperty("text").GetString() == "bold" && int.TryParse(item.GetProperty("weight").GetString(), out var weight) && weight >= 600 && item.GetProperty("width").GetDouble() > 0);
        Assert.Contains(caption.GetProperty("marked").EnumerateArray(), item => item.GetProperty("text").GetString() == "italic" && item.GetProperty("style").GetString() == "italic" && item.GetProperty("width").GetDouble() > 0);
        Assert.True(caption.GetProperty("resources").GetInt32() > 0); Assert.NotEmpty(caption.GetProperty("runs").EnumerateArray());
        Assert.All(caption.GetProperty("runs").EnumerateArray(), item => Assert.True(item.GetProperty("inside").GetBoolean()));
        Assert.Contains(caption.GetProperty("nativeCaptions").EnumerateArray(), item => item.GetString()!.Contains("Normal bold and italic\nnext", StringComparison.Ordinal));
        Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32()); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
