using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(390, false)] [InlineData(1100, false)]
    [InlineData(390, true)] [InlineData(1100, true)]
    public async Task PinnedFiniteShadowFunctionsAndInsertedGlyphFilterKeepSeparateSurfaceRoles(int width, bool dark) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var body = File.ReadAllText(Path.Combine(TestRepository.Root, "Build", "PackageConsumers", "Adapters", "sequence-retained-surface-shadows.mmd"));
        var source = "%%{init: " + JsonSerializer.Serialize(new { themeVariables = new {
            dropShadow = "drop-shadow(1px 2px 2px rgba(0,0,0,0.25))" } }) + "}%%\n" + body;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = mode, FontFamily = BrowserProofFonts.Family },
            Sequence = new MermaidSequenceRenderOptions { Width = width, Height = 900 }
        });
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var model = Assert.IsType<SequenceArtifact>(artifact.Model);
        var prepared = model.Prepare(RetainedSurfaceFixture.Context(model));
        var svg = prepared.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        var png = prepared.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(
            "<style>section{overflow:auto;max-width:100%}</style><section id='reference'></section><section id='native'>" + svg + "</section>", width, 1100);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark,
            new Dictionary<string, int> { { "sequence-participant", 2 } },
            dark ? "redux-dark-color" : "redux-color", "neo");
        var effects = await session.Page.EvaluateAsync<JsonElement>("""
            () => Object.fromEntries(['reference','native'].map(id=>{
              const s=document.querySelector('#'+id+' svg');
              return [id,{floods:[...s.querySelectorAll('feFlood')].map(e=>({opacity:e.getAttribute('flood-opacity'),color:e.getAttribute('flood-color')})),
                notes:[...s.querySelectorAll(id==='native'?'[data-cfx-role="sequence-note"]':'.note')].map(e=>({filter:getComputedStyle(e).filter,text:e.textContent})),
                text:[...s.querySelectorAll('text')].map(e=>e.textContent),scripts:s.querySelectorAll('script,foreignObject').length}];
            }))
            """);
        var stem = "retained-shadow-reference12-" + width + "-" + dark;
        await CaptureAsync(session.Page, stem, svg, png, artifact.ToInterchangeJson(), observed);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, stem + ".effects.json"), effects.GetRawText());
        foreach (var owner in new[] { "reference", "native" }) {
            Assert.Contains(effects.GetProperty(owner).GetProperty("floods").EnumerateArray(), item => item.GetProperty("opacity").GetString() == "0.06");
            Assert.Contains("Review", observed.GetProperty(owner + "Text").GetString());
            Assert.Contains("Retain audit facts", observed.GetProperty(owner + "Text").GetString());
            Assert.Equal(0, effects.GetProperty(owner).GetProperty("scripts").GetInt32());
        }
        Assert.Equal(.25, model.Paint.NoteShadow!.Opacity);
        Assert.Equal(.06, model.Participants[0].SurfaceShadow!.Opacity);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
