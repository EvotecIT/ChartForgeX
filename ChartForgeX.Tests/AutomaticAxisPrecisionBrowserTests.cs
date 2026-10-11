using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class AutomaticAxisPrecisionBrowserTests {
    [Theory]
    [InlineData("precision", "light", 800, 440)]
    [InlineData("precision", "dark", 800, 440)]
    [InlineData("compact-precision", "light", 360, 360)]
    [InlineData("compact-precision", "dark", 360, 360)]
    public async Task GeneratedAxesKeepDistinctSmallCaptionsInNativeSvgAndPairedPng(string variant, string theme, int width, int height) {
        var url = Environment.GetEnvironmentVariable("CFX_V2_GALLERY_URL");
        if (!Enabled || string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Scheme != Uri.UriSchemeHttp)
            throw new InvalidOperationException("CFX_V2_GALLERY_URL must be a task-owned loopback HTTP URL.");
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", width + 48, height * 2 + 110);
        await session.Page.GotoAsync(url);
        await session.Page.EvaluateAsync("async () => { await document.fonts.ready; }");
        var id = "family-trend-line-" + variant + "-" + theme;
        var captions = await session.Page.EvaluateAsync<string[]>("""
            async ({ id, theme, width, height }) => {
                const fetchAsset = async extension => {
                    const response = await fetch(new URL(id + extension, location.href));
                    if (!response.ok) throw new Error(id + extension + ': HTTP ' + response.status);
                    return response;
                };
                const svgText = await (await fetchAsset('.svg')).text();
                const svg = document.importNode(new DOMParser().parseFromString(svgText, 'image/svg+xml').documentElement, true);
                document.documentElement.dataset.theme = theme;
                document.body.style.cssText = 'margin:24px;padding:0;';
                const host = document.createElement('section');
                host.style.cssText = 'width:' + width + 'px;background:' + (theme === 'dark' ? '#111827' : '#fff');
                svg.style.cssText = 'display:block;width:' + width + 'px;height:' + height + 'px;max-width:none;';
                const image = document.createElement('img');
                image.src = new URL(id + '.png', location.href).href;
                image.style.cssText = 'display:block;width:' + width + 'px;height:' + height + 'px;max-width:none;';
                host.append('SVG', svg, 'PNG', image);
                document.querySelector('main').replaceChildren(host);
                await image.decode();
                if (image.naturalWidth !== width || image.naturalHeight !== height) throw new Error('Paired PNG dimensions changed');
                const bounds = svg.getBoundingClientRect();
                const labels = [...svg.querySelectorAll('[data-cfx-role="axis-y-label"]')];
                const points = [...svg.querySelectorAll('[data-cfx-role="point"]')];
                const expected = ['-0.000531583', '-0.000933522'];
                if (points.length !== expected.length || points.some((point, index) => point.getAttribute('data-cfx-label') !== expected[index]))
                    throw new Error('Default nonzero point display changed');
                for (const label of labels) {
                    const box = label.getBoundingClientRect();
                    if (box.left < bounds.left - 1 || box.right > bounds.right + 1 || box.top < bounds.top - 1 || box.bottom > bounds.bottom + 1)
                        throw new Error('Numeric caption escapes its native SVG');
                }
                return labels.map(label => label.textContent);
            }
            """, new { id, theme, width, height });
        Assert.Equal(new[] { "-0.0009", "-0.0008", "-0.0007", "-0.0006", "-0.0005" }, captions);
        AssertNoConsoleErrors(session);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await session.Page.ScreenshotAsync(new() { Path = Path.Combine(output, id + ".png"), FullPage = true });
        }
    }
}
