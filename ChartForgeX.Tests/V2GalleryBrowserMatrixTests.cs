using ChartForgeX.Core;
using ChartForgeX.Themes;
using System.Text.Json;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Checks the published examples at their native geometry in one bounded browser session.</summary>
public sealed class V2GalleryBrowserMatrixTests {
    [Fact]
    public async Task EveryFamilyKeepsReadableNativeSvgAndHealthyPairedPng() {
        var url = Environment.GetEnvironmentVariable("CFX_V2_GALLERY_URL");
        if (!Enabled || string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Scheme != Uri.UriSchemeHttp)
            throw new InvalidOperationException("CFX_V2_GALLERY_URL must be a task-owned loopback HTTP URL.");
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", 1200, 900);
        await session.Page.GotoAsync(url);
        await session.Page.EvaluateAsync("async () => { await document.fonts.ready; }");

        // Fetch a small batch at a time, then measure one native SVG at a time. This avoids
        // 196 page launches, loading their embedded factory listings, or a screenshot golden.
        var defects = await session.Page.EvaluateAsync<string[]>("""
            async expected => {
                const defects = [];
                const report = (artifact, message) => defects.push(artifact.id + ': ' + message);
                const deadline = performance.now() + 25000;
                const signal = AbortSignal.timeout(25000);
                const fetchAsset = async (file, binary = false) => {
                    const response = await fetch(new URL(file, location.href), { signal });
                    if (!response.ok) throw new Error(file + ': HTTP ' + response.status);
                    return binary ? response.blob() : response.text();
                };
                const manifest = JSON.parse(await fetchAsset('manifest.json'));
                const artifacts = manifest.artifacts.filter(item => (item.id.startsWith('family-') || item.family === 'histogram')
                    && ['light', 'dark'].includes(item.theme));
                const standard = artifacts.filter(item => item.id.startsWith('family-') && ['wide', 'compact'].includes(item.variant));
                if (standard.length !== expected.families.length * 4)
                    defects.push('The family matrix must contain one wide and compact export per theme and kind.');
                for (const { kind, family } of expected.families) for (const variant of ['wide', 'compact']) for (const theme of ['light', 'dark']) {
                    // A trend example legitimately contains Scatter and TrendLine. Coverage belongs
                    // to each canonical family ID, while secondary series remain part of its data.
                    const matching = artifacts.filter(item => item.id === 'family-' + family + '-' + variant + '-' + theme);
                    if (matching.length !== 1) defects.push(kind + ': missing or duplicate ' + variant + '/' + theme + ' export');
                    else if (!matching[0].seriesKinds.includes(kind)) report(matching[0], 'Canonical example lost its declared ' + kind + ' series');
                }
                const faces = [...document.fonts].filter(face => face.family.replaceAll('"', '').replaceAll("'", '') === 'CFX Proof Carlito');
                for (const weight of ['400', '700'])
                    if (!faces.some(face => face.weight === weight && face.status === 'loaded')) defects.push('Fixture font weight ' + weight + ' not loaded');

                const host = document.createElement('section');
                host.style.cssText = 'position:absolute;left:0;top:0;width:100%;overflow:hidden;';
                document.querySelector('main').replaceChildren(host);
                const canvas = document.createElement('canvas');
                const context = canvas.getContext('2d', { willReadFrequently: true });
                const colorKey = (red, green, blue) => (red << 16) | (green << 8) | blue;
                const paintCanvas = document.createElement('canvas');
                paintCanvas.width = paintCanvas.height = 1;
                const paintContext = paintCanvas.getContext('2d', { willReadFrequently: true });
                // Let the browser parse CSS solid paints, including rgb()/rgba(), rather than
                // assuming the serializer expresses every valid SVG paint as six-digit hex.
                const solidPaint = value => {
                    if (!value || !CSS.supports('color', value)) return null;
                    paintContext.clearRect(0, 0, 1, 1);
                    paintContext.fillStyle = value;
                    paintContext.fillRect(0, 0, 1, 1);
                    return [...paintContext.getImageData(0, 0, 1, 1).data];
                };
                const opacity = node => {
                    let value = 1;
                    for (let current = node; current && current !== host; current = current.parentElement)
                        value *= Number(getComputedStyle(current).opacity);
                    return value;
                };
                for (let start = 0; start < artifacts.length; start += 8) {
                    if (performance.now() > deadline) throw new Error('Native family inspection exceeded its 25-second bound.');
                    const batch = await Promise.all(artifacts.slice(start, start + 8).map(async artifact => ({
                        artifact, svgText: await fetchAsset(artifact.svg), png: await fetchAsset(artifact.png, true)
                    })));
                    for (const { artifact, svgText, png } of batch) {
                        document.documentElement.dataset.theme = artifact.theme;
                        const parsed = new DOMParser().parseFromString(svgText, 'image/svg+xml');
                        if (parsed.querySelector('parsererror')) { report(artifact, 'Invalid SVG document'); continue; }
                        const svg = document.importNode(parsed.documentElement, true);
                        if (svg.localName !== 'svg') { report(artifact, 'Missing SVG root'); continue; }
                        svg.style.cssText = 'display:block;width:' + artifact.width + 'px;height:' + artifact.height + 'px;max-width:none;';
                        host.replaceChildren(svg);
                        const bounds = svg.getBoundingClientRect();
                        if (Math.abs(bounds.width - artifact.width) > .01 || Math.abs(bounds.height - artifact.height) > .01)
                            report(artifact, 'SVG is scaled instead of measured at native dimensions');
                        if (document.documentElement.scrollWidth > innerWidth + 1) report(artifact, 'Native scene creates horizontal page overflow');
                        if (svg.querySelector('script')) report(artifact, 'Static SVG contains script');
                        if (svg.getAttribute('aria-label') !== artifact.title) report(artifact, 'SVG accessibility title differs from its example');
                        if (artifact.diagnostics.length && (artifact.diagnosticMessages || []).length !== artifact.diagnostics.length)
                            report(artifact, 'Layout diagnostics lost their human-readable explanation');

                        const ids = new Map();
                        for (const node of svg.querySelectorAll('[id]')) {
                            if (ids.has(node.id)) report(artifact, 'Duplicate SVG id ' + node.id);
                            ids.set(node.id, node);
                        }
                        for (const node of svg.querySelectorAll('*')) for (const attribute of node.attributes) {
                            if (attribute.name.startsWith('on')) report(artifact, 'Static SVG contains an event handler');
                            for (const reference of attribute.value.matchAll(/url\(#([^)]+)\)/g))
                                if (!ids.has(reference[1])) report(artifact, 'Missing local paint/clip reference ' + reference[1]);
                            if (['href', 'xlink:href'].includes(attribute.name) && attribute.value.startsWith('#') && !ids.has(attribute.value.substring(1)))
                                report(artifact, 'Missing local link reference ' + attribute.value);
                            if (['d', 'points', 'x', 'y', 'x1', 'y1', 'x2', 'y2', 'width', 'height', 'rx', 'ry', 'r', 'cx', 'cy', 'transform'].includes(attribute.name)
                                && /(?:NaN|Infinity)/.test(attribute.value)) report(artifact, 'Non-finite SVG geometry');
                        }
                        let visibleText = 0;
                        for (const text of svg.querySelectorAll('text')) {
                            if (!text.textContent.trim()) continue;
                            const style = getComputedStyle(text);
                            if (style.display === 'none' || style.visibility === 'hidden' || Number(style.opacity) === 0) continue;
                            visibleText++;
                            const fontSize = parseFloat(style.fontSize);
                            if (!Number.isFinite(fontSize) || fontSize < 10) report(artifact, 'Text below 10 native pixels: ' + text.textContent);
                            if (!style.fontFamily.includes('CFX Proof Carlito') || !document.fonts.check(style.fontWeight + ' ' + fontSize + 'px "CFX Proof Carlito"', text.textContent))
                                report(artifact, 'Text uses an unavailable fixture font');
                            const box = text.getBoundingClientRect();
                            if (![box.left, box.top, box.right, box.bottom].every(Number.isFinite) || box.width <= 0 || box.height <= 0)
                                report(artifact, 'Text has no finite rendered bounds: ' + text.textContent);
                            // This checks the exported canvas, not overlap among intentional chart marks.
                            if (box.left < bounds.left - 1.5 || box.top < bounds.top - 1.5 || box.right > bounds.right + 1.5 || box.bottom > bounds.bottom + 1.5)
                                report(artifact, 'Text exceeds its native canvas: ' + text.textContent);
                        }
                        if (!visibleText) report(artifact, 'Example contains no visible text');

                        const backgroundChannels = solidPaint(expected[artifact.theme]);
                        const background = colorKey(...backgroundChannels.slice(0, 3));
                        const backgroundNode = svg.querySelector('[data-cfx-role=background]');
                        const svgBackground = backgroundNode && solidPaint(getComputedStyle(backgroundNode).fill);
                        if (!svgBackground || svgBackground[3] !== 255 || colorKey(...svgBackground.slice(0, 3)) !== background || opacity(backgroundNode) !== 1)
                            report(artifact, 'SVG background differs from its canonical theme');
                        // A decoded PNG must carry the same theme and at least one solid chart
                        // paint from the SVG. Exact antialiasing or browser/raster text pixels differ.
                        const chartPaints = new Set();
                        for (const node of svg.querySelectorAll('rect,path,circle,ellipse,line,polyline,polygon,text')) {
                            if (node.closest('defs')) continue;
                            const role = node.closest('[data-cfx-role]')?.getAttribute('data-cfx-role') || '';
                            if (!role || /background|frame|legend|axis|grid|label|annotation/.test(role)) continue;
                            const style = getComputedStyle(node);
                            const elementOpacity = opacity(node);
                            for (const attribute of ['fill', 'stroke']) {
                                if (attribute === 'stroke' && parseFloat(style.strokeWidth) === 0) continue;
                                const channels = solidPaint(style[attribute]);
                                if (!channels) continue;
                                const alpha = channels[3] / 255 * elementOpacity * Number(style[attribute + 'Opacity']);
                                if (alpha <= 0) continue;
                                const composed = channels.slice(0, 3).map((channel, index) => Math.round(channel * alpha + backgroundChannels[index] * (1 - alpha)));
                                if (colorKey(...composed) === background) continue;
                                // Native and browser compositing can round by a channel value or two.
                                // The tolerance concerns paint quantization, not a screenshot comparison.
                                for (let red = Math.max(0, composed[0] - 2); red <= Math.min(255, composed[0] + 2); red++)
                                    for (let green = Math.max(0, composed[1] - 2); green <= Math.min(255, composed[1] + 2); green++)
                                        for (let blue = Math.max(0, composed[2] - 2); blue <= Math.min(255, composed[2] + 2); blue++)
                                            if (colorKey(red, green, blue) !== background) chartPaints.add(colorKey(red, green, blue));
                            }
                        }
                        if (!chartPaints.size) report(artifact, 'SVG has no bounded chart paint to check in PNG');
                        const bitmap = await createImageBitmap(png);
                        try {
                            if (bitmap.width !== artifact.width || bitmap.height !== artifact.height) report(artifact, 'Decoded PNG dimensions differ from SVG');
                            canvas.width = bitmap.width; canvas.height = bitmap.height;
                            context.drawImage(bitmap, 0, 0);
                            const pixels = context.getImageData(0, 0, bitmap.width, bitmap.height).data;
                            if (pixels[3] !== 255 || colorKey(pixels[0], pixels[1], pixels[2]) !== background)
                                report(artifact, 'PNG background differs from its paired SVG theme');
                            let chartPixels = 0;
                            for (let offset = 0; offset < pixels.length; offset += 4)
                                if (pixels[offset + 3] === 255 && chartPaints.has(colorKey(pixels[offset], pixels[offset + 1], pixels[offset + 2]))) chartPixels++;
                            if (!chartPixels) report(artifact, 'PNG lost the paired SVG chart paints');
                        } finally { bitmap.close(); }
                    }
                }
                host.remove();
                return defects;
            }
            """, new {
                families = Enum.GetValues<ChartSeriesKind>().Select(kind => new {
                    kind = kind.ToString(), family = System.Text.RegularExpressions.Regex.Replace(kind.ToString(), "([a-z])([A-Z])", "$1-$2").ToLowerInvariant()
                }).ToArray(),
                light = VisualTheme.Graphite().Resolve(VisualThemeMode.Light).Background.ToHex(),
                dark = VisualTheme.Graphite().Resolve(VisualThemeMode.Dark).Background.ToHex()
            }).WaitAsync(TimeSpan.FromSeconds(30));
        var captures = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(captures)) {
            if (!Path.IsPathRooted(captures)) throw new InvalidOperationException("CFX_V2_CAPTURE_ROOT must be an explicit task-owned absolute output path.");
            Directory.CreateDirectory(captures);
            var report = new {
                expectedChartKinds = Enum.GetValues<ChartSeriesKind>().Length,
                minimumNativeExports = Enum.GetValues<ChartSeriesKind>().Length * 4,
                validatedVariants = "All declared chart-family variants in light and dark",
                defects,
                consoleErrors = session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).Select(entry => entry.Text).ToArray()
            };
            await File.WriteAllTextAsync(Path.Combine(captures, "native-family-matrix.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
        Assert.Empty(defects);
        AssertNoConsoleErrors(session);
    }
}
