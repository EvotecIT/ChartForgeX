using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects member-specific compound captions at the prepared SVG and interactive tooltip boundaries.</summary>
public sealed class CompoundReadoutFeedbackTests {
    [Theory]
    [InlineData("boxplot", false)]
    [InlineData("horizontal-boxplot", false)]
    [InlineData("candlestick", false)]
    [InlineData("ohlc", false)]
    [InlineData("boxplot", true)]
    [InlineData("horizontal-boxplot", true)]
    [InlineData("candlestick", true)]
    [InlineData("ohlc", true)]
    public void AuthoredAxisFormatOwnsEveryCompoundMemberInSvgAndInteractiveHtml(string family, bool interactive) {
        var chart = CreateChart(family);
        var axisCalls = new Dictionary<double, int>();
        var nativeCalls = 0;
        chart.WithValueFormatter(value => { nativeCalls++; return NativeCaption(value); });
        ValueAxis(chart, family).LabelFormatter = value => {
            axisCalls[value] = axisCalls.TryGetValue(value, out var count) ? count + 1 : 1;
            return AxisCaption(family, value);
        };
        var prepared = interactive ? null : chart.Prepare(new VisualRenderContext());
        var markup = interactive ? chart.ToInteractiveHtmlFragmentWithoutAssets() : prepared!.ToSvg();
        var point = PointMetadata(markup);
        foreach (var member in Members(family)) {
            Assert.Equal(AxisCaption(family, member.Value), Attribute(point, "formatted-" + member.Name));
            Assert.Equal(member.Value, double.Parse(Attribute(point, member.Name), CultureInfo.InvariantCulture));
        }
        Assert.All(Members(family), member => Assert.Equal(1, axisCalls[member.Value]));
        Assert.All(axisCalls.Values, count => Assert.Equal(1, count));
        Assert.Equal(1, nativeCalls);
        Assert.Equal(NativeCaption(IsBoxPlot(family) ? 3.15 : 4.25), Attribute(point, "label"));
        Assert.Equal(AxisCaption(family, IsBoxPlot(family) ? 3.15 : 2.05), Attribute(point, "formatted-value"));
        if (IsBoxPlot(family)) Assert.Equal(Attribute(point, "formatted-median"), Attribute(point, "formatted-value"));
        if (prepared != null) {
            chart.WithValueFormatter(_ => throw new InvalidOperationException("Export must retain the prepared native caption."));
            ValueAxis(chart, family).LabelFormatter = _ => throw new InvalidOperationException("Export must retain prepared axis captions.");
            Assert.Equal(markup, prepared.ToSvg());
            Assert.NotEmpty(prepared.ToPng());
            Assert.Equal(1, nativeCalls);
            Assert.All(axisCalls.Values, count => Assert.Equal(1, count));
        }
        SaveArtifact(family + (interactive ? "-interactive.html" : "-native.svg"), markup);
    }

