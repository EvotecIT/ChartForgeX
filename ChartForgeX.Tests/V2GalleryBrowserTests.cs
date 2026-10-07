using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Opt-in rendered-catalog contracts using the existing HtmlTinkerX-owned browser fixture.</summary>
public sealed class V2GalleryBrowserTests {
    [Theory]
    [InlineData(1200, 900)]
    [InlineData(390, 844)]
    public async Task InlineCatalog_PreservesScopedClipsFontsAndCompactLayout(int width, int height) {
        var url = Environment.GetEnvironmentVariable("CFX_V2_GALLERY_URL");
        if (!Enabled || string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Scheme != Uri.UriSchemeHttp)
            throw new InvalidOperationException("CFX_V2_GALLERY_URL must be a task-owned loopback HTTP URL.");
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", width, height);
        var page = session.Page;
        await page.GotoAsync(url);
        await page.EvaluateAsync("async () => { await document.fonts.ready; }");

        var defects = await page.EvaluateAsync<string[]>("""
            () => {
                const defects = [];
                const ids = new Set();
                for (const node of document.querySelectorAll('[id]')) {
                    if (ids.has(node.id)) defects.push('Duplicate id: ' + node.id);
                    ids.add(node.id);
                }
                for (const node of document.querySelectorAll('svg [clip-path]')) {
                    const reference = /url\(#([^)]+)\)/.exec(node.getAttribute('clip-path'));
                    const resolved = reference && document.getElementById(reference[1]);
                    if (!resolved || resolved.ownerSVGElement !== node.ownerSVGElement)
                        defects.push('Clip resolves outside its own SVG');
                }
                const faces = [...document.fonts].filter(face => face.family.replaceAll('"', '').replaceAll("'", '') === 'CFX Proof Carlito');
                if (!faces.some(face => face.weight === '400' && face.status === 'loaded')) defects.push('Regular fixture font not loaded');
                if (!faces.some(face => face.weight === '700' && face.status === 'loaded')) defects.push('Bold fixture font not loaded');
                if (document.documentElement.scrollWidth > innerWidth + 1) defects.push('Horizontal page overflow');
                for (const preview of document.querySelectorAll('.preview')) {
                    const svg = preview.querySelector('svg');
                    if (!svg) { defects.push('Missing chart-only SVG preview'); continue; }
                    const outer = preview.getBoundingClientRect(), inner = svg.getBoundingClientRect();
                    if (inner.left < outer.left || inner.right > outer.right + 1 || inner.top < outer.top || inner.bottom > outer.bottom + 1)
                        defects.push('SVG exceeds its uniform preview bounds');
                }
                return defects;
            }
            """);
        Assert.Empty(defects);
        Assert.True(await page.Locator(".tile").CountAsync() >= 4);
        AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "catalog-" + width + ".png") });
        }
    }
}
