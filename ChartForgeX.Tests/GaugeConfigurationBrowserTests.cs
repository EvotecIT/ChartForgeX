using System.Text.Json;
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

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NearbyHorizontalExplicitTicksKeepReadableBrowserGlyphsAndTheirNativeFacts(bool dark) {
        if (!Enabled) return;
        var chart = GaugeHorizontalExplicitScaleTests.Create(dark, 1, "Near minimum").WithFontFamily("CFX Gauge Carlito");
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context);
        var omitted = Assert.Single(prepared.Regions, region => region.Id == "series-0-gauge-scale-1");
        Assert.Equal("Near minimum", omitted.Label);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "gauge.label-overflow");
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var html = "<!doctype html><html><head><style>@font-face{font-family:'CFX Gauge Carlito';src:url(data:font/ttf;base64,"
            + Convert.ToBase64String(File.ReadAllBytes(chart.Options.PngFontPath!)) + ") format('truetype');font-weight:400}body{margin:12px;background:"
            + (dark ? "#17191d" : "#fff") + "}svg,img{display:block;max-width:100%;height:auto}</style></head><body>" + svg
            + "<img src='data:image/png;base64," + Convert.ToBase64String(png) + "'></body></html>";
        await using var session = await OpenAsync(html, 390, 440);
        await session.Page.EvaluateAsync("async () => { await document.fonts.ready; await document.querySelector('img').decode(); }");
        var defects = await session.Page.EvaluateAsync<string[]>("""
            axisSize => {
              const svg = document.querySelector('svg'), view = svg.viewBox.baseVal, defects = [];
              const labels = [...svg.querySelectorAll('text')].filter(node => {
                const role = node.closest('[data-cfx-role]')?.dataset.cfxRole;
                return role === 'gauge-min-label' || role === 'gauge-max-label' || role?.startsWith('gauge-tick-label-');
              });
              if (labels.length !== 2 || labels.map(node => node.textContent).join('|') !== 'Minimum|Maximum')
                defects.push('The noncolliding explicit captions were lost or the crowded caption was painted');
              const boxes = labels.map(node => node.getBBox());
              for (const [index, node] of labels.entries()) {
                const box = boxes[index], style = getComputedStyle(node);
                if (box.width <= 0 || box.height <= 0 || parseFloat(style.fontSize) < axisSize)
                  defects.push('An explicit caption is unreadable');
                if (!style.fontFamily.includes('CFX Gauge Carlito')) defects.push('The fixture face was lost');
                if (box.x < view.x - .5 || box.x + box.width > view.x + view.width + .5
                    || box.y < view.y - .5 || box.y + box.height > view.y + view.height + .5)
                  defects.push('An explicit caption escapes the SVG viewport');
                for (let other = index + 1; other < boxes.length; other++) {
                  const next = boxes[other];
                  if (box.x < next.x + next.width && box.x + box.width > next.x
                      && box.y < next.y + next.height && box.y + box.height > next.y)
                    defects.push('Painted explicit glyphs overlap');
                }
              }
              const track = svg.querySelector('[data-cfx-role="gauge-track"]').getBBox();
              const ticks = [...svg.querySelectorAll('[data-cfx-role="gauge-tick"]')];
              const expected = [track.x, track.x + track.width * .01, track.x + track.width];
              if (ticks.length !== 3 || ticks.some((tick, index) => Math.abs(tick.getBBox().x - expected[index]) > .01))
                defects.push('Explicit ticks moved away from 0, 1 and 100');
              const image = document.querySelector('img');
              if (image.naturalWidth !== 360 || image.naturalHeight !== 180
                  || image.naturalWidth !== Number(svg.getAttribute('width')) || image.naturalHeight !== Number(svg.getAttribute('height')))
                defects.push('SVG and native PNG dimensions differ');
              if (!document.fonts.check(axisSize + 'px "CFX Gauge Carlito"')) defects.push('The fixture face did not load');
              if (document.documentElement.scrollWidth > innerWidth + 1) defects.push('The document overflows the viewport');
              return defects;
            }
            """, context.Theme.Typography.AxisSize);
        Assert.Empty(defects);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var name = "configured-horizontal-explicit-390-" + (dark ? "dark" : "light");
            await session.Page.ScreenshotAsync(new() { Path = Path.Combine(output, name + ".png"), FullPage = true });
            await File.WriteAllTextAsync(Path.Combine(output, name + ".html"), html);
            await File.WriteAllTextAsync(Path.Combine(output, name + ".svg"), svg);
            await File.WriteAllBytesAsync(Path.Combine(output, name + ".native.png"), png);
            await File.WriteAllTextAsync(Path.Combine(output, name + ".runtime.json"), JsonSerializer.Serialize(new {
                omittedFact = new { omitted.Id, omitted.Role, omitted.Label },
                diagnostics = prepared.Diagnostics.Select(diagnostic => new { diagnostic.Code, diagnostic.Message }), defects
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        AssertNoConsoleErrors(session);
    }
}
