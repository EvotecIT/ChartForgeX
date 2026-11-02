using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Explicit horizontal scale facts remain anchored and complete when nearby captions cannot both be painted.</summary>
public sealed class GaugeHorizontalExplicitScaleTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NearbyExplicitTicksOmitOnlyTheCollidingCaptionAndKeepItsCompleteSemanticFact(bool dark) {
        const string nearby = "Near minimum";
        var chart = Create(dark, 1, nearby);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "gauge-track");
        var ticks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "gauge-tick").ToArray();
        Assert.Equal(new[] { track.Bounds.Left, track.Bounds.Left + track.Bounds.Width * .01, track.Bounds.Right },
            ticks.Select(tick => tick.Start.X));
        var captions = ScaleTexts(prepared);
        Assert.Equal(new[] { "Minimum", "Maximum" }, captions.Select(Text));
        AssertSeparated(captions);
        var facts = prepared.Regions.Where(region => IsScale(region.Role)).ToArray();
        Assert.Equal(new[] { "Minimum", nearby, "Maximum" }, facts.Select(region => region.Label));
        Assert.Equal("series-0-gauge-scale-1", facts[1].Id);
        Assert.Equal("gauge-tick-label-1", facts[1].Role);
        Assert.Single(prepared.Diagnostics, diagnostic => diagnostic.Code == "gauge.label-overflow");
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");

        // An authored empty caption is an independent paint control: its tick remains,
        // while the colliding full fact must produce the same visible SVG/native pixels.
        var emptyControl = Create(dark, 1, "");
        var expectedPaint = emptyControl.Prepare(VisualExportRequest.ForChart(emptyControl).Context);
        Assert.Equal(new[] { "Minimum", "Maximum" }, SvgScaleText(prepared).Select(text => text.Caption));
        Assert.Equal(SvgScaleText(expectedPaint), SvgScaleText(prepared));
        Assert.Equal(expectedPaint.ToPng(), prepared.ToPng());
        Capture(prepared, "horizontal-explicit-close-" + (dark ? "dark" : "light"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeparatedExplicitTicksKeepEveryCompleteCaptionAtItsReading(bool dark) {
        var chart = Create(dark, 50, "Midpoint");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "gauge-track");
        var ticks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "gauge-tick").ToArray();
        Assert.Equal(new[] { track.Bounds.Left, track.Bounds.Left + track.Bounds.Width / 2, track.Bounds.Right },
            ticks.Select(tick => tick.Start.X));
        var captions = ScaleTexts(prepared);
        Assert.Equal(new[] { "Minimum", "Midpoint", "Maximum" }, captions.Select(Text));
        AssertSeparated(captions);
        Assert.All(captions, caption => Assert.Equal(chart.Options.Theme.Typography.AxisSize, caption.Text.Size));
        Assert.Equal(new[] { "Minimum", "Midpoint", "Maximum" }, SvgScaleText(prepared).Select(text => text.Caption));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "gauge.label-overflow" or "radial.text-overflow");
        var png = PngReader.Decode(prepared.ToPng());
        Assert.Equal(360, png.Width);
        Assert.Equal(180, png.Height);
        Capture(prepared, "horizontal-explicit-spaced-" + (dark ? "dark" : "light"));
    }

    internal static Chart Create(bool dark, double middle, string middleCaption) {
        var series = ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity", new ChartGaugeValue(50));
        series.ShowDataLabels = false;
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font), "The existing Carlito fixture makes the footer's glyph geometry independent of host fallback fonts.");
        return Chart.Create().WithSize(360, 180).WithHeader(false).WithLegend(false).WithPngFont(font)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(series)
            .ConfigureGauge(options => {
                options.Form = ChartGaugeForm.Linear;
                options.TickLabels = new[] { new ChartAxisLabel(0, "Minimum"), new ChartAxisLabel(middle, middleCaption), new ChartAxisLabel(100, "Maximum") };
            });
    }

    private static bool IsScale(string? role) => role is "gauge-min-label" or "gauge-max-label"
        || role?.StartsWith("gauge-tick-label-", StringComparison.Ordinal) == true;
    private static VisualSceneText[] ScaleTexts(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => IsScale(node.Role)).ToArray();
    private static string Text(VisualSceneText text) => string.Join("\n", text.Text.Lines.Select(line => line.Text));

    private static void AssertSeparated(IReadOnlyList<VisualSceneText> captions) {
        var bounds = captions.Select(text => new ChartRect(text.Text.Lines.Min(text.LineLeft), text.Baseline - text.Text.Ascent,
            text.Text.Metrics.Width, text.Text.Metrics.Height)).OrderBy(box => box.Left).ToArray();
        for (var index = 1; index < bounds.Length; index++)
            Assert.True(bounds[index - 1].Right <= bounds[index].Left, "Painted horizontal scale captions must not overlap.");
    }

    private static (string Caption, string? X, string? Y)[] SvgScaleText(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg())
        .Descendants().Where(node => node.Name.LocalName == "text" && IsScale(node.AncestorsAndSelf()
            .Select(ancestor => (string?)ancestor.Attribute("data-cfx-role")).FirstOrDefault(role => role != null)))
        .Select(node => (node.Value, (string?)node.Attribute("x"), (string?)node.Attribute("y"))).ToArray();

    private static void Capture(PreparedVisual prepared, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_GAUGE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".png"), prepared.ToPng());
    }
}
