using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Observes regenerated full-circle scales beside their native exports in representative browser states.</summary>
public sealed class GaugeCircularScaleBrowserTests {
    [BrowserTheory]
    [InlineData(ChartGaugeForm.Arc, 97, true, false, false, 0)]
    [InlineData(ChartGaugeForm.Needle, 97, false, true, false, 0)]
    [InlineData(ChartGaugeForm.Arc, 96, true, true, false, 0)]
    [InlineData(ChartGaugeForm.Needle, 9, false, false, true, 0)]
    [InlineData(ChartGaugeForm.Arc, 97, false, false, false, 40)]
    [InlineData(ChartGaugeForm.Needle, 97, false, true, false, 60)]
    public async Task RegeneratedFullTurnsKeepUniformReadableSvgAndNativePng(ChartGaugeForm form, int requested,
        bool compact, bool dark, bool longFormat, double precisionFontSize) {
        if (!Enabled) return;
        var width = longFormat ? 520 : compact ? 360 : 800;
        var height = longFormat ? 260 : compact ? 360 : 440;
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        var chart = (precisionFontSize > 0 ? GaugeCircularScaleTests.CreatePrecision(form, precisionFontSize)
                : GaugeCircularScaleTests.Create(form, requested, width, height, longFormat, dark))
            .WithTheme(VisualTheme.Graphite().WithFontFamily("CFX Circular Carlito"), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithPngFont(fontPath);
        if (precisionFontSize >= 60) chart.WithSize(520, 360); // Keep room for the registered face's authored 60 px captions.
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = "<!doctype html><html><head><style>@font-face{font-family:'CFX Circular Carlito';src:url(data:font/ttf;base64,"
            + Convert.ToBase64String(File.ReadAllBytes(fontPath)) + ") format('truetype');font-weight:400}body{margin:12px;background:"
            + (dark ? "#17191d" : "#fff") + "}svg,img{display:block;max-width:100%;height:auto}</style></head><body>"
            + prepared.ToSvg() + "<img src='data:image/png;base64," + Convert.ToBase64String(prepared.ToPng()) + "'></body></html>";
        await using var session = await OpenAsync(html, compact ? 390 : 1440, 960);
        await session.Page.EvaluateAsync("async () => { await document.fonts.ready; await document.querySelector('img').decode(); }");
        var defects = await session.Page.EvaluateAsync<string[]>("""
            options => {
              const svg=document.querySelector('svg'), outer=svg.getBoundingClientRect(), defects=[];
              const ticks=[...svg.querySelectorAll('[data-cfx-role="gauge-tick"]')];
              const labels=[...svg.querySelectorAll('[data-cfx-role="gauge-min-label"],[data-cfx-role="gauge-max-label"],[data-cfx-role^="gauge-tick-label-"]')]
                .filter(node=>!node.dataset.cfxRole.endsWith('-source'));
              if(ticks.length<2||ticks.length>(options.lowBudget?2:40))defects.push('Generated count exceeds the readable budget');
              if(labels.length!==ticks.length)defects.push('A regenerated tick lost its caption');
              if(options.precision&&new Set(labels.map(node=>node.textContent)).size!==labels.length)defects.push('Distinct default ticks share a caption');
              if(options.precision&&ticks.length===2&&labels.some(node=>node.textContent===svg.querySelector('[data-cfx-role="gauge"]').dataset.cfxGaugeTarget0FormattedValue))defects.push('A regenerated midpoint aliases its distinct target');
              const boxes=labels.map(node=>node.getBoundingClientRect());
              boxes.forEach((box,index)=>{
                if(box.left<outer.left-.5||box.right>outer.right+.5||box.top<outer.top-.5||box.bottom>outer.bottom+.5)defects.push('A scale caption escapes its viewport');
                if(labels[index].textContent.includes('...')||labels[index].textContent.includes('…'))defects.push('A numeric scale caption was shortened');
                if(boxes.slice(index+1).some(peer=>box.left<peer.right&&box.right>peer.left&&box.top<peer.bottom&&box.bottom>peer.top))defects.push('Scale captions overlap');
              });
              const image=document.querySelector('img');
              if(image.naturalWidth!==Number(svg.getAttribute('width'))||image.naturalHeight!==Number(svg.getAttribute('height')))defects.push('Paired dimensions differ');
              if(document.documentElement.scrollWidth>innerWidth+1)defects.push('The page overflows');
              return defects;
            }
            """, new { lowBudget = longFormat, precision = precisionFontSize > 0 });
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var name = "circular-" + form + "-" + requested + "-" + (compact ? "compact" : "wide") + "-" + (dark ? "dark" : "light")
                + (precisionFontSize > 0 ? "-precision" : "");
            await session.Page.ScreenshotAsync(new() { Path = Path.Combine(directory, name + ".png"), FullPage = true });
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
        }
        Assert.True(defects.Length == 0, string.Join("; ", defects.Concat(prepared.Diagnostics.Select(item => item.Code))));
        AssertNoConsoleErrors(session);
    }
}
