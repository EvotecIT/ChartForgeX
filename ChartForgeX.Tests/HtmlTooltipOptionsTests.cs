using System.Text.RegularExpressions;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlTooltipOptionsTests {
    [Fact]
    public void SharedXIsTheDefaultForChartsAndDashboardChildren() {
        Assert.Equal(HtmlChartTooltipMode.SharedX, new HtmlChartInteractionOptions().Tooltip.Mode);
        Assert.Equal(HtmlChartTooltipMode.SharedX, new HtmlInteractiveDashboardOptions().Tooltip.Mode);
        Assert.Equal(HtmlChartTooltipRange.WithinDistance(120), new HtmlChartInteractionOptions().Tooltip.Range);
        Assert.Equal(HtmlChartTooltipRange.WithinDistance(120), new HtmlInteractiveDashboardOptions().Tooltip.Range);
        Assert.True(new HtmlChartInteractionOptions().Crosshair.ShowLabel);
        Assert.True(new HtmlInteractiveDashboardOptions().Crosshair.ShowLabel);
        Assert.Equal(0, new HtmlChartInteractionOptions().Tooltip.DelayMilliseconds);
        Assert.Equal(0, new HtmlInteractiveDashboardOptions().Tooltip.DelayMilliseconds);
        AssertModes(Chart().ToInteractiveHtmlPage(), "shared-x", 1);
        AssertModes(new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(), "shared-x", 2);
    }

    [Theory]
    [InlineData(HtmlChartTooltipMode.Single, "single")]
    [InlineData(HtmlChartTooltipMode.SharedX, "shared-x")]
    public void ExplicitModeReachesPagesFragmentsAndEveryDashboardChild(HtmlChartTooltipMode mode, string serialized) {
        AssertModes(Chart().ToInteractiveHtmlPage(options => options.Tooltip.Mode = mode), serialized, 1);
        AssertModes(Chart().ToInteractiveHtmlFragment(options => options.Tooltip.Mode = mode), serialized, 1);
        AssertModes(Chart().ToInteractiveHtmlFragmentWithoutAssets(options => options.Tooltip.Mode = mode), serialized, 1);
        AssertModes(new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => options.Tooltip.Mode = mode), serialized, 2);
    }

    [Fact]
    public void InvalidModesFailAtEveryRenderBoundary() {
        var invalid = (HtmlChartTooltipMode)123;
        Assert.Equal("Mode", Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlPage(options => options.Tooltip.Mode = invalid)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlFragment(options => options.Tooltip.Mode = invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlFragmentWithoutAssets(options => options.Tooltip.Mode = invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => options.Tooltip.Mode = invalid));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(240)]
    [InlineData(int.MaxValue)]
    public void DelayReachesEveryRenderSurfaceWithoutEnablingTooltips(int delay) {
        void Configure(HtmlChartInteractionOptions options) {
            options.Tooltip.DelayMilliseconds = delay;
            options.Interaction.Features = Interactivity.ChartInteractionFeatures.None;
        }
        var surfaces = new[] {
            Chart().ToInteractiveHtmlPage(Configure), Chart().ToInteractiveHtmlFragment(Configure), Chart().ToInteractiveHtmlFragmentWithoutAssets(Configure),
            new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => {
                options.Tooltip.DelayMilliseconds = delay;
                options.Interaction.Features = Interactivity.ChartInteractionFeatures.None;
            })
        };
        foreach (var html in surfaces) {
            var roots = Regex.Matches(html, "<section[^>]*class=\"cfx-interactive-chart\"[^>]*>");
            Assert.NotEmpty(roots.Cast<Match>());
            Assert.All(roots.Cast<Match>(), root => {
                Assert.Contains("data-cfx-tooltip-delay=\"" + delay.ToString(CultureInfo.InvariantCulture) + "\"", root.Value, StringComparison.Ordinal);
                Assert.Contains("data-cfx-interaction-features=\"None\"", root.Value, StringComparison.Ordinal);
            });
        }
    }

    [Fact]
    public void NegativeDelayFailsAtEveryRenderBoundary() {
        void Configure(HtmlChartInteractionOptions options) => options.Tooltip.DelayMilliseconds = -1;
        Assert.Equal("DelayMilliseconds", Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlPage(Configure)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlFragment(Configure));
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart().ToInteractiveHtmlFragmentWithoutAssets(Configure));
        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => options.Tooltip.DelayMilliseconds = -1));
    }

    [Fact]
    public void HtmlTimingLeavesNativeExportsUnchanged() {
        var chart = Chart();
        var svg = chart.ToSvg();
        var png = chart.ToPng();
        chart.ToInteractiveHtmlPage(options => options.Tooltip.DelayMilliseconds = 240);
        Assert.Equal(svg, chart.ToSvg());
        Assert.Equal(png, chart.ToPng());
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "native-delay-default-before.svg"), svg);
            File.WriteAllText(Path.Combine(directory, "native-delay-default-after.svg"), chart.ToSvg());
            File.WriteAllBytes(Path.Combine(directory, "native-delay-default-before.png"), png);
            File.WriteAllBytes(Path.Combine(directory, "native-delay-default-after.png"), chart.ToPng());
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DistanceRejectsUnusableCssPixelValues(double distance) {
        Assert.Equal("cssPixels", Assert.Throws<ArgumentOutOfRangeException>(() => HtmlChartTooltipRange.WithinDistance(distance)).ParamName);
    }

    [Fact]
    public void RangeAndLabelPolicyReachEveryRenderSurfaceWithInvariantDistances() {
        var culture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            foreach (var range in new[] { HtmlChartTooltipRange.Exact, HtmlChartTooltipRange.Nearest, HtmlChartTooltipRange.WithinDistance(0), HtmlChartTooltipRange.WithinDistance(48.5) }) {
                void Configure(HtmlChartInteractionOptions options) { options.Tooltip.Range = range; options.Crosshair.ShowLabel = false; }
                var surfaces = new[] {
                    Chart().ToInteractiveHtmlPage(Configure), Chart().ToInteractiveHtmlFragment(Configure), Chart().ToInteractiveHtmlFragmentWithoutAssets(Configure),
                    new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => { options.Tooltip.Range = range; options.Crosshair.ShowLabel = false; })
                };
                foreach (var html in surfaces) {
                    var roots = Regex.Matches(html, "<section[^>]*class=\"cfx-interactive-chart\"[^>]*>");
                    Assert.NotEmpty(roots.Cast<Match>());
                    Assert.All(roots.Cast<Match>(), root => {
                        Assert.Contains("data-cfx-crosshair-label=\"false\"", root.Value, StringComparison.Ordinal);
                        if (range == HtmlChartTooltipRange.Exact) Assert.Contains("data-cfx-tooltip-range=\"exact\"", root.Value, StringComparison.Ordinal);
                        else if (range == HtmlChartTooltipRange.Nearest) Assert.Contains("data-cfx-tooltip-range=\"nearest\"", root.Value, StringComparison.Ordinal);
                        else {
                            Assert.Contains("data-cfx-tooltip-range=\"distance\"", root.Value, StringComparison.Ordinal);
                            Assert.Contains("data-cfx-tooltip-distance=\"" + (range == HtmlChartTooltipRange.WithinDistance(0) ? "0" : "48.5") + "\"", root.Value, StringComparison.Ordinal);
                        }
                    });
                }
            }
        } finally { CultureInfo.CurrentCulture = culture; }
    }

    private static Chart Chart() => ChartForgeX.Core.Chart.Create().WithSize(520, 320).AddLine("Readings", ChartPoints.FromValues(1, 3, 2));

    private static void AssertModes(string html, string mode, int count) {
        var roots = Regex.Matches(html, "<section[^>]*class=\"cfx-interactive-chart\"[^>]*>");
        Assert.Equal(count, roots.Count);
        Assert.All(roots.Cast<Match>(), root => Assert.Contains("data-cfx-tooltip-mode=\"" + mode + "\"", root.Value, StringComparison.Ordinal));
    }
}
