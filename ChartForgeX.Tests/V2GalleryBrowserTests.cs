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
                for (const preview of [...document.querySelectorAll('.preview')].filter(node => node.getClientRects().length)) {
                    const svg = preview.querySelector('svg');
                    if (!svg) { defects.push('Missing chart-only SVG preview'); continue; }
                    const outer = preview.getBoundingClientRect(), inner = svg.getBoundingClientRect();
                    if (inner.left < outer.left || inner.right > outer.right + 1 || inner.top < outer.top || inner.bottom > outer.bottom + 1)
                        defects.push('SVG exceeds its uniform preview bounds');
                    const title = svg.querySelector(':scope > title');
                    if (!title || !title.textContent.trim() || title.textContent === 'ChartForgeX chart') defects.push('Preview lacks its example title');
                    for (const text of svg.querySelectorAll('text')) {
                        if (!text.textContent.trim()) continue;
                        const matrix = text.getScreenCTM();
                        const size = parseFloat(getComputedStyle(text).fontSize) * Math.hypot(matrix.a, matrix.b);
                        if (size < 10) defects.push('Preview text below 10 CSS pixels: ' + text.textContent + ' (' + size.toFixed(2) + ')');
                    }
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

        await page.Locator("[data-set-theme=dark]").ClickAsync();
        Assert.Equal("dark", await page.Locator("html").GetAttributeAsync("data-theme"));
        Assert.Equal("true", await page.Locator("[data-set-theme=dark]").GetAttributeAsync("aria-pressed"));
        Assert.Equal(0, await page.Locator(".preview[data-visual-theme=light]:visible").CountAsync());
        Assert.True(await page.Locator(".preview[data-visual-theme=dark]:visible").CountAsync() >= 4);
        await page.ReloadAsync();
        Assert.Equal("dark", await page.Locator("html").GetAttributeAsync("data-theme"));

        await page.Locator("[data-gallery-search]").FillAsync("heatmap");
        var visibleFamilies = await page.Locator("[data-gallery-family]:visible").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.galleryFamily)");
        Assert.NotEmpty(visibleFamilies);
        Assert.All(visibleFamilies, family => Assert.Contains("heatmap", family, StringComparison.Ordinal));
        await page.Locator("[data-gallery-group=matrices-maps]").ClickAsync();
        Assert.True(await page.Locator("[data-gallery-family]:visible").CountAsync() > 0);
        await page.Locator("[data-gallery-search]").FillAsync("no-such-example");
        Assert.Equal(0, await page.Locator("[data-gallery-family]:visible").CountAsync());
        Assert.True(await page.Locator("[data-gallery-empty]").IsVisibleAsync());
        await page.Locator("[data-gallery-reset]").ClickAsync();
        Assert.False(await page.Locator("[data-gallery-empty]").IsVisibleAsync());

        await page.Locator("[data-gallery-search]").FillAsync("heatmap");
        var example = page.Locator("[data-gallery-family]:visible a.preview:visible").First;
        await example.ClickAsync();
        Assert.Equal("dark", await page.Locator("html").GetAttributeAsync("data-theme"));
        Assert.Equal(1, await page.Locator(".full:visible").CountAsync());
        Assert.Equal(width <= 600 ? "360" : "800", await page.Locator(".full[data-output-format=svg]:visible > svg").GetAttributeAsync("width"));
        if (width <= 600) {
            Assert.Contains("-compact-", page.Url, StringComparison.Ordinal);
            // Compact is a navigation default, while the explicit Standard choice stays usable.
            await page.Locator(".variant-nav").GetByText("Standard", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
            Assert.Equal("800", await page.Locator(".full[data-output-format=svg]:visible > svg").GetAttributeAsync("width"));
            await page.Locator(".variant-nav").GetByText("Compact", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
            Assert.Equal("360", await page.Locator(".full[data-output-format=svg]:visible > svg").GetAttributeAsync("width"));
        }
        Assert.Equal(1, await page.Locator("pre[data-visual-theme]:visible").CountAsync());
        await page.Locator("[data-set-output=png]").ClickAsync();
        Assert.Equal(1, await page.Locator(".full[data-output-format=png]:visible").CountAsync());
        Assert.Equal(0, await page.Locator(".full[data-output-format=svg]:visible").CountAsync());
        await page.ReloadAsync();
        Assert.Equal("png", await page.Locator("html").GetAttributeAsync("data-output-view"));
        var downloads = await page.Locator(".export-row a").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.getAttribute('href'))");
        Assert.All(downloads, href => Assert.Contains("-dark.", href, StringComparison.Ordinal));
        await page.Locator("[data-set-theme=light]").ClickAsync();
        Assert.Equal("light", await page.Locator("html").GetAttributeAsync("data-theme"));
        var lightDownloads = await page.Locator(".export-row a").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.getAttribute('href'))");
        Assert.All(lightDownloads, href => Assert.Contains("-light.", href, StringComparison.Ordinal));
        await page.Locator(".back-link").ClickAsync();
        Assert.Equal("heatmap", await page.Locator("[data-gallery-search]").InputValueAsync());
        Assert.True(await page.Locator("[data-gallery-family]:visible").CountAsync() > 0);

        // A hosting gallery supplies its selected theme and same-origin return context.
        // This must override a previous standalone light preference without losing the hub filters.
        var returnPath = new Uri(url).AbsolutePath + "?q=heatmap&family=matrices-maps&theme=dark";
        var hostedExample = new Uri(new Uri(url), "family-heatmap-wide-light.html").AbsoluteUri + "?theme=dark&return=" + Uri.EscapeDataString(returnPath);
        await page.GotoAsync(hostedExample);
        Assert.Equal("dark", await page.Locator("html").GetAttributeAsync("data-theme"));
        Assert.Equal(new Uri(new Uri(url), returnPath).AbsoluteUri, await page.Locator(".back-link").GetAttributeAsync("href"));
        await page.Locator("[data-set-theme=system]").ClickAsync();
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await page.WaitForFunctionAsync("() => document.documentElement.dataset.theme === 'dark'");
        Assert.Equal("true", await page.Locator("[data-set-theme=system]").GetAttributeAsync("aria-pressed"));
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        await page.WaitForFunctionAsync("() => document.documentElement.dataset.theme === 'light'");
        await page.Locator(".back-link").ClickAsync();
        Assert.Equal("heatmap", await page.Locator("[data-gallery-search]").InputValueAsync());
        AssertNoConsoleErrors(session);
    }
}
