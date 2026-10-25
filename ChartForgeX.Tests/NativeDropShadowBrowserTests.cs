using System.Text.Json;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class NativeDropShadowBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task WholeNativeChartAndFiniteSourceShadowStayVisibleWithActualAlphaCoverage(bool dark, bool compact) {
        if (!Enabled) return;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var chart = NativeDropShadowExample.Create(compact ? "compact" : "wide", mode);
        var nativeProbe = ShadowFilterFixture.Source().CaptureLayer(new VisualDropShadow(ChartColor.FromRgba(0, 0, 255, 128), 4, 2, 0,
            spread: 1, orderedDither: false), ShadowFilterFixture.Options);
        var source = NativeDropShadowExample.FilterSource();
        var background = dark ? "#0f172a" : "#f8fafc"; var foreground = dark ? "#f8fafc" : "#0f172a";
        var html = "<!doctype html><html><head><meta charset='utf-8'><style>body{margin:0;padding:16px;background:" + background
            + ";color:" + foreground + ";font:16px system-ui}section{margin:16px 0;overflow:auto}.probes{display:flex;gap:24px;flex-wrap:wrap}.probes svg{width:256px;height:192px}h1{font-size:20px}</style></head><body>"
            + "<h1>Captured native chart and finite source filter</h1><section id='native-chart'>" + chart.Visual.ToSvg()
            + "</section><div class='probes'><section id='native-probe'><h2>Native pixels</h2>" + nativeProbe.Visual.ToSvg()
            + "</section><section id='source-probe'><h2>Source filter</h2>" + source + "</section></div></body></html>";
        var errors = new List<string>();
        await using var session = await OpenAsync(html, compact ? 390 : 1100, 920, configurePage: page => page.PageError += (_, message) => errors.Add(message));
        var page = session.Page; await page.WaitForSelectorAsync("#native-chart svg image");
        var observed = await page.EvaluateAsync<JsonElement>("""
            async () => {
              async function sample(selector) {
                const svg=document.querySelector(selector),copy=svg.cloneNode(true);copy.setAttribute('xmlns','http://www.w3.org/2000/svg');
                const uri=URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(copy)],{type:'image/svg+xml'}));
                try {const image=new Image();await new Promise((resolve,reject)=>{image.onload=resolve;image.onerror=reject;image.src=uri});
                  const canvas=document.createElement('canvas');canvas.width=svg.viewBox.baseVal.width||svg.width.baseVal.value;canvas.height=svg.viewBox.baseVal.height||svg.height.baseVal.value;
                  const context=canvas.getContext('2d');context.drawImage(image,0,0);return [[24,17],[35,21],[38,21]].map(([x,y])=>[...context.getImageData(x,y,1,1).data]);
                } finally {URL.revokeObjectURL(uri)}
              }
              const chart=document.querySelector('#native-chart svg'),b=chart.getBoundingClientRect();
              return {native:await sample('#native-probe svg'),source:await sample('#source-probe svg'),chart:{width:b.width,height:b.height,
                images:chart.querySelectorAll('image').length,targets:chart.querySelectorAll('[data-cfx-target-kind]').length,
                active:chart.querySelectorAll('script,foreignObject').length},sourceFilters:document.querySelectorAll('#source-probe filter').length};
            }
            """);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "native-shadow-" + (compact ? "compact" : "wide") + (dark ? "-dark" : "-light"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            File.WriteAllText(stem + ".native.svg", chart.Visual.ToSvg()); File.WriteAllBytes(stem + ".native.png", chart.Visual.ToPng());
            File.WriteAllText(stem + ".source.svg", source); File.WriteAllText(stem + ".observed.json", JsonSerializer.Serialize(new { observed, errors, session.ConsoleLog }));
        }
        var expected = new[] { new[] { 255, 0, 0, 128 }, new[] { 0, 0, 255, 64 }, new[] { 0, 0, 0, 0 } };
        foreach (var role in new[] { "native", "source" }) {
            var samples = observed.GetProperty(role).EnumerateArray().ToArray(); Assert.Equal(3, samples.Length);
            for (var index = 0; index < 3; index++) Assert.Equal(expected[index], samples[index].EnumerateArray().Select(item => item.GetInt32()));
        }
        Assert.True(observed.GetProperty("chart").GetProperty("width").GetDouble() > 0);
        Assert.True(observed.GetProperty("chart").GetProperty("height").GetDouble() > 0);
        Assert.Equal(1, observed.GetProperty("chart").GetProperty("images").GetInt32());
        Assert.Equal(0, observed.GetProperty("chart").GetProperty("targets").GetInt32());
        Assert.Equal(0, observed.GetProperty("chart").GetProperty("active").GetInt32());
        Assert.Equal(1, observed.GetProperty("sourceFilters").GetInt32()); Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}