    [Theory]
    [InlineData("boxplot", false)]
    [InlineData("horizontal-boxplot", false)]
    [InlineData("candlestick", false)]
    [InlineData("ohlc", false)]
    [InlineData("boxplot", true)]
    [InlineData("horizontal-boxplot", true)]
    [InlineData("candlestick", true)]
    [InlineData("ohlc", true)]
    public void AutomaticCompoundFactsReusePreparedCaptionsAndKeepCustomLabelsLazy(string family, bool customLabel) {
        var chart = CreateChart(family);
        const string authoredLabel = "Operator supplied complete caption";
        if (customLabel) chart.Series[0].WithPointLabel(0, authoredLabel);
        var calls = 0;
        chart.WithValueFormatter(value => {
            calls++;
            if (customLabel) throw new InvalidOperationException("A custom observation label must keep value formatting lazy.");
            return NativeCaption(value);
        });
        var prepared = chart.Prepare(new VisualRenderContext());
        var svg = prepared.ToSvg();
        var point = PointMetadata(svg);
        foreach (var member in Members(family)) {
            var nativeMember = member.Name == (IsBoxPlot(family) ? "median" : "close");
            // Automatic readouts use axis precision; the underlying fact remains exact.
            var automaticCaption = member.Value == 1.025 ? "1.03" : ExactCaption(member.Value);
            var expected = !customLabel && nativeMember ? NativeCaption(member.Value) : automaticCaption;
            Assert.Equal(expected, Attribute(point, "formatted-" + member.Name));
            Assert.Equal(member.Value, double.Parse(Attribute(point, member.Name), CultureInfo.InvariantCulture));
        }
        Assert.Equal(customLabel ? 0 : 1, calls);
        Assert.Equal(customLabel ? authoredLabel : NativeCaption(IsBoxPlot(family) ? 3.15 : 4.25), Attribute(point, "label"));
        Assert.Equal(IsBoxPlot(family) ? Attribute(point, "formatted-median") : ExactCaption(2.05), Attribute(point, "formatted-value"));
        chart.WithValueFormatter(_ => throw new InvalidOperationException("The prepared snapshot must be detached."));
        Assert.Equal(svg, prepared.ToSvg());
        Assert.NotEmpty(prepared.ToPng());
        Assert.Equal(customLabel ? 0 : 1, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DumbbellDeltaUsesAuthoredValueAxisInSvgAndInteractiveHtml(bool interactive) {
        var chart = CreateChart("dumbbell");
        var calls = new Dictionary<double, int>();
        chart.Options.YAxis.LabelFormatter = value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return AxisCaption("dumbbell", value);
        };
        var prepared = interactive ? null : chart.Prepare(new VisualRenderContext());
        var markup = interactive ? chart.ToInteractiveHtmlFragmentWithoutAssets() : prepared!.ToSvg();
        var point = PointMetadata(markup);
        Assert.Equal("2,200 ms", Attribute(point, "formatted-delta"));
        Assert.Equal(4.25 - 2.05, double.Parse(Attribute(point, "delta"), CultureInfo.InvariantCulture));
        Assert.Equal("2,050 ms", Attribute(point, "formatted-start"));
        Assert.Equal("4,250 ms", Attribute(point, "formatted-end"));
        Assert.All(new[] { 2.05, 4.25, 4.25 - 2.05 }, value => Assert.Equal(1, calls[value]));
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        if (prepared != null) {
            chart.Options.YAxis.LabelFormatter = _ => throw new InvalidOperationException("Export must retain the captured delta.");
            Assert.Equal(markup, prepared.ToSvg());
            Assert.NotEmpty(prepared.ToPng());
        }
        SaveArtifact("dumbbell-" + (interactive ? "interactive.html" : "native.svg"), markup);
    }

    [BrowserTheory]
    [InlineData("boxplot", false)]
    [InlineData("horizontal-boxplot", true)]
    [InlineData("candlestick", false)]
    [InlineData("ohlc", true)]
    public async Task CompoundTooltipsUseCapturedMemberFormatsAndKeepRawSelectionValues(string family, bool compact) {
        if (!Enabled) return;
        var chart = CreateChart(family).WithSize(compact ? 316 : 596, 338)
            .WithPointLegend().WithLegend()
            .WithTheme(VisualTheme.Graphite(), compact ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithValueFormatter(NativeCaption);
        ValueAxis(chart, family).LabelFormatter = value => AxisCaption(family, value);
        var html = chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single);
        chart.WithValueFormatter(_ => throw new InvalidOperationException("The browser consumes captured native captions."));
        ValueAxis(chart, family).LabelFormatter = _ => throw new InvalidOperationException("The browser consumes captured member captions.");
        await using var session = await OpenAsync(html, compact ? 340 : 700, 500);
        var page = session.Page;
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var point = page.Locator(Point(0, 0));
        var rawValue = await point.GetAttributeAsync(IsBoxPlot(family) ? "data-cfx-median" : "data-cfx-open");
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.compoundSelection = event.detail.target)");
        await point.FocusAsync();
        var keyboardRows = await TooltipRowsAsync(page);
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.compoundSelection");
        Assert.Equal("compound:0", selected.GetProperty("targetId").GetString());
        Assert.Equal(rawValue, selected.GetProperty("value").GetString());
        Assert.Equal(IsBoxPlot(family) ? 3.15 : 2.05, selected.GetProperty("numericValue").GetDouble());
        Assert.Equal(.025, selected.GetProperty("x").GetDouble());
        await CaptureAsync(page, family, "keyboard", new { keyboardRows, selected, errors });
        await page.Keyboard.PressAsync("Space");
        await point.BlurAsync();
        await MoveAwayAsync(page);
        var markRole = IsBoxPlot(family) ? "boxplot-body" : family == "candlestick" ? "candlestick-body" : "ohlc-stem";
        await MoveToAsync(page, Point(0, 0) + " [data-cfx-role='" + markRole + "']");
        var pointerRows = await TooltipRowsAsync(page);
        Assert.Equal(keyboardRows, pointerRows);
        var expected = new List<string> { "Series", "Compound", IsBoxPlot(family) ? "Kategoria" : "X", "Próbka" };
        foreach (var member in Members(family)) {
            expected.Add(MemberLabel(member.Name));
            expected.Add(AxisCaption(family, member.Value));
            Assert.Equal(member.Value, double.Parse((await point.GetAttributeAsync("data-cfx-" + member.Name))!, CultureInfo.InvariantCulture));
        }
        Assert.Equal(expected, keyboardRows);
        await CaptureAsync(page, family, "pointer", new { pointerRows, selected, errors });
        var legendRows = expected.Skip(IsBoxPlot(family) ? 2 : 4).ToArray();
        await page.Locator("[data-cfx-role='legend-item'][data-cfx-point='0']").FocusAsync();
        Assert.Equal(legendRows, await TooltipRowsAsync(page));
        await CaptureAsync(page, family, "legend", new { legendRows, selected, errors });
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task DumbbellPointLegendUsesCapturedDeltaAlongsideFormattedEndpoints() {
        if (!Enabled) return;
        var chart = CreateChart("dumbbell").WithSize(396, 338).WithPointLegend().WithLegend();
        chart.Options.YAxis.LabelFormatter = value => AxisCaption("dumbbell", value);
        var html = chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single);
        chart.Options.YAxis.LabelFormatter = _ => throw new InvalidOperationException("The browser consumes the captured delta.");
        await using var session = await OpenAsync(html, 420, 500);
        var page = session.Page;
        var legend = page.Locator("[data-cfx-role='legend-item'][data-cfx-point='0']");
        await legend.FocusAsync();
        var keyboardRows = await TooltipRowsAsync(page);
        Assert.Equal(new[] { "Category", "Próbka", "Start", "2,050 ms", "End", "4,250 ms", "Delta", "2,200 ms" }, keyboardRows);
        await legend.BlurAsync();
        await MoveAwayAsync(page);
        await MoveToAsync(page, "[data-cfx-role='legend-item'][data-cfx-point='0']");
        Assert.Equal(keyboardRows, await TooltipRowsAsync(page));
        Assert.Equal(4.25 - 2.05, double.Parse((await page.Locator(Point(0, 0)).GetAttributeAsync("data-cfx-delta"))!, CultureInfo.InvariantCulture));
        await CaptureAsync(page, "dumbbell", "legend", new { keyboardRows });
        AssertNoConsoleErrors(session);
    }

