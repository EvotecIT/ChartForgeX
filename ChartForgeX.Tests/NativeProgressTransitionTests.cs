using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeProgressTransitionTests {
    [Theory]
    [InlineData(0, .5, .75, .625)]
    [InlineData(2, -8, 12, 2)]
    public void NativeFramesReuseCapturedRowPolicyAndMatchAnIndependentlyPreparedIntermediateValue(int row, double first, double last, double middle) {
        var family = BrowserProofFonts.Family; var renderer = new HtmlInteractiveChartRenderer();
        var beforeChart = Model(390, false, row, first, family); var afterChart = Model(390, false, row, last, family);
        var before = renderer.PrepareSection(beforeChart); var after = renderer.PrepareSection(afterChart);
        var expected = renderer.PrepareSection(Model(390, false, row, middle, family)).PreparedVisual;
        var frames = VisualValueCapture.Frames(before.PreparedVisual, after.PreparedVisual, 3);
        Assert.Same(before.PreparedVisual, frames[0]); Assert.Same(after.PreparedVisual, frames[2]);
        Assert.Equal(expected.ToSvg("native-midpoint"), frames[1].ToSvg("native-midpoint"));
        Assert.Equal(expected.ToPng(), frames[1].ToPng());
        NativeValueIdentityProof.AssertEqual(expected, frames[1], "progress-existing-" + row);
        var svg = frames.Select(frame => frame.ToSvg("captured-progress")).ToArray();
        var png = frames.Select(frame => frame.ToPng()).ToArray();
        beforeChart.WithFontFamily("Missing later face").WithValueFormat(ChartValueFormat.Custom(_ => "Changed"));
        beforeChart.Series[0].UpdateProgressItem(row, new ChartProgressItem("Changed", first, row == 0 ? 0 : -20, row == 0 ? 1 : 20));
        try {
            FontRegistry.Clear();
            var captured = VisualValueCapture.Frames(before.PreparedVisual, after.PreparedVisual, 3);
            Assert.Equal(svg, captured.Select(frame => frame.ToSvg("captured-progress")));
            for (var index = 0; index < captured.Count; index++) Assert.Equal(png[index], captured[index].ToPng());
        } finally { _ = BrowserProofFonts.Family; }
        using var json = JsonDocument.Parse(renderer.PrepareValueTransitionJson(before, after, 3));
        Assert.Equal("progress-value", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal("delivery", json.RootElement.GetProperty("seriesKey").GetString());
        Assert.Equal(row == 0 ? "reviewed" : "temperature", json.RootElement.GetProperty("observationId").GetString());
        Assert.Equal(first, json.RootElement.GetProperty("fromValue").GetDouble());
        Assert.Equal(last, json.RootElement.GetProperty("toValue").GetDouble());
        var source = XDocument.Parse(json.RootElement.GetProperty("wide")[1].GetString()!);
        Assert.Contains(source.Descendants(), node => (string?)node.Attribute("data-cfx-observation-id") == (row == 0 ? "reviewed" : "temperature")
            && (string?)node.Attribute("data-cfx-value") == middle.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        Capture(frames[1], "native-progress-midpoint-" + row);
    }

    [Fact]
    public void UnsupportedEndpointChangesAndCaptureLimitsLeaveOrdinaryPreparedScenesAvailable() {
        var family = BrowserProofFonts.Family; var renderer = new HtmlInteractiveChartRenderer();
        var initial = renderer.PrepareSection(Model(760, true, 0, .5, family));
        var destination = renderer.PrepareSection(Model(760, true, 0, .75, family));
        var unchanged = initial.PreparedVisual.ToSvg();
        foreach (var count in new[] { 1, 17 }) Assert.Throws<ArgumentOutOfRangeException>(() => renderer.PrepareValueTransitionJson(initial, destination, count));
        var changed = Model(760, true, 0, .75, family); changed.Series[0].UpdateProgressItem(0, new ChartProgressItem("Reviewed fraction", .75, 0, 2));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(initial, renderer.PrepareSection(changed), 3));
        var formatted = Model(760, true, 0, .75, family).WithValueFormat(ChartValueFormat.Custom(value => value.ToString("0.##")));
        var ordinary = renderer.PrepareSection(formatted); Assert.NotEmpty(ordinary.PreparedVisual.ToPng());
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(initial, ordinary, 3));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => renderer.PrepareValueTransitionJson(initial, destination, 3, cancelled.Token));
        Assert.Equal(unchanged, initial.PreparedVisual.ToSvg());
    }

    [Fact]
    public void AdaptiveFramesRetainBothNativeDrawingsAuthoredLabelsAndCapturedDecorationPolicy() {
        var renderer = new HtmlInteractiveChartRenderer(); var family = BrowserProofFonts.Family;
        HtmlChartSection Section(double value) {
            var chart = Model(760, true, 0, value, family); chart.Series[0].PointLabels.Add("Authored reading");
            return renderer.PrepareSection(chart, configure: options => {
                options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
                options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 340;
            });
        }
        var before = Section(.5); var after = Section(.75);
        using var json = JsonDocument.Parse(renderer.PrepareValueTransitionJson(before, after, 3));
        foreach (var drawing in new[] { "wide", "narrow" }) {
            var frames = json.RootElement.GetProperty(drawing); Assert.Equal(3, frames.GetArrayLength());
            var midpoint = XDocument.Parse(frames[1].GetString()!);
            Assert.Contains(midpoint.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "text-decoration");
            var row = Assert.Single(midpoint.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "progress-row"
                && (string?)node.Attribute("data-cfx-observation-id") == "reviewed");
            Assert.Equal("0.625", (string?)row.Attribute("data-cfx-value")); Assert.Equal("Authored reading", (string?)row.Attribute("data-cfx-full-label"));
            Assert.Equal("Authored reading", (string?)row.Attribute("data-cfx-authored-label"));
        }
        Assert.NotEqual(XDocument.Parse(json.RootElement.GetProperty("wide")[1].GetString()!).Root!.Attribute("viewBox")!.Value,
            XDocument.Parse(json.RootElement.GetProperty("narrow")[1].GetString()!).Root!.Attribute("viewBox")!.Value);
        var outOfDomain = renderer.PrepareSection(Model(760, true, 0, 2, family)); Assert.NotEmpty(outOfDomain.PreparedVisual.ToPng());
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(renderer.PrepareSection(Model(760, true, 0, .5, family)), outOfDomain, 3));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(before, before, 3));
    }

    internal static Chart Model(int width, bool dark, int row, double reading, string family) {
        var chart = IndependentProgressRecipe.Create(width == 390 ? "compact" : "wide", dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithFontFamily(family).WithProgressHandles();
        chart.Series[0].ConfigureDataLabelStyle(style => style.WithItalic().WithUnderline(TextDecorationStyle.Dashed));
        var current = chart.Series[0].ProgressItems[row];
        chart.Series[0].UpdateProgressItem(row, new ChartProgressItem(current.Label, reading, current.Minimum, current.Maximum!.Value, current.Color));
        return chart;
    }
    private static void Capture(PreparedVisual frame, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), frame.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".png"), frame.ToPng());
    }
}
