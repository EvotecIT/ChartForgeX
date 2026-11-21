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
}
