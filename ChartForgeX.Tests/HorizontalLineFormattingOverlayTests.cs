using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HorizontalLineFormattingOverlayTests {
    [Theory]
    [InlineData(360, false, false)]
    [InlineData(360, true, true)]
    [InlineData(760, false, true)]
    [InlineData(760, true, false)]
    public async Task BrowserExportsKeepValueUnitsAndStatisticsOnPhysicalX(int width, bool dark, bool step) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Model(step).WithSize(width - 32, 420).WithXAxis("Latency").WithYAxis("Run")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        chart.AddMeanLine("Mean", chart.Series[0].Points).AddStandardDeviationBand("Deviation", chart.Series[0].Points);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.XAxis.WithValueFormat(ChartValueFormat.Number("0' ms'"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage();
        await using var session = await InteractiveChartBrowser.OpenAsync(html, width, 540);
        var page = session.Page;
        var values = await page.Locator("[data-cfx-role=axis-x-label]").AllTextContentsAsync();
        var categories = await page.Locator("[data-cfx-role=axis-y-label]").AllTextContentsAsync();
        Assert.NotEmpty(values); Assert.All(values, value => Assert.Contains("ms", value));
        Assert.DoesNotContain(categories, category => category.Contains("ms"));
        var guide = await page.Locator("[data-cfx-kind=VerticalLine] [data-cfx-role=annotation-line]").BoundingBoxAsync();
        Assert.NotNull(guide); Assert.True(guide!.Height > 150 && guide.Width < 2, "Guide bounds: " + guide.Width + " x " + guide.Height);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = "physical-values-" + width + (dark ? "-dark" : "-light") + (step ? "-step" : "-line");
            File.WriteAllText(Path.Combine(capture, name + ".html"), html);
            File.WriteAllBytes(Path.Combine(capture, name + ".native.png"), prepared.ToPng());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png"), FullPage = true });
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ValueFormattingAndExplicitLabelsStayOnPhysicalX(bool step, bool explicitLabels) {
        var chart = Model(step).WithXAxisBounds(0, 40).WithYAxisBounds(0, 4);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.XAxis.TickCount = 3;
        chart.Options.YAxis.TickCount = 3;
        chart.Options.XAxis.WithValueFormat(ChartValueFormat.Number("0.0' ms'"));
        if (explicitLabels) {
            chart.Options.XAxis.Labels.Add(new ChartAxisLabel(0, "zero value"));
            chart.Options.XAxis.Labels.Add(new ChartAxisLabel(40, "maximum value"));
        }
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var values = prepared.Regions.Where(region => region.Role == "axis-x-label").Select(region => region.Label!).ToArray();
        var categories = prepared.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label!).ToArray();
        Assert.NotEmpty(values);
        Assert.DoesNotContain(categories, label => label.Contains("ms") || label.Contains("value"));
        if (explicitLabels) {
            Assert.Contains(values, label => label.StartsWith("zero value"));
            Assert.Contains(values, label => label.StartsWith("maximum value"));
        } else Assert.All(values, label => Assert.Contains("ms", label));
        Assert.All(categories, label => Assert.DoesNotContain("ms", label));
    }

    [Fact]
    public void EachPhysicalAxisCapturesOnlyItsOwnCallbackAndLabels() {
        var chart = Model(false).WithXAxisBounds(0, 40).WithYAxisBounds(0, 4);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.XAxis.TickCount = 3;
        chart.Options.YAxis.TickCount = 3;
        var values = new Dictionary<double, int>(); var categories = new Dictionary<double, int>();
        chart.Options.XAxis.WithLabelFormatter(value => Capture(values, value, "Value "));
        chart.Options.YAxis.WithLabelFormatter(value => Capture(categories, value, "Category "));
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(0, "zero value"));
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(4, "last category"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label!.StartsWith("zero value"));
        Assert.Contains(prepared.Regions, region => region.Role == "axis-y-label" && region.Label!.StartsWith("last category"));
        Assert.DoesNotContain(0, values.Keys);
        Assert.DoesNotContain(4, categories.Keys);
        Assert.All(values.Values.Concat(categories.Values), count => Assert.Equal(1, count));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.XAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed value callback"));
        chart.Options.YAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed category callback"));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void StatisticalOverlaysFollowTheValueAxisRegardlessOfConfigurationOrder(bool step, bool configureFirst) {
        var chart = Model(step);
        if (configureFirst) chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var points = chart.Series[0].Points;
        chart.AddStandardDeviationBand("Deviation", points, color: HorizontalRasterAssertions.Band, opacity: 1)
            .AddMeanLine("Mean", points, HorizontalRasterAssertions.Guide).AddMedianLine("Median", points, HorizontalRasterAssertions.Guide);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var range = ChartRange.FromChart(chart);
        Assert.Equal(1, range.MinY); Assert.Equal(4, range.MaxY);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var guides = prepared.Regions.Where(region => region.Role == "annotation").ToArray();
        Assert.Equal(3, guides.Length);
        Assert.All(guides, guide => Assert.True(guide.Bounds.Height > 200));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = "statistics-category-range-" + (step ? "step" : "line") + (configureFirst ? "-first" : "-last");
            File.WriteAllText(Path.Combine(capture, name + ".svg"), prepared.ToSvg());
            File.WriteAllBytes(Path.Combine(capture, name + ".native.png"), prepared.ToPng());
        }
        var observations = prepared.Regions.Where(region => region.Role == "point").ToArray();
        var categorySpan = observations.Max(point => point.Bounds.Y) - observations.Min(point => point.Bounds.Y);
        Assert.True(categorySpan > guides[0].Bounds.Height * .8, "Statistical reference fitting must preserve the observation category domain.");
        Assert.Equal(2, guides.Count(guide => guide.Bounds.Width == 0));
        var band = Assert.Single(guides, guide => guide.Bounds.Width > 0);
        Assert.True(band.Bounds.Width > 50);
        Assert.Contains("data-cfx-kind=\"VerticalLine\"", prepared.ToSvg());
        Assert.Contains("data-cfx-kind=\"VerticalBand\"", prepared.ToSvg());
        HorizontalRasterAssertions.Statistics(prepared, guides.First(guide => guide.Bounds.Width == 0).Bounds, band.Bounds);
        // A later preparation follows the current orientation; the detached scene retains its geometry.
        chart.Options.LineOrientation = ChartOrientation.Vertical;
        var vertical = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.All(vertical.Regions.Where(region => region.Role == "annotation"), guide => Assert.True(guide.Bounds.Width > 200));
        Assert.Contains("data-cfx-kind=\"VerticalLine\"", prepared.ToSvg());
    }

    [Fact]
    public void ExplicitHorizontalAnnotationsRemainOnPhysicalY() {
        var chart = Model(false);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.AddHorizontalLine(8, "Category guide").AddHorizontalBand(5, 6, "Category band");
        Assert.Equal(8, ChartRange.FromChart(chart).MaxY);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.All(prepared.Regions.Where(region => region.Role == "annotation"), guide => Assert.True(guide.Bounds.Width > 200));
        Assert.Contains("data-cfx-kind=\"HorizontalLine\"", prepared.ToSvg());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HorizontalBarAndWaterfallStatisticsUseTheirSharedValueAxis(bool waterfall) {
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30), new ChartPoint(4, 40) };
        var chart = Chart.Create().WithSize(700, 450).WithLegend(false).WithHeader(false);
        if (waterfall) chart.AddWaterfall("Readings", points).ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal);
        else chart.AddHorizontalBar("Readings", points);
        var initial = ChartRange.FromChart(chart);
        chart.AddMeanLine("Mean", points).AddStandardDeviationBand("Deviation", points);
        var range = ChartRange.FromChart(chart);
        Assert.Equal(initial.MinY, range.MinY); Assert.Equal(initial.MaxY, range.MaxY);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Contains("data-cfx-kind=\"VerticalLine\"", prepared.ToSvg());
        Assert.All(prepared.Regions.Where(region => region.Role == "annotation"), guide => Assert.True(guide.Bounds.Height > 200));
    }

    private static Chart Model(bool step) {
        var chart = Chart.Create().WithSize(700, 450).WithLegend(false).WithHeader(false).WithLineMarkers(ChartLineMarkerMode.None);
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30), new ChartPoint(4, 40) };
        return step ? chart.AddStepLine("Readings", points) : chart.AddLine("Readings", points);
    }

    private static string Capture(Dictionary<double, int> calls, double value, string prefix) {
        calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
        return prefix + value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
