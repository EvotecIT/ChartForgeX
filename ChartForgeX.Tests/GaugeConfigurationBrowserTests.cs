using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Observes configured gauge glyphs and their paired native export in real browser viewports.</summary>
public sealed class GaugeConfigurationBrowserTests {
    public static IEnumerable<object[]> Layouts() {
        foreach (var layout in new[] { "semicircle", "needle", "full-circle", "vertical" })
            foreach (var dark in new[] { false, true })
                foreach (var width in new[] { 390, 1440 }) yield return new object[] { layout, dark, width };
    }

    [BrowserTheory]
    [MemberData(nameof(Layouts))]
    public async Task ConfiguredGlyphsStayAnchoredAndContainedBesideTheirNativePng(string layout, bool dark, int viewportWidth) {
        if (!Enabled) return;
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        var chart = GaugeConfigurationRenderingTests.Create(layout, dark, viewportWidth == 390)
            .WithTheme(VisualTheme.Graphite().WithFontFamily("CFX Gauge Carlito"), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithPngFont(fontPath);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = "<!doctype html><html><head><style>@font-face{font-family:'CFX Gauge Carlito';src:url(data:font/ttf;base64,"
            + Convert.ToBase64String(File.ReadAllBytes(fontPath)) + ") format('truetype');font-weight:400}body{margin:12px;background:"
            + (dark ? "#17191d" : "#fff") + "}svg,img{display:block;max-width:100%;height:auto}</style></head><body>" + prepared.ToSvg()
            + "<img src='data:image/png;base64," + Convert.ToBase64String(prepared.ToPng()) + "'></body></html>";
        await using var session = await OpenAsync(html, viewportWidth, viewportWidth == 390 ? 780 : 960);
        await session.Page.EvaluateAsync("async () => { await document.fonts.ready; await document.querySelector('img').decode(); }");
        var defects = await session.Page.EvaluateAsync<string[]>("""
            () => {
              const svg = document.querySelector('svg'), outer = svg.getBoundingClientRect(), defects = [];
              const labels = [...svg.querySelectorAll('[data-cfx-role="gauge-min-label"],[data-cfx-role="gauge-max-label"],[data-cfx-role^="gauge-tick-label-"]')]
                .filter(node => !node.dataset.cfxRole.endsWith('-source'));
              const ticks = svg.querySelectorAll('[data-cfx-role="gauge-tick"]');
              if (labels.length !== ticks.length) defects.push('A regular scale tick lost its caption');
              for (const label of labels) {
                const box = label.getBoundingClientRect();
                if (box.left < outer.left - .5 || box.right > outer.right + .5 || box.top < outer.top - .5 || box.bottom > outer.bottom + .5)
                  defects.push('Scale glyphs escape the native viewport');
                if (label.textContent.includes('…')) defects.push('A numeric scale caption was shortened');
              }
              const line = svg.querySelector('[data-cfx-role="gauge-needle"]');
              if (line) {
                const x0=line.x1.baseVal.value,y0=line.y1.baseVal.value,dx=line.x2.baseVal.value-x0,dy=line.y2.baseVal.value-y0;
                for (const role of ['gauge-label','gauge-title']) {
                  const box=svg.querySelector('[data-cfx-role="'+role+'"]').getBBox(); let lo=0,hi=1;
                  for (const [p,q] of [[-dx,x0-box.x],[dx,box.x+box.width-x0],[-dy,y0-box.y],[dy,box.y+box.height-y0]]) {
                    if (Math.abs(p)<1e-10) { if(q<0){lo=2;break;} } else { const bound=q/p;if(p<0)lo=Math.max(lo,bound);else hi=Math.min(hi,bound); }
                  }
                  if(lo<=hi)defects.push('Needle intersects '+role);
                }
              }
              const image=document.querySelector('img');
              if(image.naturalWidth!==Number(svg.getAttribute('width'))||image.naturalHeight!==Number(svg.getAttribute('height')))
                defects.push('SVG and native PNG dimensions differ');
              if(document.documentElement.scrollWidth>innerWidth+1)defects.push('Document overflows the viewport');
              return defects;
            }
            """);
        Assert.Empty(defects);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var name = "configured-" + layout + "-" + viewportWidth + "-" + (dark ? "dark" : "light");
            await session.Page.ScreenshotAsync(new() { Path = Path.Combine(output, name + ".png"), FullPage = true });
            await File.WriteAllTextAsync(Path.Combine(output, name + ".html"), html);
        }
        AssertNoConsoleErrors(session);
    }
}
