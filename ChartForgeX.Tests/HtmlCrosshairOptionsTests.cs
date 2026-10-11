using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlCrosshairOptionsTests {
    [Fact]
    public void NullOverridesRetainDefaultMarkupAndCanRestoreIt() {
        var options = new HtmlChartCrosshairOptions();
        Assert.Null(options.Stroke);
        Assert.Null(options.StrokeWidth);
        Assert.Null(options.LineStyle);
        var defaults = RenderSurfaces(_ => { });
        var restored = RenderSurfaces(crosshair => {
            crosshair.Stroke = ChartColor.Transparent;
            crosshair.StrokeWidth = 0;
            crosshair.LineStyle = HtmlChartCrosshairLineStyle.Dotted;
            crosshair.Stroke = null;
            crosshair.StrokeWidth = null;
            crosshair.LineStyle = null;
        });
        Assert.Equal(defaults, restored);
        foreach (var html in defaults)
            Assert.All(Roots(html), root => Assert.DoesNotContain("data-cfx-crosshair-styled", root, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(HtmlChartCrosshairLineStyle.Solid, "solid")]
    [InlineData(HtmlChartCrosshairLineStyle.Dashed, "dashed")]
    [InlineData(HtmlChartCrosshairLineStyle.Dotted, "dotted")]
    public void ExplicitPaintReachesEverySurfaceWithoutEnablingFeatures(HtmlChartCrosshairLineStyle lineStyle, string cssStyle) {
        var culture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            foreach (var html in RenderSurfaces(crosshair => {
                crosshair.Stroke = ChartColor.FromArgb(128, 37, 99, 235);
                crosshair.StrokeWidth = 1.5;
                crosshair.LineStyle = lineStyle;
                crosshair.ShowLabel = false;
            })) {
                Assert.All(Roots(html), root => {
                    Assert.Contains("data-cfx-crosshair-styled=\"true\"", root, StringComparison.Ordinal);
                    Assert.Contains("--cfx-crosshair-stroke:rgba(37,99,235,0.502)", root, StringComparison.Ordinal);
                    Assert.Contains("--cfx-crosshair-stroke-width:1.5px", root, StringComparison.Ordinal);
                    Assert.Contains("--cfx-crosshair-line-style:" + cssStyle, root, StringComparison.Ordinal);
                    Assert.Contains("data-cfx-crosshair-label=\"false\"", root, StringComparison.Ordinal);
                    Assert.Contains("data-cfx-interaction-features=\"None\"", root, StringComparison.Ordinal);
                });
            }
        } finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public void ZeroWidthIsAnExplicitOverrideAndDoesNotSuppressTheLabel() {
        foreach (var html in RenderSurfaces(crosshair => crosshair.StrokeWidth = 0)) {
            Assert.All(Roots(html), root => {
                Assert.Contains("--cfx-crosshair-stroke-width:0px", root, StringComparison.Ordinal);
                Assert.DoesNotContain("--cfx-crosshair-stroke:", root, StringComparison.Ordinal);
                Assert.DoesNotContain("--cfx-crosshair-line-style:", root, StringComparison.Ordinal);
                Assert.Contains("data-cfx-crosshair-label=\"true\"", root, StringComparison.Ordinal);
            });
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidWidthsFailAtEveryRenderBoundary(double width) =>
        AssertRejected(crosshair => crosshair.StrokeWidth = width, "StrokeWidth");

    [Fact]
    public void UndefinedLineStylesFailAtEveryRenderBoundary() =>
        AssertRejected(crosshair => crosshair.LineStyle = (HtmlChartCrosshairLineStyle)123, "LineStyle");

    [Fact]
    public void AdapterPaintLeavesNativeSvgAndPngUnchangedAndExampleUsesThePublicOptions() {
        var chart = Chart();
        var svg = chart.ToSvg();
        var png = chart.ToPng();
        chart.ToInteractiveHtmlPage(options => {
            options.Crosshair.Stroke = ChartColor.FromRgb(37, 99, 235);
            options.Crosshair.StrokeWidth = 2;
            options.Crosshair.LineStyle = HtmlChartCrosshairLineStyle.Dotted;
        });
        Assert.Equal(svg, chart.ToSvg());
        Assert.Equal(png, chart.ToPng());
        var example = Assert.Single(Roots(HtmlCrosshairExamples.CreatePage(HtmlChartCrosshairLineStyle.Dotted)));
        Assert.Contains("--cfx-crosshair-line-style:dotted", example, StringComparison.Ordinal);
        Assert.Contains("--cfx-crosshair-stroke-width:2px", example, StringComparison.Ordinal);
    }

    private static string[] RenderSurfaces(Action<HtmlChartCrosshairOptions> configure) => Renderers()
        .Select(render => render(configure)).ToArray();

    private static void AssertRejected(Action<HtmlChartCrosshairOptions> configure, string property) {
        foreach (var render in Renderers())
            Assert.Equal(property, Assert.Throws<ArgumentOutOfRangeException>(() => render(configure)).ParamName);
    }

    private static Func<Action<HtmlChartCrosshairOptions>, string>[] Renderers() => new Func<Action<HtmlChartCrosshairOptions>, string>[] {
        configure => Chart().ToInteractiveHtmlPage(options => { configure(options.Crosshair); options.Interaction.Features = ChartInteractionFeatures.None; }),
        configure => Chart().ToInteractiveHtmlFragment(options => { configure(options.Crosshair); options.Interaction.Features = ChartInteractionFeatures.None; }),
        configure => Chart().ToInteractiveHtmlFragmentWithoutAssets(options => { configure(options.Crosshair); options.Interaction.Features = ChartInteractionFeatures.None; }),
        configure => new[] { Chart(), Chart() }.ToInteractiveHtmlDashboardPage(options => { configure(options.Crosshair); options.Interaction.Features = ChartInteractionFeatures.None; })
    };

    private static string[] Roots(string html) {
        var roots = Regex.Matches(html, "<section[^>]*class=\"cfx-interactive-chart\"[^>]*>").Cast<Match>().Select(match => match.Value).ToArray();
        Assert.NotEmpty(roots);
        return roots;
    }

    private static Chart Chart() => ChartForgeX.Core.Chart.Create().WithSize(520, 320)
        .AddScatter("Reading", new[] { new ChartPoint(1, 3), new ChartPoint(2, 2) });
}
