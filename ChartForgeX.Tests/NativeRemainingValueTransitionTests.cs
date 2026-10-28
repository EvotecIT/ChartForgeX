using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRemainingValueTransitionTests {
    public static IEnumerable<object[]> Families() => RemainingValueTransitionDefaultControls.Families.Select(kind => new object[] { kind });
    [Theory]
    [MemberData(nameof(Families))]
    public void TypedSiblingFramesMatchIndependentlyPreparedNativeMidpointsAndRetainCapturedFontsAfterProducerMutation(string kind) {
        var renderer = new HtmlInteractiveChartRenderer(); var font = BrowserProofFonts.Family;
        var first = kind == "rings" ? 20 : -20; var last = kind == "rings" ? 80 : 120; var middle = 50d;
        var chart = Model(kind, 390, true, first, font); var from = renderer.PrepareSection(chart);
        var to = renderer.PrepareSection(Model(kind, 390, true, last, font));
        var expected = renderer.PrepareSection(Model(kind, 390, true, middle, font)).PreparedVisual;
        var frames = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3);
        Assert.Same(from.PreparedVisual, frames[0]); Assert.Same(to.PreparedVisual, frames[2]);
        Assert.Equal(expected.ToSvg("remaining-midpoint"), frames[1].ToSvg("remaining-midpoint"));
        Assert.Equal(expected.ToPng(), frames[1].ToPng()); NativeValueIdentityProof.AssertEqual(expected, frames[1], "remaining-" + kind);
        var svg = frames.Select(frame => frame.ToSvg("detached-remaining")).ToArray(); var png = frames.Select(frame => frame.ToPng()).ToArray();
        chart.WithFontFamily("Missing later font").WithValueFormat(ChartValueFormat.Custom(_ => "Changed"));
        foreach (var series in chart.Series) series.WithDataLabels(false).ConfigureDataLabelStyle(style => style.FontSize = 40);
        try {
            FontRegistry.Clear(); var retained = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3);
            Assert.Equal(svg, retained.Select(frame => frame.ToSvg("detached-remaining")));
            for (var index = 0; index < retained.Count; index++) Assert.Equal(png[index], retained[index].ToPng());
        } finally { _ = BrowserProofFonts.Family; }
        using var json = JsonDocument.Parse(renderer.PrepareValueTransitionJson(from, to, 3));
        Assert.Equal(kind == "rings" ? "progress-ring-value" : kind.StartsWith("gauge-", StringComparison.Ordinal) ? "gauge-value" : "bullet-value", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal(kind == "rings" ? "completion" : kind.StartsWith("gauge-", StringComparison.Ordinal) ? "capacity" : "verified", json.RootElement.GetProperty("seriesKey").GetString());
        Assert.Equal(kind == "rings" ? "stage-1" : "current", json.RootElement.GetProperty("observationId").GetString());
        Assert.Equal(first, json.RootElement.GetProperty("fromValue").GetDouble()); Assert.Equal(last, json.RootElement.GetProperty("toValue").GetDouble());
        var frameSvg = XDocument.Parse(json.RootElement.GetProperty("wide")[1].GetString()!);
        Assert.Contains(frameSvg.Descendants(), node => (string?)node.Attribute("data-cfx-value") == middle.ToString("R", CultureInfo.InvariantCulture)
            && (string?)node.Attribute("data-cfx-series-key") == json.RootElement.GetProperty("seriesKey").GetString());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "native-remaining-midpoint-" + kind + ".svg"), frames[1].ToSvg());
            File.WriteAllBytes(Path.Combine(output, "native-remaining-midpoint-" + kind + ".png"), frames[1].ToPng());
        }
    }
    [Theory]
    [MemberData(nameof(Families))]
    public void AuthoredPolicyChangesCallbacksAndMissingIdentitiesRejectWithoutChangingOrdinaryNativeOutput(string kind) {
        var renderer = new HtmlInteractiveChartRenderer(); var font = BrowserProofFonts.Family;
        var from = renderer.PrepareSection(Model(kind, 760, false, 20, font)); var unchanged = from.PreparedVisual.ToSvg();
        var callback = Model(kind, 760, false, 80, font).WithValueFormat(ChartValueFormat.Custom(value => value + " units"));
        var ordinary = renderer.PrepareSection(callback); Assert.NotEmpty(ordinary.PreparedVisual.ToPng());
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, ordinary, 3));
        var hidden = Model(kind, 760, false, 80, font); foreach (var series in hidden.Series) series.WithDataLabels(false);
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, renderer.PrepareSection(hidden), 3));
        var policy = Model(kind, 760, false, 80, font);
        if (kind == "rings") policy.WithRadialProgressStrokeScale(1.2);
        else if (kind.StartsWith("gauge-", StringComparison.Ordinal)) policy.ConfigureGauge(options => options.BandGap += 1);
        else policy.Series[1].UpdateBulletValue(0, new ChartBulletValue(80, 76, rangeEnds: new[] { 50d, 80 }));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, renderer.PrepareSection(policy), 3));
        policy = Model(kind, 760, false, 80, font, identified: false);
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, renderer.PrepareSection(policy), 3));
        Assert.Equal(unchanged, from.PreparedVisual.ToSvg());
    }
    [Fact]
    public void IndependentBulletAxisFormatsAndOrderedSeriesCurrentIdentitiesRemainDistinctAcrossAdaptiveFrames() {
        var renderer = new HtmlInteractiveChartRenderer(); var font = BrowserProofFonts.Family;
        HtmlChartSection Section(double value) => renderer.PrepareSection(Model("bullet-horizontal", 760, false, value, font), configure: Configure);
        using var json = JsonDocument.Parse(renderer.PrepareValueTransitionJson(Section(20), Section(80), 3));
        Assert.Equal("verified", json.RootElement.GetProperty("seriesKey").GetString());
        foreach (var drawing in new[] { "wide", "narrow" }) {
            var svg = XDocument.Parse(json.RootElement.GetProperty(drawing)[1].GetString()!);
            var rows = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "bullet-row").ToArray();
            Assert.Equal(2, rows.Length); Assert.All(rows, row => Assert.Equal("current", (string?)row.Attribute("data-cfx-observation-id")));
            Assert.Equal(new[] { "reviewed", "verified" }, rows.Select(row => (string?)row.Attribute("data-cfx-series-key")));
            Assert.Equal(new[] { "35", "50" }, rows.Select(row => (string?)row.Attribute("data-cfx-value")));
            Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "bullet-value-label-source" && (string?)node.Attribute("data-cfx-label") == "50.00 tasks");
        }
        HtmlChartSection Authored(double value) {
            var chart = Model("bullet-horizontal", 760, false, value, font); chart.Series[1].WithPointLabel(0, "Accepted count");
            return renderer.PrepareSection(chart, configure: Configure);
        }
        var authoredMiddle = VisualValueCapture.Frames(Authored(20).PreparedVisual, Authored(80).PreparedVisual, 3)[1];
        NativeValueIdentityProof.AssertEqual(Authored(50).PreparedVisual, authoredMiddle, "bullet-authored");
        Assert.Contains(authoredMiddle.Regions, region => region.Role == "bullet-value-label" && region.Label == "Accepted count");
        var changed = Model("bullet-horizontal", 760, false, 80, font); changed.Series[1].ConfigureBulletAxis(axis => axis.WithValueFormat(ChartValueFormat.Number("0.00' other'")));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(Section(20), renderer.PrepareSection(changed, configure: Configure), 3));
    }
    internal static Chart Model(string kind, int width, bool dark, double value, string font, bool identified = true) {
        var chart = RemainingValueTransitionDefaultControls.Model(kind, width, dark, value, font, identified);
        foreach (var series in chart.Series) series.ConfigureDataLabelStyle(style => style.WithUnderline(TextDecorationStyle.Dashed));
        return chart;
    }
    internal static void Configure(HtmlChartInteractionOptions options) {
        options.Interaction.ChartId = "remaining-native-value"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 440;
    }
}
