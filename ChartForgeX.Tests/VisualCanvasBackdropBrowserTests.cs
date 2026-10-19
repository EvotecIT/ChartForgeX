using System.Text.Json;
using ChartForgeX.Composition;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class VisualCanvasBackdropBrowserTests {
    [BrowserTheory]
    [InlineData(VisualThemeMode.Light, 960)]
    [InlineData(VisualThemeMode.Dark, 640)]
    public async Task AuthoredHorizonAndExplicitOrDisabledHaloRenderInBothStaticOutputs(VisualThemeMode mode, int width) {
        if (!Enabled) return;
        var theme = VisualTheme.Graphite();
        var horizon = VisualCanvas.Create(800, 400).WithTheme(theme, mode).WithBackground(ChartColor.White)
            .WithBackdrop(VisualCanvasBackdropStyle.TechHorizon).ConfigureStyle(style => style.TechHorizonFill = ChartColor.Black);
        var halo = VisualCanvas.Create(200, 160).WithTheme(theme, mode).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddHeroBadge(40, 30, 120, 90, "")
            .ConfigureStyle(style => style.HeroBadgeGlowColor = ChartColor.FromHex("#D01BCC"));
        var horizonSvg = horizon.ToSvg(); var horizonPng = horizon.ToPng();
        var haloSvg = halo.ToSvg(); var haloPng = halo.ToPng();
        halo.ConfigureStyle(style => style.HeroBadgeGlowColor = ChartColor.Transparent);
        var clearSvg = halo.ToSvg(); var clearPng = halo.ToPng();
        static string Section(string id, string title, string svg, byte[] png) =>
            "<section id='" + id + "'><h2>" + title + "</h2><div class='pair'><div>SVG" + svg
            + "</div><div>Native PNG<img alt='" + title + " native PNG' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></div></div></section>";
        var html = "<!doctype html><html><head><style>body{margin:12px;font:14px sans-serif;background:" + theme.Resolve(mode).Background.ToCss()
            + ";color:" + theme.Resolve(mode).Foreground.ToCss() + "}main{max-width:1600px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}"
            + ".pair>div{min-width:0}svg,img{display:block;max-width:100%;height:auto}#halo .pair,#clear .pair{grid-template-columns:200px 200px}</style></head><body><main>"
            + Section("horizon", "Technology horizon", horizonSvg, horizonPng)
            + Section("halo", "Explicit magenta halo", haloSvg, haloPng)
            + Section("clear", "Transparent halo", clearSvg, clearPng) + "</main></body></html>";
        await using var session = await OpenAsync(html, width, 1000);
        var receipt = await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              await Promise.all([...document.images].map(image => image.decode()));
              const raster = async (selector, vector) => {
                let image = document.querySelector(selector);
                if (vector) {
                  const svg = image.outerHTML;
                  image = new Image();
                  image.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svg)));
                  await image.decode();
                }
                const canvas = document.createElement('canvas');
                canvas.width = image.naturalWidth; canvas.height = image.naturalHeight;
                const context = canvas.getContext('2d'); context.drawImage(image, 0, 0);
                return { context, width:canvas.width, height:canvas.height };
              };
              const vector = await raster('#horizon svg', true), native = await raster('#horizon img', false);
              const firstDark = (paint,x) => {
                for(let y=270;y<paint.height;y++) if(paint.context.getImageData(x,y,1,1).data[0]<128) return y;
                return -1;
              };
              const colored = await raster('#halo svg',true), clear = await raster('#clear svg',true);
              return { width:innerWidth, overflow:document.documentElement.scrollWidth>innerWidth,
                horizon:[160,320,592,672].map(x=>({x,svg:firstDark(vector,x),native:firstDark(native,x)})),
                colored:[...colored.context.getImageData(37,75,1,1).data],
                clear:[...clear.context.getImageData(37,75,1,1).data] };
            }
            """);
        Assert.False(receipt.GetProperty("overflow").GetBoolean());
        foreach (var probe in receipt.GetProperty("horizon").EnumerateArray())
            Assert.InRange(Math.Abs(probe.GetProperty("svg").GetInt32() - probe.GetProperty("native").GetInt32()), 0, 1);
        var color = receipt.GetProperty("colored").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        Assert.True(color[0] > color[1] + 40 && color[2] > color[1] + 40 && color[3] > 4, "SVG halo must use the authored magenta paint.");
        Assert.Equal(0, receipt.GetProperty("clear")[3].GetInt32());
        await CaptureContractAsync(session.Page, "canvas-backdrop-" + mode + "-" + width,
            new { mode, receipt }, horizonSvg, horizonPng, haloSvg, haloPng, clearSvg, clearPng);
        AssertNoConsoleErrors(session);
    }

    private static async Task CaptureContractAsync(IPage page, string name, object receipt,
        string horizonSvg, byte[] horizonPng, string haloSvg, byte[] haloPng, string clearSvg, byte[] clearPng) {
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var path = Path.Combine(captures, name);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(path + ".json", JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var pair in new[] { ("horizon", horizonSvg, horizonPng), ("halo", haloSvg, haloPng), ("clear", clearSvg, clearPng) }) {
            await File.WriteAllTextAsync(path + "." + pair.Item1 + ".svg", pair.Item2);
            await File.WriteAllBytesAsync(path + "." + pair.Item1 + ".native.png", pair.Item3);
        }
    }
}
