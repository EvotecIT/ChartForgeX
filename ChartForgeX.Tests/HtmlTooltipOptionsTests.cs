using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlTooltipOptionsTests {
    [Fact]
    public void SharedXIsTheDefaultForChartsAndDashboardChildren() {
        Assert.Equal(HtmlChartTooltipMode.SharedX, new HtmlChartInteractionOptions().TooltipMode);
        Assert.Equal(HtmlChartTooltipMode.SharedX, new HtmlInteractiveDashboardOptions().TooltipMode);
        AssertModes(Chart().ToInteractiveHtmlPage(), "shared-x", 1);
        AssertModes(new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(), "shared-x", 2);
    }

    [Theory]
    [InlineData(HtmlChartTooltipMode.Single, "single")]
    [InlineData(HtmlChartTooltipMode.SharedX, "shared-x")]
    public void ExplicitModeReachesPagesFragmentsAndEveryDashboardChild(HtmlChartTooltipMode mode, string serialized) {
        AssertModes(Chart().ToInteractiveHtmlPage(options => options.TooltipMode = mode), serialized, 1);
        AssertModes(Chart().ToInteractiveHtmlFragment(options => options.TooltipMode = mode), serialized, 1);
        AssertModes(Chart().ToInteractiveHtmlFragmentWithoutAssets(options => options.TooltipMode = mode), serialized, 1);
        AssertModes(new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => options.TooltipMode = mode), serialized, 2);
    }

    [Fact]
    public void InvalidModesFailAtEveryRenderBoundary() {
        var invalid = (HtmlChartTooltipMode)123;
        Assert.Equal("TooltipMode", Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlPage(options => options.TooltipMode = invalid)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlFragment(options => options.TooltipMode = invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlFragmentWithoutAssets(options => options.TooltipMode = invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => options.TooltipMode = invalid));
    }

    private static Chart Chart() => ChartForgeX.Core.Chart.Create().WithSize(520, 320).AddLine("Readings", ChartPoints.FromValues(1, 3, 2));

    private static void AssertModes(string html, string mode, int count) {
        var roots = Regex.Matches(html, "<section[^>]*class=\"cfx-interactive-chart\"[^>]*>");
        Assert.Equal(count, roots.Count);
        Assert.All(roots.Cast<Match>(), root => Assert.Contains("data-cfx-tooltip-mode=\"" + mode + "\"", root.Value, StringComparison.Ordinal));
    }
}
