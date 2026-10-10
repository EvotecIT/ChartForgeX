using System.Text.RegularExpressions;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Proves typed position policies against actual Chromium screen geometry.</summary>
public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(HtmlChartTooltipPlacement.Top)]
    [InlineData(HtmlChartTooltipPlacement.TopRight)]
    [InlineData(HtmlChartTooltipPlacement.Right)]
    [InlineData(HtmlChartTooltipPlacement.BottomRight)]
    [InlineData(HtmlChartTooltipPlacement.Bottom)]
    [InlineData(HtmlChartTooltipPlacement.BottomLeft)]
    [InlineData(HtmlChartTooltipPlacement.Left)]
    [InlineData(HtmlChartTooltipPlacement.TopLeft)]
    public async Task NativeAnchorAppliesEachDirectionAndSignedOffsets(HtmlChartTooltipPlacement placement) {
        if (!Enabled) return;
        var placements = new[] { placement };
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
            options.Tooltip.Position.Placements = placements;
            options.Tooltip.Position.Gap = 11.5;
            options.Tooltip.Position.OffsetX = -3.25;
            options.Tooltip.Position.OffsetY = 5.75;
        }), 950, 800);
        await PointerAsync(session.Page);
        var geometry = await AssertPositionAsync(session.Page, HtmlChartTooltipAnchor.Node, placements, 11.5, -3.25, 5.75);
        await CaptureAsync(session.Page, "position-direction-" + placement, geometry);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Pointer, HtmlChartResponsiveLayout.Fit, 340, false)]
    [InlineData(HtmlChartTooltipAnchor.Pointer, HtmlChartResponsiveLayout.Readable, 950, true)]
    [InlineData(HtmlChartTooltipAnchor.Node, HtmlChartResponsiveLayout.Readable, 340, true)]
    [InlineData(HtmlChartTooltipAnchor.Node, HtmlChartResponsiveLayout.Fit, 950, false)]
    [InlineData(HtmlChartTooltipAnchor.Node, HtmlChartResponsiveLayout.Fit, 340, true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, HtmlChartResponsiveLayout.Fit, 950, true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, HtmlChartResponsiveLayout.Readable, 340, false)]
    public async Task AnchorsFollowScreenGeometryAcrossThemesAndResponsiveLayouts(HtmlChartTooltipAnchor anchor, HtmlChartResponsiveLayout layout, int width, bool dark) {
        if (!Enabled) return;
        var placements = new[] { HtmlChartTooltipPlacement.BottomRight, HtmlChartTooltipPlacement.TopLeft };
        await using var session = await OpenAsync(Observations(dark).ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = layout;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Position.Placements = placements;
        }), width, 760);
        await CentreReadableTargetAsync(session.Page);
        var pointer = await PointerAsync(session.Page);
        var geometry = await AssertPositionAsync(session.Page, anchor, placements, pointer: pointer);
        if (layout == HtmlChartResponsiveLayout.Readable && width == 340)
            Assert.True(await session.Page.Locator(".cfx-stage").EvaluateAsync<bool>("stage=>stage.scrollWidth>stage.clientWidth"));
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth"));
        await CaptureAsync(session.Page, $"position-{anchor}-{layout}-{width}-{(dark ? "dark" : "light")}", geometry);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrderedFallbackUsesFirstFitOrClampsTheFirstChoice(bool noneFit) {
        if (!Enabled) return;
        var placements = new[] { HtmlChartTooltipPlacement.BottomRight, HtmlChartTooltipPlacement.BottomLeft, HtmlChartTooltipPlacement.TopLeft };
        var x = noneFit ? 10000 : -2.5; var y = noneFit ? -10000 : 3.5;
        await using var session = await OpenAsync(Observations(true).ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Position.Placements = placements;
            options.Tooltip.Position.OffsetX = x;
            options.Tooltip.Position.OffsetY = y;
        }), 700, 900);
        var pointer = await PointerAsync(session.Page, Point(0, 2));
        var geometry = await AssertPositionAsync(session.Page, HtmlChartTooltipAnchor.Pointer, placements, offsetX: x, offsetY: y, pointer: pointer, selector: Point(0, 2));
        var tip = ScreenRect.Read(geometry.GetProperty("tip"));
        if (noneFit) { Assert.InRange(tip.X + tip.Width, 691.85, 692.15); Assert.InRange(tip.Y, 7.85, 8.15); }
        else { Assert.True(tip.X < pointer[0]); Assert.True(tip.Y > pointer[1]); }
        await CaptureAsync(session.Page, "position-fallback-" + noneFit, geometry);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartResponsiveLayout.Fit, 340, false)]
    [InlineData(HtmlChartResponsiveLayout.Readable, 340, true)]
    [InlineData(HtmlChartResponsiveLayout.Fit, 950, true)]
    [InlineData(HtmlChartResponsiveLayout.Readable, 950, false)]
    public async Task DefaultsRetainPointerOffsetAndPriorRuntimeGeometry(HtmlChartResponsiveLayout layout, int width, bool dark) {
        if (!Enabled) return;
        var html = Observations(dark).ToInteractiveHtmlPage(options => options.ResponsiveLayout = layout);
        await using var session = await OpenAsync(html, width, 760);
        var page = session.Page;
        await CentreReadableTargetAsync(page);
        var pointer = await PointerAsync(page);
        var current = await AssertPositionAsync(page, HtmlChartTooltipAnchor.Pointer, new[] { HtmlChartTooltipPlacement.BottomRight }, pointer: pointer);
        var baseline = Environment.GetEnvironmentVariable("CFX_TOOLTIP_BASELINE_SCRIPT");
        if (!string.IsNullOrWhiteSpace(baseline)) {
            var oldScript = await File.ReadAllTextAsync(baseline);
            await page.SetContentAsync(Regex.Replace(html, "<script[^>]*>.*?</script>", _ => "<script>" + oldScript + "</script>", RegexOptions.Singleline));
            await CentreReadableTargetAsync(page);
            await page.Mouse.MoveAsync(0, 0);
            await PointerAsync(page);
            var old = await GeometryAsync(page);
            var before = ScreenRect.Read(old.GetProperty("tip")); var after = ScreenRect.Read(current.GetProperty("tip"));
            Assert.InRange(after.X, before.X - .6, before.X + .6);
            Assert.InRange(after.Y, before.Y - .6, before.Y + .6);
            Assert.InRange(after.Width, before.Width - .6, before.Width + .6);
            Assert.InRange(after.Height, before.Height - .6, before.Height + .6);
            await CaptureAsync(page, $"position-default-before-{layout}-{width}", old);
            await page.SetContentAsync(html);
            await CentreReadableTargetAsync(page);
            await page.Mouse.MoveAsync(0, 0);
            await PointerAsync(page);
        }
        await CaptureAsync(page, $"position-default-current-{layout}-{width}", current);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Pointer)]
    [InlineData(HtmlChartTooltipAnchor.Node)]
    [InlineData(HtmlChartTooltipAnchor.Chart)]
    public async Task ExecutableGalleryFactoryUsesTheRenderedPolicy(HtmlChartTooltipAnchor anchor) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HtmlTooltipPositionExamples.CreatePage(anchor, true), 950, 760);
        await PointerAsync(session.Page, Point(0, 1));
        var placements = anchor == HtmlChartTooltipAnchor.Chart ? new[] { HtmlChartTooltipPlacement.Top, HtmlChartTooltipPlacement.Bottom }
            : new[] { HtmlChartTooltipPlacement.TopRight, HtmlChartTooltipPlacement.BottomRight, HtmlChartTooltipPlacement.BottomLeft, HtmlChartTooltipPlacement.TopLeft };
        var pointer = await PointerAsync(session.Page, Point(0, 1));
        var geometry = await AssertPositionAsync(session.Page, anchor, placements, 12, 6, -4, pointer, Point(0, 1));
        await CaptureAsync(session.Page, "position-gallery-" + anchor, geometry);
        AssertNoConsoleErrors(session);
    }
}
