using System.Text.Json;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Independent SVG glyph bounds preserve complete hierarchy captions with the native fixture face.</summary>
public sealed class HierarchyCompleteCaptionBrowserTests {
    [BrowserTheory]
    [InlineData(false, VisualThemeMode.Light)]
    [InlineData(false, VisualThemeMode.Dark)]
    [InlineData(true, VisualThemeMode.Light)]
    [InlineData(true, VisualThemeMode.Dark)]
    public async Task CompleteCaptionsStayInsideTheirMarksWithTheNativeFont(bool treemap, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = treemap ? HierarchyCompleteCaptionTests.Treemap() : HierarchyCompleteCaptionTests.Tree(true);
        if (!treemap) chart.Series[0].WithPointLabel(0, "Authored operations");
        int width = treemap ? 390 : 760;
        var family = BrowserProofFonts.Family;
        var prepared = chart.Prepare(HierarchyCompleteCaptionTests.Context(width, 420, mode, FontSpec.FromFamily(family)));
        var expected = chart.Series[0].Nodes.Select((node, index) => new {
            id = "series-0-node-" + Uri.EscapeDataString(node.Id), source = node.Label,
            caption = index < chart.Series[0].PointLabels.Count ? chart.Series[0].PointLabels[index] ?? node.Label : node.Label
        }).ToArray();
        await using var session = await BrowserProofFonts.OpenAsync(prepared.ToSvg(), width, 420);
        var pageErrors = new List<string>(); session.Page.PageError += (_, error) => pageErrors.Add(error);
        var defects = await session.Page.EvaluateAsync<string[]>("""
            ({expected, family}) => {
                const defects = [];
                const owners = [...document.querySelectorAll("g[data-cfx-role='tree-node'],g[data-cfx-role='treemap-group'],g[data-cfx-role='treemap-tile']")];
                if (owners.length !== expected.length) defects.push('Missing source nodes');
                for (const fact of expected) {
                    const owner = owners.find(node => node.getAttribute('data-cfx-source-id') === fact.id);
                    if (!owner) { defects.push(fact.id + ': missing owner'); continue; }
                    const role = owner.getAttribute('data-cfx-role');
                    const labelRole = role === 'tree-node' ? 'tree-node-label' : role === 'treemap-group' ? 'treemap-group-label' : 'treemap-label';
                    const markRole = role === 'tree-node' ? 'tree-node-mark' : role === 'treemap-group' ? 'treemap-group-mark' : 'treemap-tile-mark';
                    const mark = owner.querySelector(':scope > [data-cfx-role="' + markRole + '"]');
                    const texts = [...owner.querySelectorAll('[data-cfx-role="' + labelRole + '"] text')];
                    if (!mark || !texts.length) { defects.push(fact.id + ': missing painted caption'); continue; }
                    const value = owner.getAttribute('data-cfx-formatted-value');
                    const wanted = fact.caption + (value == null ? '' : role === 'treemap-group' ? ' · ' + value : ' ' + value);
                    if (texts.map(text => text.textContent).join(' ') !== wanted) defects.push(fact.id + ': incomplete caption');
                    if (owner.getAttribute('data-cfx-label') !== fact.source || owner.getAttribute('data-cfx-full-label') !== fact.caption)
                        defects.push(fact.id + ': source name or authored caption was lost');
                    const bounds = mark.getBBox();
                    for (const text of texts) {
                        const style = getComputedStyle(text), box = text.getBBox();
                        if (parseFloat(style.fontSize) !== 12 || !style.fontFamily.includes(family)) defects.push(fact.id + ': requested type changed');
                        const matrix = mark.getCTM().inverse().multiply(text.getCTM());
                        for (const x of [box.x, box.x + box.width]) for (const y of [box.y, box.y + box.height]) {
                            const point = new DOMPoint(x, y).matrixTransform(matrix);
                            if (point.x < bounds.x - .5 || point.x > bounds.x + bounds.width + .5
                                || point.y < bounds.y - .5 || point.y > bounds.y + bounds.height + .5) defects.push(fact.id + ': glyph crosses its mark');
                        }
                    }
                }
                return defects;
            }
            """, new { expected, family });
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var stem = (treemap ? "treemap" : "tree") + "-complete-captions-" + mode;
            await File.WriteAllTextAsync(Path.Combine(directory, stem + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, stem + ".native.png"), prepared.ToPng());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, stem + ".browser.png") });
            await File.WriteAllTextAsync(Path.Combine(directory, stem + ".json"), JsonSerializer.Serialize(new {
                width, height = 420, family, captions = expected.Length, defects, pageErrors,
                consoleErrors = session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).ToArray()
            }));
        }
        Assert.Empty(defects); Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }
}
