using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Host theme changes preserve distinct quantitative fill and neutral-outline tokens and point ownership.</summary>
public sealed class QuantitativeMarkerOutlineBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Scatter, 360, false)]
    [InlineData(ChartSeriesKind.Scatter, 360, true)]
    [InlineData(ChartSeriesKind.Scatter, 800, false)]
    [InlineData(ChartSeriesKind.Scatter, 800, true)]
    [InlineData(ChartSeriesKind.Bubble, 360, false)]
    [InlineData(ChartSeriesKind.Bubble, 360, true)]
    [InlineData(ChartSeriesKind.Bubble, 800, false)]
    [InlineData(ChartSeriesKind.Bubble, 800, true)]
    public async Task EqualRgbOutlineSurfaceAndRampFollowIndependentHostVariablesWithoutChangingPointIdentity(ChartSeriesKind kind, int width, bool dark) {
        if (!Enabled) return;
        var shared = ChartColor.FromHex("#35495D");
        var theme = VisualTheme.Graphite().Configure(builder => {
            builder.Light.Foreground = shared; builder.Light.Surface = shared;
            builder.Dark.Foreground = shared; builder.Dark.Surface = shared;
        });
        var chart = QuantitativeMarkerOutlineTests.Create(kind, dark, width).WithTheme(theme, dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.Options.ShowPlotBackground = true; chart.Options.TransparentBackground = false;
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(shared, shared).WithValueRange(0, 20));
        chart.Series[0].WithObservationId(0, new ChartObservationId("color-low")).WithObservationId(1, new ChartObservationId("color-high"));
        chart.WithSvgColorVariables(new SvgColorVariables()
            .Add("--quant-surface", shared, SvgColorRole.Surface)
            .Add("--quant-ramp", shared, SvgColorRole.Ramp)
            .AddThemeToken(VisualThemeColorToken.Foreground, "--quant-foreground"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single);
        await using (var session = await OpenAsync(html, width + 24, 560)) {
            var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
            var marks = page.Locator("[data-cfx-role='point'] [data-cfx-role='" + (kind == ChartSeriesKind.Bubble ? "bubble" : "marker") + "']");
            Assert.Equal(2, await marks.CountAsync());
            Assert.All(await marks.EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.getAttribute('stroke'))"), stroke => Assert.StartsWith("var(--quant-foreground,", stroke, StringComparison.Ordinal));
            Assert.All(await marks.EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.getAttribute('fill'))"), fill => Assert.Contains("var(--quant-ramp,", fill));
            var surface = page.Locator("rect[fill^='var(--quant-surface,']").First;
            Assert.True(await surface.CountAsync() > 0);
            var initialIds = await page.Locator(PointTarget).EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxTargetId)");
            Assert.Equal(new[] { "[\"measured\",\"color-low\"]", "[\"measured\",\"color-high\"]" }, initialIds);
            await page.EvaluateAsync("() => { window.quantitativeSelections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.quantitativeSelections.push(event.detail.target)); }");
            for (var phase = 0; phase < 2; phase++) {
                var phaseDark = phase == 0 ? dark : !dark;
                var foreground = phaseDark ? ChartColor.FromHex("#E8EEF6") : ChartColor.FromHex("#15253B");
                var backdrop = phaseDark ? ChartColor.FromHex("#101722") : ChartColor.FromHex("#F4F7FB");
                var ramp = phase == 0 ? ChartColor.FromHex("#2C98BB") : ChartColor.FromHex("#D27828");
                await page.Locator("svg").EvaluateAsync("""
                    (svg, colors) => {
                        svg.style.setProperty('--quant-foreground', colors.foreground);
                        svg.style.setProperty('--quant-surface', colors.backdrop);
                        svg.style.setProperty('--quant-ramp', colors.ramp);
                    }
                    """, new { foreground = foreground.ToCss(), backdrop = backdrop.ToCss(), ramp = ramp.ToCss() });
                foreach (var mark in await marks.AllAsync()) {
                    await AssertCssColorsEqualAsync(page, foreground.ToCss(), await mark.EvaluateAsync<string>("node => getComputedStyle(node).stroke"));
                    var expectedFill = kind == ChartSeriesKind.Bubble ? "color-mix(in srgb, " + ramp.ToCss() + " 30%, transparent)" : ramp.ToCss();
                    await AssertCssColorsEqualAsync(page, expectedFill, await mark.EvaluateAsync<string>("node => getComputedStyle(node).fill"));
                    Assert.Equal(kind == ChartSeriesKind.Bubble ? "2px" : "1px", await mark.EvaluateAsync<string>("node => getComputedStyle(node).strokeWidth"));
                }
                await AssertCssColorsEqualAsync(page, backdrop.ToCss(), await surface.EvaluateAsync<string>("node => getComputedStyle(node).fill"));
                var target = page.Locator(Point(0, phase));
                await MoveAwayAsync(page); await MoveToAsync(page, Point(0, phase));
                Assert.Contains("Temperature", await TooltipTextAsync(page));
                Assert.Equal(phase == 0 ? "-10" : "100", await target.GetAttributeAsync("data-cfx-color-value"));
                await target.Locator("[data-cfx-role='" + (kind == ChartSeriesKind.Bubble ? "bubble" : "marker") + "']").ClickAsync();
                var selected = await page.EvaluateAsync<JsonElement>("() => window.quantitativeSelections.at(-1)");
                Assert.Equal(initialIds[phase], selected.GetProperty("targetId").GetString());
                Assert.Equal(phase == 0 ? "color-low" : "color-high", selected.GetProperty("observationId").GetString());
                Assert.Equal(phase.ToString(), selected.GetProperty("sourcePoint").GetString());
                Assert.Equal(initialIds, await page.Locator(PointTarget).EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxTargetId)"));
                Assert.Equal(new[] { "-10", "100" }, await page.Locator(PointTarget).EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxColorValue)"));
                Assert.Equal("0", await page.Locator("[data-cfx-role='color-scale-0']").GetAttributeAsync("data-cfx-min-value"));
                Assert.Equal("20", await page.Locator("[data-cfx-role='color-scale-0']").GetAttributeAsync("data-cfx-max-value"));
                if (kind == ChartSeriesKind.Bubble) Assert.Equal(new[] { "4", "9" }, await page.Locator(PointTarget).EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxSize)"));
                await CaptureAsync(phase, selected);
            }
            Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
            Assert.Empty(errors); AssertNoConsoleErrors(session);

            async Task CaptureAsync(int phase, JsonElement selected) {
                var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
                if (string.IsNullOrWhiteSpace(directory)) return;
                Directory.CreateDirectory(directory);
                var name = $"quantitative-outline-{kind}-{width}-{(dark ? "dark" : "light")}";
                if (phase == 0) {
                    await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
                    await File.WriteAllBytesAsync(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
                }
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + $"-phase-{phase}.browser.png") });
                var paints = await marks.EvaluateAllAsync<JsonElement>("nodes => nodes.map(node => ({ fill: getComputedStyle(node).fill, stroke: getComputedStyle(node).stroke, width: getComputedStyle(node).strokeWidth }))");
                await File.WriteAllTextAsync(Path.Combine(directory, name + $"-phase-{phase}.runtime.json"), JsonSerializer.Serialize(new {
                    Paints = paints, Selected = selected, Readout = await TooltipTextAsync(page), Errors = errors,
                    ConsoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error)
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var recipe = (kind == ChartSeriesKind.Bubble ? BubbleColorScaleRecipe.Create(mode: mode) : ScatterColorScaleRecipe.Create(mode: mode))
            .WithSize(width, 400).WithTheme(VisualTheme.Graphite(), mode).WithTitle(V2GalleryModels.Title(kind));
        await using var recipeSession = await OpenAsync(recipe.ToInteractiveHtmlPage(), width + 24, 560);
        var recipePage = recipeSession.Page; var missing = recipePage.Locator(Point(0, 2));
        Assert.Equal("true", await missing.GetAttributeAsync("data-cfx-color-missing"));
        Assert.Null(await missing.GetAttributeAsync("data-cfx-color-value"));
        var recipeMark = missing.Locator("[data-cfx-role='" + (kind == ChartSeriesKind.Bubble ? "bubble" : "marker") + "']");
        await AssertCssColorsEqualAsync(recipePage, VisualTheme.Graphite().Resolve(mode).Foreground.ToCss(), await recipeMark.EvaluateAsync<string>("node => getComputedStyle(node).stroke"));
        await recipePage.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.recipeOutlineSelection = event.detail.target)");
        await MoveToAsync(recipePage, Point(0, 2));
        Assert.Contains("Not sampled", await TooltipTextAsync(recipePage));
        await recipeMark.ClickAsync();
        var recipeSelected = await recipePage.EvaluateAsync<JsonElement>("() => window.recipeOutlineSelection");
        Assert.Equal("recorded-batches:2", recipeSelected.GetProperty("targetId").GetString());
        Assert.Equal("2", recipeSelected.GetProperty("sourcePoint").GetString());
        AssertNoConsoleErrors(recipeSession);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            var name = $"quantitative-outline-{kind}-{width}-{(dark ? "dark" : "light")}-recipe";
            await recipePage.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".browser.png") });
            await File.WriteAllTextAsync(Path.Combine(captures, name + ".runtime.json"), JsonSerializer.Serialize(new {
                Selected = recipeSelected, Missing = await missing.GetAttributeAsync("data-cfx-color-missing"), Readout = await TooltipTextAsync(recipePage),
                Stroke = await recipeMark.EvaluateAsync<string>("node => getComputedStyle(node).stroke"),
                ConsoleErrors = recipeSession.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error)
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
