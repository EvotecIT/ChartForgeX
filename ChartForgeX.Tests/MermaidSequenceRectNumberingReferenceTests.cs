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
    public async Task CurrentReferenceObservesRectPaintAndExactAuthoredNumberingOccurrences(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidSequenceRectNumberingTests.Source(dark);
        var rendered = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family },
            Sequence = new MermaidSequenceRenderOptions { Width = width - 32, Height = 620 }
        });
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + "</h2><section id='reference'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark, new Dictionary<string, int> { ["sequence-message"] = 7, ["sequence-block-background"] = 3 });
        var numbering = await session.Page.EvaluateAsync<JsonElement>("""
            () => ({numbers:[...document.querySelectorAll('#reference .sequenceNumber')].map(text=>text.textContent),nativeNumbers:[...document.querySelectorAll('#native [data-cfx-role="sequence-number"] text')].map(text=>text.textContent),
                rectangles:[...document.querySelectorAll('#reference rect.rect')].map(rect=>rect.getAttribute('fill'))})
            """);
        var name = "sequence-rect-numbering-reference12-" + width + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output)
            File.WriteAllText(Path.Combine(output, name + ".numbering.json"), numbering.GetRawText());
        Assert.Equal(new[] { "5.5", "6.5", "10", "11", "12" }, numbering.GetProperty("numbers").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "5.5", "6.5", "10", "11", "12" }, numbering.GetProperty("nativeNumbers").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(3, numbering.GetProperty("rectangles").GetArrayLength());
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
        Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32());
        foreach (var label in new[] { "Before", "Started", "Hidden", "Resumed", "Reset", "Continued", "Default color" })
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(label, observed.GetProperty(surface).GetString());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
