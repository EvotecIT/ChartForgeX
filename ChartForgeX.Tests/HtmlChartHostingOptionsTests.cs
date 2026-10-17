using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlChartHostingOptionsTests {
    [Fact]
    public void AdaptivePreparationCapturesDetachedNativeDrawingsWithoutMutatingTheChart() {
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Requests by region").WithXLabels("North", "South")
            .AddBar("Requests", new[] { new ChartPoint(0, 12), new ChartPoint(1, 24) });
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart, configure: options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 360;
            options.Adaptive.NarrowHeight = 420;
        });
        Assert.Equal(640, chart.Options.Size.Width);
        Assert.Equal(360, chart.Options.Size.Height);
        Assert.Equal(640, section.PreparedVisual.Size.Width);
        var narrow = Assert.IsType<ChartForgeX.Rendering.PreparedVisual>(section.NarrowPreparedVisual);
        Assert.Equal(360, narrow.Size.Width);
        Assert.Equal(420, narrow.Size.Height);
        var svg = XDocument.Parse(narrow.ToSvg());
        Assert.Equal("0 0 360 420", svg.Root!.Attribute("viewBox")!.Value);
        var png = PngReader.Decode(narrow.ToPng());
        Assert.Equal(360, png.Width);
        Assert.Equal(420, png.Height);
        chart.WithSize(900, 700).WithTitle("Changed after preparation");
        Assert.Contains("Requests by region", narrow.ToSvg(), StringComparison.Ordinal);
        Assert.DoesNotContain("Changed after preparation", narrow.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingHostingDefaultsRetainSingleDrawingAndOptInPoliciesAreRendered() {
        var defaults = new HtmlChartInteractionOptions();
        Assert.True(defaults.Tooltip.PinOnSelect);
        Assert.Equal(HtmlChartToolbarLayout.Band, defaults.Toolbar);
        Assert.Equal(HtmlChartResponsiveLayout.Readable, defaults.ResponsiveLayout);
        var chart = Chart.Create().AddLine("Measurements", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        var renderer = new HtmlInteractiveChartRenderer();
        Assert.Null(renderer.PrepareSection(chart).NarrowPreparedVisual);
        var html = renderer.PrepareSection(chart, configure: options => {
            options.Tooltip.PinOnSelect = false;
            options.Toolbar = HtmlChartToolbarLayout.Compact;
        }).Html;
        Assert.Contains("data-cfx-pin-on-select=\"false\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-toolbar=\"compact\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardChildrenReceiveTheConfiguredHostingPolicies() {
        var chart = Chart.Create().AddLine("Measurements", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        var html = new[] { chart, chart }.ToInteractiveHtmlDashboardPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 380; options.Adaptive.NarrowHeight = 420; options.Adaptive.Breakpoint = 600;
            options.Toolbar = HtmlChartToolbarLayout.Compact; options.Tooltip.PinOnSelect = false;
        });
        var sections = System.Text.RegularExpressions.Regex.Matches(html, "<section[^>]+class=\"cfx-interactive-chart\"[^>]*>");
        Assert.Equal(4, sections.Count);
        foreach (System.Text.RegularExpressions.Match section in sections) {
            Assert.Contains("data-cfx-pin-on-select=\"false\"", section.Value, StringComparison.Ordinal);
            Assert.Contains("data-cfx-toolbar=\"compact\"", section.Value, StringComparison.Ordinal);
            Assert.Contains("data-cfx-adaptive-breakpoint=\"600\"", section.Value, StringComparison.Ordinal);
        }
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "viewBox=\"0 0 380 420\"").Count);
    }

    [Theory]
    [InlineData(0, 360, 640)]
    [InlineData(360, 0, 640)]
    [InlineData(360, 360, 0)]
    public void AdaptivePreparationRejectsNonPositiveDrawingAndBreakpoint(int width, int height, int breakpoint) {
        var chart = Chart.Create().AddLine("Measurements", new[] { new ChartPoint(0, 1) });
        Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlInteractiveChartRenderer().PrepareSection(chart, configure: options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = width; options.Adaptive.NarrowHeight = height; options.Adaptive.Breakpoint = breakpoint;
        }));
    }
}
