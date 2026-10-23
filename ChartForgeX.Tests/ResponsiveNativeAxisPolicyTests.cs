using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ResponsiveNativeAxisPolicyTests {
    [Theory]
    [InlineData("ordinal", false)]
    [InlineData("ordinal", true)]
    [InlineData("calendar", false)]
    [InlineData("calendar", true)]
    [InlineData("negative-log", false)]
    [InlineData("negative-log", true)]
    public void SerializedBarAnchorsMatchTheNativePaintedSourcePolicy(string mode, bool reversed) {
        var chart = Create(mode, 760, reversed); var source = Source(chart); var axis = chart.Options.XAxis;
        Assert.NotEmpty(Bars(chart.ToSvg())); // Establish the supported native path before responsive serialization.
        using var spec = Specification(new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600));
        var painted = Bars(spec.RootElement.GetProperty("svg").GetString()!);
        var bars = spec.RootElement.GetProperty("bars").EnumerateArray().ToArray();
        Assert.NotEmpty(painted); Assert.Equal(painted.Count, bars.Length);
        foreach (var bar in bars) {
            var expected = painted[(bar.GetProperty("series").GetInt32(), bar.GetProperty("point").GetInt32())];
            Assert.InRange(Math.Abs(expected.Center - bar.GetProperty("anchor").GetDouble()), 0, .001);
            Assert.InRange(Math.Abs(expected.Center - bar.GetProperty("categoryAnchor").GetDouble()), 0, .001);
        }
        Assert.Same(axis, chart.Options.XAxis); Assert.Equal(760, chart.Options.Size.Width); Assert.Equal(source, Source(chart));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryAndSparklineBarsRemainUsableWithoutChangingCallerPolicies(bool sparkline) {
        var chart = Create("numeric", 760, sparkline: sparkline); var source = Source(chart); var axis = chart.Options.XAxis;
        using var spec = Specification(new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600));
        Assert.Equal(3, spec.RootElement.GetProperty("bars").GetArrayLength());
        Assert.Equal(3, Bars(spec.RootElement.GetProperty("svg").GetString()!).Count);
        Assert.Same(axis, chart.Options.XAxis); Assert.Equal(760, chart.Options.Size.Width); Assert.Equal(source, Source(chart));
    }

    [Fact]
    public void NegativeLogarithmicLineNeedsNoBarProjectionToProduceItsResponsiveFragment() {
        var chart = Create("negative-log", 760, line: true); var source = Source(chart);
        Assert.NotEmpty(chart.ToSvg());
        using var spec = Specification(new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600));
        Assert.Empty(spec.RootElement.GetProperty("bars").EnumerateArray()); Assert.Equal(source, Source(chart));
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData("calendar")]
    [InlineData("ordinal")]
    [InlineData("negative-log")]
    [InlineData("sparkline")]
    public async Task ResizedBarsMatchIndependentNativeDrawingsWithoutLosingCalendarGaps(string mode) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Create(mode, 760); var source = Source(chart);
        var body = "<div id='panel' style='width:320px'>" + new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600)
            + "</div><div id='native' style='position:absolute;left:0;top:300px'></div>";
        await using var session = await BrowserProofFonts.OpenAsync(body, 1440, 640); var page = session.Page;
        await page.EvaluateAsync("async()=>await document.querySelector('[data-cfx-responsive-chart]').cfxResponsiveReady");
        var widths = new[] { 320, 779, 1120, 320 };
        for (var step = 0; step < widths.Length; step++) {
            var width = widths[step];
            await page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width);
            await page.EvaluateAsync("svg=>document.querySelector('#native').innerHTML=svg", Create(mode, width).ToSvg());
            var geometry = await page.EvaluateAsync<double[][][]>("""
                () => ['#panel svg','#native svg'].map(selector =>
                  [...document.querySelector(selector).querySelectorAll('[data-cfx-role=point]')].map(point => {
                    const b=point.querySelector('[data-cfx-role=bar]').getBBox();
                    return [Number(point.dataset.cfxSeries),Number(point.dataset.cfxPoint),b.x+b.width/2,b.width];
                  }))
                """);
            // Retain the observed drawings even when the following geometry assertions fail.
            if (step is 0 or 2) await Capture(page, mode, width, geometry);
            Assert.Equal(geometry[1].Length, geometry[0].Length); Assert.NotEmpty(geometry[1]);
            for (var index = 0; index < geometry[1].Length; index++) {
                var actual = geometry[0][index];
                var expected = Assert.Single(geometry[1], row => row[0] == actual[0] && row[1] == actual[1]);
                Assert.InRange(Math.Abs(expected[2] - actual[2]), 0, .01);
                Assert.InRange(Math.Abs(expected[3] - actual[3]), 0, .01);
            }
        }
        Assert.Equal(source, Source(chart)); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static Chart Create(string mode, int width, bool reversed = false, bool sparkline = false, bool line = false) {
        var january = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var values = mode == "calendar" ? new[] { january.AddDays(9).ToOADate(), january.AddMonths(2).AddDays(29).ToOADate() }
            : mode == "ordinal" ? new[] { january.ToOADate(), january.AddDays(1).ToOADate(), january.AddDays(11).ToOADate() }
            : mode == "negative-log" ? new[] { -16d, -4, -1 } : new[] { 1d, 2, 3 };
        var chart = Chart.Create().WithSize(width, 280).WithFontFamily(BrowserProofFonts.Family);
        if (sparkline || mode == "sparkline") chart.WithSparkline();
        chart.WithPadding(0, 0, 0, 0).WithAxes(false).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithCard(false).WithBarStyle(ChartBarStyle.Flat).WithYAxisBounds(0, 4);
        if (!sparkline && mode != "sparkline") chart.WithMaximumBarWidth(1000);
        var points = values.Select((value, index) => new ChartPoint(value, index + 1)).ToArray();
        if (line) chart.AddLine("Observations", points); else chart.AddBar("Observations", points);
        if (mode == "negative-log") chart.Options.XAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative).WithBounds(-32, -.5);
        else if (mode == "ordinal") chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
        else if (mode == "calendar") chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
        else chart.Options.XAxis.WithBounds(.5, 3.5);
        chart.Options.XAxis.Reversed = reversed;
        return chart;
    }

    private static double[] Source(Chart chart) => chart.Series[0].Points.SelectMany(point => new[] { point.X, point.Y }).ToArray();
    private static JsonDocument Specification(string fragment) => JsonDocument.Parse(Regex.Match(fragment,
        "<script type=\"application/json\" data-cfx-spec>(.*?)</script>", RegexOptions.Singleline).Groups[1].Value);
    private static Dictionary<(int Series, int Point), (double Center, double Width)> Bars(string svg) =>
        XElement.Parse(svg).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToDictionary(
            point => (int.Parse(point.Attribute("data-cfx-series")!.Value, CultureInfo.InvariantCulture),
                int.Parse(point.Attribute("data-cfx-point")!.Value, CultureInfo.InvariantCulture)), point => {
                var bar = Assert.Single(point.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "bar");
                var x = double.Parse(bar.Attribute("x")!.Value, CultureInfo.InvariantCulture);
                var width = double.Parse(bar.Attribute("width")!.Value, CultureInfo.InvariantCulture);
                return (x + width / 2, width);
            });
    private static async Task Capture(IPage page, string mode, int width, double[][][] geometry) {
        var folder = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(folder) || width == 779) return;
        if (!Path.IsPathRooted(folder)) throw new InvalidOperationException("Responsive-axis captures require an absolute task-owned directory.");
        Directory.CreateDirectory(folder); var stem = Path.Combine(folder, $"responsive-native-axis-{mode}-{width}");
        await File.WriteAllTextAsync(stem + ".json", JsonSerializer.Serialize(new { mode, width, geometry }));
        await page.ScreenshotAsync(new() { Path = stem + ".png", FullPage = true, Animations = ScreenshotAnimations.Disabled });
    }
}
