using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Report-style responsive matrices retain their native gutters and legend bands when the panel resizes.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class ResponsiveMatrixLayoutBrowserTests {
    [BrowserTheory]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public async Task RowLabelsCellsAndLegendStayInsideTheirBandsAcrossWideCompactAndReturn(bool categorical, bool bottom, bool compressed) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(1200, 320).WithHeader(false)
            .WithXLabels("00:00", "00:15", "00:30", "00:45")
            .WithLegendPosition(bottom ? ChartLegendPosition.Bottom : ChartLegendPosition.TopLeft);
        if (categorical) {
            var captions = new[] { "Maintenance", "No data", "Below 50%", "50-90%", "90-99.9%", "99.9-100%" };
            chart.WithStateCategories(captions.Select((caption, index) => new ChartStateCategory("s" + index, caption, ChartColor.FromHex("#258960"))).ToArray())
                .AddHeatmapCategoryRow("dc-one", new ChartHeatmapCell("s5"), new ChartHeatmapCell("s4"), new ChartHeatmapCell("s3"), new ChartHeatmapCell("s1"));
        } else {
            chart.AddHeatmapRow("dc-one", new[] { new ChartPoint(1, 99.9), new ChartPoint(2, 95), new ChartPoint(3, 80), new ChartPoint(4, 60) });
        }
        var html = "<html><body style='margin:0'><div id='panel' style='width:1120px'>" +
            new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600, compressLayouts: compressed) + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1440, 900);
        foreach (var width in new[] { 1120, 320, 779, 1120 }) {
            await session.Page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await session.Page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            var violations = await session.Page.EvaluateAsync<string[]>("""
                () => {
                    const svg=document.querySelector('[data-cfx-responsive-chart] svg'),frame=svg.getBoundingClientRect(),errors=[];
                    const rects=role=>[...svg.querySelectorAll('[data-cfx-role='+role+']')].filter(n=>getComputedStyle(n).display!=='none').map(n=>({text:n.textContent,box:n.getBoundingClientRect()}));
                    const labels=rects('heatmap-row-label'),cells=rects('heatmap-cell'),legend=rects('legend-entry');
                    if(labels.length!==1||labels[0].text!=='dc-one')errors.push('row identity');
                    if(cells.length!==4)errors.push('four source buckets');
                    for(const item of [...labels,...cells,...legend]) {
                        const b=item.box;
                        if(b.left<frame.left-.5||b.right>frame.right+.5||b.top<frame.top-.5||b.bottom>frame.bottom+.5)errors.push('outside SVG: '+item.text);
                    }
                    for(const l of legend)for(const c of cells) {
                        const a=l.box,b=c.box;
                        if(a.left<b.right-.5&&a.right>b.left+.5&&a.top<b.bottom-.5&&a.bottom>b.top+.5)errors.push('legend overlaps bucket: '+l.text);
                    }
                    if(labels.length&&cells.length&&labels[0].box.right>Math.min(...cells.map(c=>c.box.left))+.5)errors.push('row label overlaps cells');
                    const columns=rects('heatmap-column-label');
                    if(columns.length!==4)errors.push('four visible bucket captions');
                    columns.forEach((label,index)=>{
                        const cell=cells[index]?.box,b=label.box;
                        if(cell&&Math.abs(b.x+b.width/2-cell.x-cell.width/2)>2)errors.push('bucket caption is not centered: '+label.text);
                    });
                    return errors;
                }
                """);
            Assert.Empty(violations);
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(directory)) {
                Directory.CreateDirectory(directory);
                await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, $"responsive-matrix-{categorical}-{bottom}-{compressed}-{width}.png") });
            }
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
