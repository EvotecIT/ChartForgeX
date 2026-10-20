using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class ChartContextMenuBrowserTests {
    [BrowserTheory]
    [InlineData(false, 900)]
    [InlineData(true, 390)]
    public async Task ExistingActionsKeepTranslationsKeyboardFocusExportsAndOwnedLifetime(bool rtl, int width) {
        if (!Enabled) return;
        var chart = SeverityBars(rtl).WithSize(width, 420);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => Options(options, rtl)), width + 24, 720);
        var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("""
            () => { window.menuChart = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                const point = menuChart.resolveTargets().find(target => target.targetKind === 'point');
                menuChart.select({targets:[point]}); }
            """);
        await FocusWithKeyboardAsync(page, page.Locator(Point(0, 0)));
        var focus = await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId");
        await page.Keyboard.PressAsync("Shift+F10");
        var menu = page.Locator(".cfx-chart-context-menu");
        Assert.True(await menu.IsVisibleAsync());
        Assert.Equal(rtl ? "rtl" : "ltr", await menu.EvaluateAsync<string>("node => getComputedStyle(node).direction"));
        Assert.Equal(rtl ? "Chart actions translated" : "Chart controls", await menu.GetAttributeAsync("aria-label"));
        Assert.Equal(new[] { rtl ? "Save vector translated" : "Download SVG", "Download PNG", rtl ? "Reset translated" : "Reset view" },
            await menu.Locator("[role=menuitem]").AllTextContentsAsync());
        await page.Keyboard.PressAsync("End");
        Assert.Equal("reset", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxContextAction"));
        await page.Keyboard.PressAsync("Home");
        Assert.Equal("svg", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxContextAction"));
        await Capture(page, rtl, "keyboard");
        await page.Keyboard.PressAsync("Escape");
        Assert.Equal(0, await menu.CountAsync());
        Assert.Equal(focus, await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));

        var stageBounds = await page.Locator(".cfx-stage").BoundingBoxAsync(); Assert.NotNull(stageBounds);
        await page.Locator(".cfx-stage").ClickAsync(new LocatorClickOptions { Button = MouseButton.Right,
            Position = new Position { X = stageBounds!.Width - 2, Y = 20 } });
        var box = await menu.BoundingBoxAsync(); Assert.NotNull(box);
        Assert.InRange(box!.X, 0, width + 24 - box.Width); Assert.InRange(box.Y, 0, 720 - box.Height);
        var download = await page.RunAndWaitForDownloadAsync(() => menu.Locator("[data-cfx-context-action=svg]").ClickAsync());
        var path = await download.PathAsync(); Assert.NotNull(path);
        var svg = XDocument.Load(path!);
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "bar");
        Assert.Equal(0, await menu.CountAsync());
        Assert.Single((await page.EvaluateAsync<JsonElement>("() => menuChart.getSelection()")).EnumerateArray());

        await page.Keyboard.PressAsync("Shift+F10");
        await menu.Locator("[data-cfx-context-action=reset]").ClickAsync();
        Assert.Empty((await page.EvaluateAsync<JsonElement>("() => menuChart.getSelection()")).EnumerateArray());
        await page.Keyboard.PressAsync("Shift+F10");
        Assert.True(await menu.Locator("[data-cfx-context-action=reset]").IsDisabledAsync());
        await page.EvaluateAsync("html => menuChart.update(html)", chart.ToInteractiveHtmlFragmentWithoutAssets(options => Options(options, rtl)));
        Assert.Equal(0, await menu.CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("() => menuChart.root.contains(document.activeElement)"));
        await page.Keyboard.PressAsync("ContextMenu");
        Assert.True(await menu.IsVisibleAsync());
        await page.EvaluateAsync("() => menuChart.dispose()");
        Assert.Equal(0, await menu.CountAsync());
        Assert.True(await page.Locator(".cfx-stage").EvaluateAsync<bool>("node => node.dispatchEvent(new MouseEvent('contextmenu',{bubbles:true,cancelable:true}))"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task NativeBrowserMenuRemainsAvailableWhenDisabledOrThereAreNoAvailableChartActions(int mode) {
        if (!Enabled) return;
        await using var session = await OpenAsync(SeverityBars(false).ToInteractiveHtmlPage(options => {
            options.IncludeContextMenu = mode != 0; options.IncludeResetButton = mode == 2;
            if (mode == 1) options.Interaction.Features = ChartInteractionFeatures.None;
        }));
        Assert.True(await session.Page.Locator(".cfx-stage").EvaluateAsync<bool>("node => node.dispatchEvent(new MouseEvent('contextmenu',{bubbles:true,cancelable:true}))"));
        Assert.Equal(0, await session.Page.Locator(".cfx-chart-context-menu").CountAsync());
        AssertNoConsoleErrors(session);
    }

    private static void Options(HtmlChartInteractionOptions options, bool rtl) {
        options.IncludeContextMenu = true; options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Interaction.Features |= ChartInteractionFeatures.Export; options.Tooltip.PinOnSelect = false;
        if (!rtl) return;
        options.Localization.Direction = HtmlChartTextDirection.RightToLeft;
        options.Localization[HtmlChartText.ChartControls] = "Chart actions translated";
        options.Localization[HtmlChartText.DownloadSvg] = "Save vector translated";
        options.Localization[HtmlChartText.ResetView] = "Reset translated";
    }
    private static async Task Capture(IPage page, bool rtl, string state) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, $"chart-menu-{rtl}-{state}.png"), FullPage = true });
    }
}
