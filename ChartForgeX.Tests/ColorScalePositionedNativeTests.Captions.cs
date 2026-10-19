using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ColorScalePositionedNativeTests {
    [Theory]
    [InlineData("scatter", true, false)]
    [InlineData("heatmap", false, false)]
    [InlineData("treemap", true, false)]
    [InlineData("region-map", true, true)]
    [InlineData("region-map", false, true)]
    public void ArbitraryCaptionsFollowNumericPositionsAndActualSampleCenters(string family, bool gradient, bool right) {
        var scale = CaptionScale().WithCaptions(new[] {
            new ChartColorScaleCaption(-3, "Ice"), new ChartColorScaleCaption(5, "Thaw"), new ChartColorScaleCaption(10, "Threshold")
        });
        var chart = Create(family, scale);
        Dimension(chart, family).LegendStyle = gradient ? ChartColorLegendStyle.Gradient : ChartColorLegendStyle.Swatches;
        if (right) chart.WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var document = XDocument.Parse(svg); var prefix = Prefix(family);
        var source = CaptionSources(document, family).Single(node => (string?)node.Attribute("data-cfx-caption-value") == "10");
        Assert.Equal("authored", (string?)source.Attribute("data-cfx-caption-origin"));
        Assert.Equal("true", (string?)source.Attribute("data-cfx-caption-visible"));
        Assert.Equal("Threshold · 10", source.Descendants().Single(node => node.Name.LocalName == "text").Value);
        var ramp = gradient ? ((VisualSceneRectangle)Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>()).Shape).Bounds
            : CaptionRamp(prepared, prefix);
        // Asymmetric unpositioned stops sample [-20, 0, 13 1/3, 26 2/3, 40]. Ten lies 3/4 between sample1 and sample2.
        var ratio = gradient ? .5 : .45;
        var tick = source.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == prefix + "-caption-tick");
        Assert.Equal(right ? ramp.Top + ramp.Height * (1 - ratio) : ramp.Left + ramp.Width * ratio,
            Number(tick, right ? "y1" : "x1"), 3);
        Assert.Equal(ratio, Number(source, "data-cfx-caption-ratio"), 8);
        AssertMappedMarks(prepared, document, family, scale);
        if (gradient) AssertGradientPixels(prepared, Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>()), scale);
        else {
            var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == prefix + "-swatch").ToArray();
            var values = Role(document, prefix + "-step").Select(node => Number(node, "data-cfx-value")).ToArray();
            var expected = new[] { -20d, 0, 40d / 3, 80d / 3, 40 };
            Assert.Equal(expected.Length, values.Length);
            for (var index = 0; index < values.Length; index++) Assert.Equal(expected[index], values[index], 10);
            Assert.Equal(values.Select(value => scale.ColorFor(value)), swatches.Select(node => node.Fill!.Value));
        }
        AssertCaptionInkBounds(prepared, prefix);
        SetScale(chart, family, scale.WithCaptions(new[] { new ChartColorScaleCaption(99, "Changed") }));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        ScatterBubbleColorRenderingTests.Capture(prepared, "captions-" + family + (gradient ? "-gradient" : "-swatches") + (right ? "-right" : ""));
    }

    [Fact]
    public void ExplicitCoincidentCaptionsWinPaintWhileLegacyLabelsRemainDescriptive() {
        var scale = CaptionScale().WithCaptions(new[] {
            new ChartColorScaleCaption(-20, "Start"), new ChartColorScaleCaption(0, "Target"), new ChartColorScaleCaption(40, "End")
        });
        var chart = Create("heatmap", scale); Dimension(chart, "heatmap").LegendStyle = ChartColorLegendStyle.Gradient;
        var prepared = Prepare(chart); var source = CaptionSources(XDocument.Parse(prepared.ToSvg()), "heatmap");
        Assert.Equal(6, source.Length);
        Assert.All(source.Where(node => (string?)node.Attribute("data-cfx-caption-origin") == "authored"), node => {
            Assert.Equal("true", (string?)node.Attribute("data-cfx-caption-visible"));
            Assert.Single(node.Descendants(), child => child.Name.LocalName == "text");
        });
        Assert.All(source.Where(node => (string?)node.Attribute("data-cfx-caption-origin") != "authored"), node => {
            Assert.Equal("false", (string?)node.Attribute("data-cfx-caption-visible"));
            Assert.DoesNotContain(node.Descendants(), child => child.Name.LocalName == "text");
        });
        var description = prepared.Regions.Single(region => region.Role == "heatmap-scale").Label!;
        foreach (var text in new[] { "Cold · -20", "Zero · 0", "Hot · 40", "Start · -20", "Target · 0", "End · 40" }) Assert.Contains(text, description);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "color-scale.caption-overflow");
    }

    [Fact]
    public void CrowdedCaptionsKeepCompleteFactsWithoutOverprintingOrUnreadableType() {
        var captions = Enumerable.Range(0, 20).Select(index => new ChartColorScaleCaption(-19 + index * 3,
            "Authored threshold " + index + " with complete context")).ToArray();
        var chart = Create("heatmap", CaptionScale().WithCaptions(captions), 360);
        Dimension(chart, "heatmap").LegendStyle = ChartColorLegendStyle.Gradient;
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg()); var facts = CaptionSources(document, "heatmap");
        Assert.Equal(23, facts.Length);
        Assert.Equal(captions.Select(caption => caption.Text), facts.Where(node => (string?)node.Attribute("data-cfx-caption-origin") == "authored")
            .Select(node => (string?)node.Attribute("data-cfx-caption-label")));
        Assert.Contains(facts, node => (string?)node.Attribute("data-cfx-caption-origin") == "authored"
            && (string?)node.Attribute("data-cfx-caption-visible") == "true");
        Assert.Contains(facts, node => (string?)node.Attribute("data-cfx-caption-visible") == "false");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "color-scale.caption-overflow");
        AssertCaptionInkBounds(prepared, "heatmap-scale");
        Assert.All(facts, node => {
            Assert.Null(node.Attribute("data-cfx-target-id")); Assert.Null(node.Attribute("data-cfx-series")); Assert.Null(node.Attribute("tabindex"));
        });
        ScatterBubbleColorRenderingTests.Capture(prepared, "captions-crowded-heatmap-360");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MissingAndConstantDomainsRetainOutsideCaptionsWithoutClampedOrInventedAnchors(bool allMissing, bool fixedDomain) {
        var scale = ChartColorScale.Sequential(Cold, Hot).WithCaptions(new[] {
            new ChartColorScaleCaption(-2, "Outside low"), new ChartColorScaleCaption(7, "Observed"), new ChartColorScaleCaption(12, "Outside high")
        });
        if (fixedDomain) scale = scale.WithValueRange(0, 10);
        var chart = Chart.Create().WithSize(800, 480).WithHeatmapColorScale(scale)
            .AddHeatmapRows(new[] { ChartHeatmapRow.CreateMasked("Observations", allMissing ? new double?[] { null, null } : new double?[] { 7, 7 }) });
        chart.ConfigureHeatmapColorDimension(options => options.LegendStyle = ChartColorLegendStyle.Gradient);
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg()); var source = CaptionSources(document, "heatmap");
        var key = Assert.Single(Role(document, "heatmap-scale"));
        var outside = source.Where(node => (string?)node.Attribute("data-cfx-caption-label") is "Outside low" or "Outside high").ToArray();
        Assert.Equal(2, outside.Length);
        Assert.All(outside, node => {
            Assert.Equal("false", (string?)node.Attribute("data-cfx-caption-in-domain"));
            Assert.Equal("false", (string?)node.Attribute("data-cfx-caption-visible")); Assert.Null(node.Attribute("data-cfx-caption-ratio"));
            Assert.DoesNotContain(node.Descendants(), child => child.Name.LocalName == "text");
        });
        if (allMissing && !fixedDomain) {
            Assert.Empty(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
            Assert.Equal("false", (string?)key.Attribute("data-cfx-has-domain")); Assert.Null(key.Attribute("data-cfx-min-value"));
            Assert.Equal(3, source.Length);
            Assert.All(source, node => Assert.Equal("false", (string?)node.Attribute("data-cfx-caption-in-domain")));
            Assert.Empty(Role(document, "heatmap-scale-caption-tick")); Assert.NotEmpty(Role(document, "heatmap-scale-missing-swatch"));
        } else if (fixedDomain) {
            Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
            Assert.Equal("0", (string?)key.Attribute("data-cfx-min-value")); Assert.Equal("10", (string?)key.Attribute("data-cfx-max-value"));
            var observed = source.Single(node => (string?)node.Attribute("data-cfx-caption-label") == "Observed");
            Assert.Equal("true", (string?)observed.Attribute("data-cfx-caption-visible")); Assert.Equal(.7, Number(observed, "data-cfx-caption-ratio"));
            Assert.NotEmpty(Role(document, "heatmap-scale-missing-swatch"));
        } else {
            Assert.Empty(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
            Assert.Equal("7", (string?)key.Attribute("data-cfx-min-value")); Assert.Equal("7", (string?)key.Attribute("data-cfx-max-value"));
            var observed = source.Single(node => (string?)node.Attribute("data-cfx-caption-label") == "Observed");
            Assert.Equal("true", (string?)observed.Attribute("data-cfx-caption-visible")); Assert.Equal(.5, Number(observed, "data-cfx-caption-ratio"));
            Assert.Equal(Cold, prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "heatmap-scale-gradient").Fill);
        }
    }

    [Fact]
    public void CaptionFormattingUsesThePreparedDistinctValueCacheAndDeliberateEmptyNumericText() {
        var calls = new Dictionary<double, int>();
        var scale = CaptionScale().WithCaptions(new[] { new ChartColorScaleCaption(0, "Target"), new ChartColorScaleCaption(10, "Threshold") });
        var chart = Create("scatter", scale);
        Dimension(chart, "scatter").ValueFormat = ChartValueFormat.Custom(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return value == 10 ? "" : value.ToString("0.0", CultureInfo.InvariantCulture) + " °C";
        });
        var prepared = Prepare(chart); var snapshot = calls.ToDictionary(item => item.Key, item => item.Value);
        var svg = prepared.ToSvg(); _ = prepared.ToPng(); Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(snapshot.OrderBy(item => item.Key), calls.OrderBy(item => item.Key)); Assert.All(calls.Values, count => Assert.Equal(1, count));
        var threshold = CaptionSources(XDocument.Parse(svg), "scatter").Single(node => (string?)node.Attribute("data-cfx-caption-value") == "10");
        Assert.Equal("", (string?)threshold.Attribute("data-cfx-caption-formatted-value"));
        Assert.Equal("Threshold", threshold.Descendants().Single(node => node.Name.LocalName == "text").Value);
        Assert.Contains(10d, calls.Keys);
    }

    [Theory]
    [InlineData("scatter")]
    [InlineData("region-map")]
    [InlineData("heatmap")]
    [InlineData("treemap")]
    public void ClearingTheCaptionCatalogPreservesTheExistingNativeOutput(string family) {
        var scale = Scale(); var ordinary = Prepare(Create(family, scale));
        var cleared = scale.WithCaptions(new[] { new ChartColorScaleCaption(10, "Threshold") }).WithCaptions(Array.Empty<ChartColorScaleCaption>());
        var restored = Prepare(Create(family, cleared));
        Assert.Equal(ordinary.ToSvg(), restored.ToSvg()); Assert.Equal(ordinary.ToPng(), restored.ToPng());
    }

    private static ChartColorScale CaptionScale() => ChartColorScale.Diverging(new[] { Cold }, ChartColor.White, new[] { Cool, Warm, Hot }, 0)
        .WithValueRange(-20, 40).WithLabels("Cold", "Zero", "Hot").WithNoDataColor(Missing);

    private static XElement[] CaptionSources(XDocument document, string family) => Role(document, Prefix(family) + "-caption-source");

    private static ChartRect CaptionRamp(PreparedVisual prepared, string prefix) {
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == prefix + "-swatch").ToArray();
        return new ChartRect(swatches.Min(node => node.Bounds.Left), swatches.Min(node => node.Bounds.Top),
            swatches.Max(node => node.Bounds.Right) - swatches.Min(node => node.Bounds.Left), swatches.Max(node => node.Bounds.Bottom) - swatches.Min(node => node.Bounds.Top));
    }

    private static void AssertCaptionInkBounds(PreparedVisual prepared, string prefix) {
        var key = prepared.Regions.Single(region => region.Role == prefix).Bounds;
        var text = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == prefix + "-label").ToArray();
        Assert.NotEmpty(text);
        var boxes = text.Select(node => new ChartRect(node.LineLeft(node.Text.Lines[0]), node.Baseline - node.Text.Metrics.LineHeight,
            node.Text.Metrics.Width, node.Text.Metrics.Height)).ToArray();
        for (var index = 0; index < text.Length; index++) {
            Assert.True(text[index].Text.Size >= 10); var box = boxes[index];
            Assert.InRange(box.Left, key.Left - .01, key.Right); Assert.InRange(box.Right, key.Left, key.Right + .01);
            Assert.InRange(box.Top, key.Top - .01, key.Bottom); Assert.InRange(box.Bottom, key.Top, key.Bottom + .01);
            for (var other = 0; other < index; other++) Assert.False(box.Left < boxes[other].Right && box.Right > boxes[other].Left
                && box.Top < boxes[other].Bottom && box.Bottom > boxes[other].Top, "Caption glyph boxes must not overlap.");
        }
    }
}