    private static Chart CreateChart(string family) {
        var chart = Chart.Create().WithAxes(false).WithLegend(false).WithHeader(false).WithDataLabels(false)
            .WithXLabels(new[] { new ChartAxisLabel(.025, "Próbka") });
        chart.Options.ShowGrid = false;
        if (IsBoxPlot(family)) {
            chart.AddSeries(ChartSeries.CreateBoxPlot(new ChartSeriesId("compound"), "Compound", new[] { new ChartBoxPlot(.025, 1.025, 2.05, 3.15, 4.25, 5.35) }));
            if (family == "horizontal-boxplot") chart.ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal);
        } else if (family == "dumbbell") {
            chart.AddSeries(ChartSeries.CreateDumbbell(new ChartSeriesId("compound"), "Compound", new[] { new ChartDumbbell(.025, 2.05, 4.25) }));
        } else {
            var prices = new[] { new ChartCandlestick(.025, 2.05, 5.35, 1.025, 4.25) };
            chart.AddSeries(family == "candlestick"
                ? ChartSeries.CreateCandlestick(new ChartSeriesId("compound"), "Compound", prices)
                : ChartSeries.CreateOhlc(new ChartSeriesId("compound"), "Compound", prices));
        }
        if (IsBoxPlot(family)) {
            chart.ConfigureLabels(labels => {
                labels.Category = "Kategoria"; labels.Minimum = "Minimum"; labels.Q1 = "Dolny kwartyl";
                labels.Median = "Mediana"; labels.Q3 = "Górny kwartyl"; labels.Maximum = "Maksimum";
            });
        }
        return chart;
    }

    private static bool IsBoxPlot(string family) => family is "boxplot" or "horizontal-boxplot";
    private static ChartAxis ValueAxis(Chart chart, string family) => family == "horizontal-boxplot" ? chart.Options.XAxis : chart.Options.YAxis;

    private static (string Name, double Value)[] Members(string family) => IsBoxPlot(family)
        ? new[] { ("minimum", 1.025), ("q1", 2.05), ("median", 3.15), ("q3", 4.25), ("maximum", 5.35) }
        : new[] { ("open", 2.05), ("high", 5.35), ("low", 1.025), ("close", 4.25) };

    private static string ExactCaption(double value) => value.ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant();
    private static string NativeCaption(double value) => "Native " + value.ToString("0.000", CultureInfo.GetCultureInfo("pl-PL"));
    private static string AxisCaption(string family, double value) => value == (IsBoxPlot(family) ? 2.05 : 1.025)
        ? string.Empty : value.ToString("0.000", CultureInfo.GetCultureInfo("pl-PL")) + " ms";

    private static string MemberLabel(string member) => member switch {
        "minimum" => "Minimum", "q1" => "Dolny kwartyl", "median" => "Mediana", "q3" => "Górny kwartyl", "maximum" => "Maksimum",
        "open" => "Open", "high" => "High", "low" => "Low", "close" => "Close",
        _ => throw new ArgumentOutOfRangeException(nameof(member))
    };

    private static XElement PointMetadata(string markup) {
        var start = markup.IndexOf("<svg", StringComparison.Ordinal);
        var end = markup.IndexOf("</svg>", start, StringComparison.Ordinal) + "</svg>".Length;
        return XDocument.Parse(markup.Substring(start, end - start)).Descendants().Single(node => node.Attribute("data-cfx-point") != null);
    }

    private static string Attribute(XElement point, string member) {
        var attribute = point.Attribute("data-cfx-" + member);
        Assert.NotNull(attribute);
        return attribute.Value;
    }

    private static Task<string[]> TooltipRowsAsync(IPage page) => page.Locator(".cfx-tooltip__meta")
        .EvaluateAsync<string[]>("node => Array.from(node.children).map(child => child.textContent)");

    private static void SaveArtifact(string name, string markup) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "compound-" + name), markup);
    }

    private static async Task CaptureAsync(IPage page, string family, string state, object facts) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "compound-" + family + "-" + state);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".png" });
        await File.WriteAllTextAsync(path + ".html", await page.ContentAsync());
        await File.WriteAllTextAsync(path + ".json", JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }));
    }
}
