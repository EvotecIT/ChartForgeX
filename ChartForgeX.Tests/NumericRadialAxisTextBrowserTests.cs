using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Actual hosts preserve readable axis text and source readouts after measured title/rotation layout.</summary>
public sealed class NumericRadialAxisTextBrowserTests {
    private const string ProofFont = "CFX Radial Axis Carlito";
    private static readonly Lazy<string> ProofFontCss = new(CreateProofFontCss);

    [Theory]
    [InlineData(false, 360, false)]
    [InlineData(false, 360, true)]
    [InlineData(false, 800, false)]
    [InlineData(false, 800, true)]
    [InlineData(true, 360, false)]
    [InlineData(true, 360, true)]
    [InlineData(true, 800, false)]
    [InlineData(true, 800, true)]
    public async Task TitlesAndFixedLabelsRetainNativeCentreTooltipAndSelection(bool bars, int width, bool dark) {
        if (!Enabled) return;
        var chart = NumericRadialAxisTextTests.Configured(bars, width, dark);
        UseProofFonts(chart);
        chart.Series[0].WithInteractionKey("requests"); chart.Series[1].WithInteractionKey("resolved");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var mark = NumericRadialSeriesTests.Marks(prepared.Scene)[0];
        var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var native = new { x = mark.Cx + Math.Cos(angle) * radius, y = mark.Cy + Math.Sin(angle) * radius };
        await using var session = await OpenTitleChartAsync(chart, width + 40, width == 360 ? 440 : 520);
        var page = session.Page;
        var titleBounds = await TitleBoundsAsync(page, prepared);
        Assert.Equal(3, await page.Locator("[data-cfx-role='radial-value-axis-title'] text,[data-cfx-role='radial-category-axis-title'] text").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const svg = document.querySelector('.cfx-stage svg').getBoundingClientRect();
                const texts = Array.from(document.querySelectorAll('[data-cfx-role="radial-value-axis-title"] text,[data-cfx-role="radial-category-axis-title"] text,[data-cfx-role="radial-value-label"] text,[data-cfx-role="radial-category-label"] text'));
                const boxes = texts.map(text => text.getBoundingClientRect());
                return boxes.every((a,i) => a.width > 0 && a.height > 0 && a.left >= svg.left-.1 && a.right <= svg.right+.1 && a.top >= svg.top-.1 && a.bottom <= svg.bottom+.1 &&
                    boxes.slice(i+1).every(b => a.right<=b.left+.1 || b.right<=a.left+.1 || a.bottom<=b.top+.1 || b.bottom<=a.top+.1));
            }
            """));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var stem = string.IsNullOrWhiteSpace(capture) ? null : Path.Combine(capture, $"axis-text-{(bars ? "bar" : "column")}-{(width == 360 ? "compact" : "wide")}-{(dark ? "dark" : "light")}");
        if (stem != null) {
            Directory.CreateDirectory(capture!);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-browser.png", FullPage = true });
        }
        await page.EvaluateAsync("() => { window.cfxSelections=[];document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect',event=>window.cfxSelections.push(event.detail)); }");
        var screen = await page.EvaluateAsync<double[]>("point => { const svg=document.querySelector('.cfx-stage svg'); const p=svg.createSVGPoint(); p.x=point.x; p.y=point.y; const q=p.matrixTransform(svg.getScreenCTM());return [q.x,q.y]; }", native);
        await page.Mouse.MoveAsync((float)screen[0], (float)screen[1], new MouseMoveOptions { Steps = 4 });
        var tooltip = await TooltipTextAsync(page);
        Assert.Matches("1,?200", tooltip);
        await page.Mouse.ClickAsync((float)screen[0], (float)screen[1]);
        Assert.Equal("true", await page.Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        Assert.Equal("0", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.sourcePoint"));
        Assert.Equal("requests:0", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.targetId"));
        if (stem != null) {
            File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + "-native.png", prepared.ToPng());
            File.WriteAllText(stem + ".html", await page.ContentAsync());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-readout.png", FullPage = true });
            File.WriteAllText(stem + "-receipt.json", JsonSerializer.Serialize(new { nativeCentre = native, screenCentre = screen, tooltip,
                sourcePoint = await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.sourcePoint"),
                titleBounds,
                textBounds = NumericRadialAxisTextTests.Footprints(prepared.Scene).Select(item => new { item.Text.Role, item.Text.Id, item.Bounds, item.Angle }),
                console = session.ConsoleLog.Select(entry => new { type = entry.Type.ToString(), entry.Text }) }, new JsonSerializerOptions { WriteIndented = true }));
        }
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task InsufficientSpaceRetainsKeyboardFactsWithoutInventingAPointerSurface() {
        if (!Enabled) return;
        var chart = NumericRadialAxisTextTests.LongTitles(true);
        UseProofFonts(chart);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        await using var session = await OpenTitleChartAsync(chart, 400, 440);
        var page = session.Page; var target = page.Locator(Point(0, 0));
        var titleBounds = await TitleBoundsAsync(page, prepared);
        Assert.Equal(3, await page.Locator("[data-cfx-role='radial-value-axis-title'] text,[data-cfx-role='radial-category-axis-title'] text").CountAsync());
        Assert.Equal(6, await page.Locator("[data-cfx-role='point'][data-cfx-geometry-status='layout-collapse']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role='radial-bar'],[data-cfx-role='point'] path,[data-cfx-role='point'] rect").CountAsync());
        Assert.Equal("0", await target.GetAttributeAsync("tabindex"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var stem = string.IsNullOrWhiteSpace(capture) ? null : Path.Combine(capture, "axis-text-layout-collapse");
        if (stem != null) {
            Directory.CreateDirectory(capture!);
            File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + "-native.png", prepared.ToPng());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-browser.png" });
        }
        await target.FocusAsync();
        Assert.Matches("1,?200", await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("1", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxPoint"));
        if (stem != null) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-readout.png" });
            File.WriteAllText(stem + "-receipt.json", JsonSerializer.Serialize(new { pointCount = 6, pointerSurfaces = 0, titleBounds,
                keyboardReadout = await TooltipTextAsync(page), consoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlConsoleMessageType.Error) },
                new JsonSerializerOptions { WriteIndented = true }));
        }
        AssertNoConsoleErrors(session);
    }

    private static void UseProofFonts(Chart chart) {
        _ = ProofFontCss.Value;
        chart.WithFontFamily(ProofFont);
        chart.Options.PngFontPath = null;
    }

    private static string CreateProofFontCss() {
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        string Face(string file, int weight) {
            var path = Path.Combine(directory, file);
            Assert.True(File.Exists(path), "The existing Carlito fixture must be available.");
            FontRegistry.Register(ProofFont, path, weight);
            return "@font-face{font-family:'" + ProofFont + "';src:url(data:font/ttf;base64," + Convert.ToBase64String(File.ReadAllBytes(path))
                + ") format('truetype');font-weight:" + weight + ";font-display:block}";
        }
        return "<style>" + Face("Carlito-Regular.ttf", 400) + Face("Carlito-Bold.ttf", 700) + "</style>";
    }

    private static async Task<HtmlBrowserSession> OpenTitleChartAsync(Chart chart, int width, int height) {
        var html = chart.ToInteractiveHtmlPage().Replace("</head>", ProofFontCss.Value + "</head>", StringComparison.Ordinal);
        var session = await OpenAsync(html, width, height);
        try {
            Assert.True(await session.Page.EvaluateAsync<bool>("""
                async family => {
                    await Promise.all([document.fonts.load(`400 12px "${family}"`), document.fonts.load(`650 28px "${family}"`)]);
                    await document.fonts.ready;
                    return document.fonts.check(`400 12px "${family}"`) && document.fonts.check(`650 28px "${family}"`);
                }
                """, ProofFont));
            return session;
        } catch { await session.DisposeAsync(); throw; }
    }

    private static async Task<JsonElement> TitleBoundsAsync(IPage page, PreparedVisual prepared) {
        var strips = prepared.Regions.Where(region => region.Role is "radial-value-axis-title" or "radial-category-axis-title")
            .Select(region => new { id = region.Id, left = region.Bounds.Left, right = region.Bounds.Right, top = region.Bounds.Top, bottom = region.Bounds.Bottom }).ToArray();
        var result = await page.EvaluateAsync<JsonElement>("""
            strips => {
                const svg = document.querySelector('.cfx-stage svg'), viewport = svg.viewBox.baseVal, defects = [];
                const context = document.createElement('canvas').getContext('2d');
                const titles = [...svg.querySelectorAll('[data-cfx-role="radial-value-axis-title"] text,[data-cfx-role="radial-category-axis-title"] text')].map(text => {
                    const id = text.parentElement.dataset.cfxSourceId, style = getComputedStyle(text), box = text.getBBox();
                    const strip = strips.find(region => region.id === id);
                    context.font = `${style.fontStyle} ${style.fontWeight} ${style.fontSize} ${style.fontFamily}`;
                    const metrics = context.measureText(text.textContent), x = Number(text.getAttribute('x')), y = Number(text.getAttribute('y'));
                    const ink = { left:x-metrics.actualBoundingBoxLeft, right:x+metrics.actualBoundingBoxRight,
                        top:y-metrics.actualBoundingBoxAscent, bottom:y+metrics.actualBoundingBoxDescent };
                    if (!style.fontFamily.includes('CFX Radial Axis Carlito')) defects.push(id + ': lost the fixture face');
                    if (box.width <= 0 || box.height <= 0 || box.x < viewport.x-.1 || box.x+box.width > viewport.x+viewport.width+.1
                        || box.y < viewport.y-.1 || box.y+box.height > viewport.y+viewport.height+.1) defects.push(id + ': outside viewport');
                    // Glyph ink is narrower than font ascent/descent padding and is what the strip must visibly contain.
                    if (!strip || ink.left < strip.left-.1 || ink.right > strip.right+.1 || ink.top < strip.top-.1 || ink.bottom > strip.bottom+.1)
                        defects.push(id + ': visible title outside its measured strip');
                    return { id, text:text.textContent, fontFamily:style.fontFamily, fontWeight:style.fontWeight,
                        bounds:{left:box.x,right:box.x+box.width,top:box.y,bottom:box.y+box.height}, ink, strip };
                });
                return { titles, defects };
            }
            """, strips);
        Assert.Empty(result.GetProperty("defects").EnumerateArray());
        return result;
    }
}
