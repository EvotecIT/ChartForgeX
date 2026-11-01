using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class HierarchyCaptionPolicyBrowserTests {
    [BrowserTheory]
    [InlineData(false, 420, VisualThemeMode.Light)]
    [InlineData(false, 420, VisualThemeMode.Dark)]
    [InlineData(false, 1100, VisualThemeMode.Light)]
    [InlineData(false, 1100, VisualThemeMode.Dark)]
    [InlineData(true, 420, VisualThemeMode.Light)]
    [InlineData(true, 420, VisualThemeMode.Dark)]
    [InlineData(true, 1100, VisualThemeMode.Light)]
    [InlineData(true, 1100, VisualThemeMode.Dark)]
    public async Task NativeArcAndTreemapPoliciesKeepBrowserGlyphInkInsideItsSourceMark(bool treemap, int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = treemap ? TreemapCaptionPoliciesRecipe.Create(mode: mode) : SunburstCurvedLabelsRecipe.Create(mode: mode);
        var family = BrowserProofFonts.Family;
        var prepared = chart.Prepare(HierarchyCaptionPolicyTests.Context(width, mode, family));
        var expected = chart.Series[0].HierarchyItems.Select(item => new { id = item.Id, label = item.Label, parent = item.ParentId, value = item.Value }).ToArray();
        await using var session = await BrowserProofFonts.OpenAsync(prepared.ToSvg(), width, 520);
        var pageErrors = new List<string>(); session.Page.PageError += (_, error) => pageErrors.Add(error);
        var defects = await session.Page.EvaluateAsync<string[]>("""
            ({treemap, expected, family}) => {
                const defects = [];
                const owners = [...document.querySelectorAll(treemap
                    ? "g[data-cfx-role='treemap-group'],g[data-cfx-role='treemap-tile']" : "g[data-cfx-role='sunburst-segment']")];
                if (owners.length !== expected.length) defects.push('source owner count');
                let arcCount = 0;
                for (const fact of expected) {
                    const owner = owners.find(node => node.getAttribute('data-cfx-target-id') === fact.id);
                    if (!owner) { defects.push(fact.id + ': owner missing'); continue; }
                    if (owner.getAttribute('data-cfx-full-label') !== fact.label) defects.push(fact.id + ': full source caption changed');
                    const mark = owner.querySelector(':scope > [data-cfx-role="' + (treemap
                        ? (owner.getAttribute('data-cfx-role') === 'treemap-group' ? 'treemap-group-mark' : 'treemap-tile-mark') : 'sunburst-segment-mark') + '"]');
                    if (!mark) { defects.push(fact.id + ': mark missing'); continue; }
                    const labels = [...owner.querySelectorAll(treemap ? ':scope [data-cfx-role="treemap-label"] text,:scope [data-cfx-role="treemap-group-label"] text'
                        : ':scope [data-cfx-role="sunburst-label"] text')].filter(text => text.closest('g[data-cfx-target-id]') === owner);
                    const arc = owner.querySelector(':scope [data-cfx-caption-placement="arc"]');
                    if (arc) {
                        arcCount++;
                        if (labels.map(text => text.textContent).join('') !== fact.label || arc.getAttribute('data-cfx-description') !== fact.label)
                            defects.push(fact.id + ': arc caption incomplete');
                    }
                    for (const text of labels) {
                        const style = getComputedStyle(text), box = text.getBBox();
                        if (!style.fontFamily.includes(family)) defects.push(fact.id + ': proof font lost');
                        if (parseFloat(style.fontSize) < 11 || parseFloat(style.fontSize) > (treemap ? 14 : 13)) defects.push(fact.id + ': explicit font bound');
                        if (box.width === 0 || box.height === 0) continue;
                        const matrix = mark.getCTM().inverse().multiply(text.getCTM());
                        for (const x of [box.x, box.x + box.width]) for (const y of [box.y, box.y + box.height]) {
                            const point = new DOMPoint(x, y).matrixTransform(matrix);
                            if (!mark.isPointInFill(point)) defects.push(fact.id + ': browser glyph crosses native mark');
                        }
                    }
                }
                if (!treemap && arcCount === 0) defects.push('no curved caption was painted');
                if (document.querySelector('script,foreignObject,iframe')) defects.push('active markup');
                return defects;
            }
            """, new { treemap, expected, family });
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var stem = (treemap ? "treemap-caption-policy-" : "sunburst-curved-policy-") + width + "-" + mode;
            await File.WriteAllTextAsync(Path.Combine(directory, stem + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, stem + ".native.png"), prepared.ToPng());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, stem + ".browser.png") });
            await File.WriteAllTextAsync(Path.Combine(directory, stem + ".json"), JsonSerializer.Serialize(new { width, mode, treemap, expected, defects, pageErrors, session.ConsoleLog }));
        }
        Assert.Empty(defects); Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }
}
