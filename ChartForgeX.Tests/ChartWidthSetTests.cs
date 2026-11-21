using ChartForgeX.Core;
using ChartForgeX.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartWidthSetTests {
    private static Chart Chart() => ChartForgeX.Core.Chart.Create().WithTitle("Checks over the week").WithSize(1000, 320)
        .WithXLabels("Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday")
        .AddLine("Passed", Enumerable.Range(1, 7).Select(index => new ChartPoint(index, index * 3 % 7 + 2)).ToArray());

    [Fact]
    public void EachWidthIsPreparedNativelyAndTheAuthoredSizeIsRestored() {
        var chart = Chart();
        var html = new HtmlChartRenderer().RenderWidths(chart, "checks");
        Assert.Equal(1000, chart.Options.Size.Width);
        foreach (var width in HtmlChartRenderer.StandardWidths)
            Assert.Contains("data-cfx-width=\"" + width + "\"", html, StringComparison.Ordinal);
        // Native preparation at each width, not one scaled drawing: three distinct view boxes and ID scopes.
        Assert.Equal(new[] { "800", "600", "390" }, System.Text.RegularExpressions.Regex.Matches(html, "viewBox=\"0 0 (\\d+)").Select(match => match.Groups[1].Value));
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(html, "id=\"(cfx-scope-[0-9a-f]{64})-title\"").Select(match => match.Groups[1].Value).Distinct().Count());
        Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlChartRenderer().RenderWidths(chart, "checks", 800));
        // A default (Auto) chart follows the viewer in every drawing; an explicit Light chart keeps literal colours.
        Assert.Contains("var(--cfx-auto-", html, StringComparison.Ordinal);
        Assert.Contains("prefers-color-scheme:dark", html, StringComparison.Ordinal);
        var light = new HtmlChartRenderer().RenderWidths(Chart().WithTheme(ChartForgeX.Themes.VisualTheme.Graphite(), ThemeMode.Light), "checks");
        Assert.DoesNotContain("var(--cfx-auto-", light, StringComparison.Ordinal);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(1000, 800)]
    [InlineData(700, 600)]
    [InlineData(600, 600)]
    [InlineData(420, 390)]
    [InlineData(320, 390)]
    public async Task TheContainerShowsTheWidestDrawingThatFits(int container, int expected) {
        if (!InteractiveChartBrowser.Enabled) return;
        var fragment = new HtmlChartRenderer { StyleNonce = "n" }.RenderWidths(Chart(), "checks");
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><head></head><body style=\"margin:0\"><div style=\"width:"
            + container + "px\">" + fragment + "</div></body></html>", 1100, 600);
        var visible = await session.Page.EvaluateAsync<string[]>("()=>[...document.querySelectorAll('[data-cfx-width]')].filter(node=>getComputedStyle(node).display!=='none').map(node=>node.dataset.cfxWidth)");
        Assert.Equal(new[] { expected.ToString(System.Globalization.CultureInfo.InvariantCulture) }, visible);
        var drawn = await session.Page.Locator("[data-cfx-width='" + expected + "'] svg").EvaluateAsync<double>("node=>node.getBoundingClientRect().width");
        Assert.Equal(Math.Min(container, expected), drawn, 1);
    }

    [Trait("Category", "Browser")]
    [Fact]
    public async Task DifferentWidthSetsOnOnePageChooseIndependentlyOnScreenAndInPrint() {
        if (!InteractiveChartBrowser.Enabled) return;
        var renderer = new HtmlChartRenderer();
        var html = "<!doctype html><html><head></head><body style=\"margin:0\"><div id=\"a\" style=\"width:1000px\">" + renderer.RenderWidths(Chart(), "a")
            + "</div><div id=\"b\" style=\"width:1000px\">" + renderer.RenderWidths(Chart(), "b", 900, 700, 400) + "</div></body></html>";
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 1100, 900);
        const string Visible = "id=>[...document.querySelectorAll('#'+id+' [data-cfx-width]')].filter(node=>getComputedStyle(node).display!=='none').map(node=>node.dataset.cfxWidth)";
        Assert.Equal(new[] { "800" }, await session.Page.EvaluateAsync<string[]>(Visible, "a"));
        Assert.Equal(new[] { "900" }, await session.Page.EvaluateAsync<string[]>(Visible, "b"));
        await session.Page.EmulateMediaAsync(new() { Media = Microsoft.Playwright.Media.Print });
        Assert.Equal(new[] { "800" }, await session.Page.EvaluateAsync<string[]>(Visible, "a"));
        Assert.Equal(new[] { "900" }, await session.Page.EvaluateAsync<string[]>(Visible, "b"));
    }
}
