using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class StageConeCaptionContrastTests {
    public static IEnumerable<object[]> ComponentSurfaces() {
        foreach (var component in new[] { "category", "value", "retention", "dropoff" })
            foreach (var orientation in new[] { ChartOrientation.Vertical, ChartOrientation.Horizontal })
                foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) yield return new object[] { component, orientation, mode };
    }

    [Theory]
    [MemberData(nameof(ComponentSurfaces))]
    public void EveryContainedConeComponentUsesTheRealIncomingOverrideOrHiddenFrame(string component, ChartOrientation orientation, VisualThemeMode mode) {
        var chart = Component(false, ChartFunnelForm.Cone, orientation, mode, component);
        var raw = chart.Series[0].Points.ToArray(); var prior = StagePresentationOptionsTests.Prepare(chart); var oldSvg = prior.ToSvg(); var oldPng = prior.ToPng();
        var inherited = mode == VisualThemeMode.Dark ? ChartColor.Black : ChartColor.White;
        var overridden = mode == VisualThemeMode.Dark ? ChartColor.White : ChartColor.Black;
        chart.ConfigureFunnel(options => options.ConnectorStyle = new ChartFunnelConnectorStyle(fill: inherited, fillOpacity: 1));
        foreach (var variant in new[] { "hidden", "alpha", "opaque" }) {
            chart.Series[0].WithFunnelConnectorStyle(0, new ChartFunnelConnectorStyle(visible: variant != "hidden", fill: overridden,
                fillOpacity: variant == "opaque" ? 1 : .12));
            var prepared = StagePresentationOptionsTests.Prepare(chart); var texts = CaptionTexts(prepared, component);
            Assert.Equal(3, texts.Length);
            ScatterBubbleColorRenderingTests.Capture(prepared, $"cone-surface-{component}-{orientation}-{mode}-{variant}");
            var underneath = WithoutText(prepared);
            foreach (var text in texts) {
                Assert.False(text.Stroke.HasValue); var bounds = prepared.Regions.Single(region => region.Id == text.Id).Bounds;
                var surface = Pixel(underneath, bounds); Assert.True(ChartColorMath.ContrastRatio(text.Color, surface) >= 4.5,
                    $"{component}/{variant}/{text.Id}: {text.Color.ToCss()} does not contrast with actual {surface.ToCss()}.");
                Assert.Equal(text.Color.ToCss(), SvgText(prepared, text.Id!).Attribute("fill")!.Value);
            }
            var painted = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "funnel-connection-area").ToArray();
            Assert.Equal(variant == "hidden" ? 1 : 2, painted.Length);
            if (variant != "hidden") Assert.Equal(ChartColorMath.WithOpacity(overridden, variant == "opaque" ? 1 : .12), painted[0].Fill);
            Assert.Equal(raw, chart.Series[0].Points); Assert.Equal(oldSvg, prior.ToSvg()); Assert.Equal(oldPng, prior.ToPng());
        }
    }

    [Theory]
    [InlineData(false, ChartOrientation.Vertical, VisualThemeMode.Light)]
    [InlineData(false, ChartOrientation.Vertical, VisualThemeMode.Dark)]
    [InlineData(false, ChartOrientation.Horizontal, VisualThemeMode.Light)]
    [InlineData(false, ChartOrientation.Horizontal, VisualThemeMode.Dark)]
    [InlineData(true, ChartOrientation.Vertical, VisualThemeMode.Light)]
    [InlineData(true, ChartOrientation.Vertical, VisualThemeMode.Dark)]
    [InlineData(true, ChartOrientation.Horizontal, VisualThemeMode.Light)]
    [InlineData(true, ChartOrientation.Horizontal, VisualThemeMode.Dark)]
    public void AlphaFilledPartitionsUseTheirPaintedFrameWithoutChangingTheAuthoredAlpha(bool pyramid, ChartOrientation orientation, VisualThemeMode mode) {
        var chart = Component(pyramid, ChartFunnelForm.StageBars, orientation, mode, "value");
        var fill = (mode == VisualThemeMode.Dark ? ChartColor.White : ChartColor.Black).WithAlpha(24); chart.Series[0].WithPointColor(0, fill);
        var prepared = StagePresentationOptionsTests.Prepare(chart); var first = CaptionTexts(prepared, "value").Single(text => text.Id == "series-0-point-0-value-label");
        ScatterBubbleColorRenderingTests.Capture(prepared, $"stage-alpha-caption-{pyramid}-{orientation}-{mode}");
        Assert.False(first.Stroke.HasValue); var surface = Pixel(WithoutText(prepared), prepared.Regions.Single(region => region.Id == first.Id).Bounds);
        Assert.True(ChartColorMath.ContrastRatio(first.Color, surface) >= 4.5); Assert.Equal(fill, chart.Series[0].PointColors[0]);
        Assert.Equal(first.Color.ToCss(), SvgText(prepared, first.Id!).Attribute("fill")!.Value); Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false, ChartOrientation.Vertical)]
    [InlineData(false, ChartOrientation.Horizontal)]
    [InlineData(true, ChartOrientation.Vertical)]
    [InlineData(true, ChartOrientation.Horizontal)]
    public void OpaquePartitionsRetainTheIncumbentInkPaintAndHaveNoHalo(bool pyramid, ChartOrientation orientation) {
        var chart = Component(pyramid, ChartFunnelForm.StageBars, orientation, VisualThemeMode.Dark, "value");
        var fill = ChartColor.FromHex("#C8A5EF"); chart.Series[0].WithColor(fill);
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        Assert.Equal(3, CaptionTexts(prepared, "value").Length);
        foreach (var text in CaptionTexts(prepared, "value")) {
            Assert.Equal(ChartColorMath.AccessibleTextOnBackground(fill), text.Color);
            Assert.False(text.Stroke.HasValue); Assert.Equal(0, text.StrokeWidth);
            Assert.Equal(SvgPaint.Contrast(fill, VisualChartPaint.SeriesRole(chart.Series[0], 0)).Value, text.Paint!.Value.Value);
        }
    }

    [Theory]
    [InlineData("component")]
    [InlineData("chart")]
    [InlineData("series")]
    [InlineData("point")]
    public void ExplicitInkAtEachExistingLevelRemainsAuthoritativeWithoutAnAutomaticHalo(string level) {
        var chart = Component(false, ChartFunnelForm.Cone, ChartOrientation.Horizontal, VisualThemeMode.Dark, "value");
        var ink = ChartColor.FromHex("#B94168");
        if (level == "component") chart.Options.Funnel.ValueLabel.Style.Color = ink;
        else if (level == "chart") chart.Options.DataLabelStyle.Color = ink;
        else if (level == "series") chart.Series[0].DataLabelStyle.Color = ink;
        else chart.Series[0].ConfigurePointDataLabelStyle(0, style => style.Color = ink);
        var prepared = StagePresentationOptionsTests.Prepare(chart); var text = CaptionTexts(prepared, "value").Single(item => item.Id == "series-0-point-0-value-label");
        Assert.Equal(ink, text.Color); Assert.False(text.Stroke.HasValue); Assert.Equal(ink.ToCss(), SvgText(prepared, text.Id!).Attribute("fill")!.Value);
    }

    [Theory]
    [InlineData(ChartOrientation.Vertical, 390, VisualThemeMode.Light)]
    [InlineData(ChartOrientation.Vertical, 800, VisualThemeMode.Dark)]
    [InlineData(ChartOrientation.Horizontal, 390, VisualThemeMode.Dark)]
    [InlineData(ChartOrientation.Horizontal, 800, VisualThemeMode.Light)]
    public void PatternedConeSurfacesUseTheExistingRealGlyphHaloInBothStaticBackends(ChartOrientation orientation, int width, VisualThemeMode mode) {
        var chart = Component(false, ChartFunnelForm.Cone, orientation, mode, "value").WithSize(width, 640);
        chart.Series[0].WithFunnelConnectorStyle(0, new ChartFunnelConnectorStyle(fill: ChartColor.White, fillOpacity: 1, fillPattern: ChartFillPattern.Crosshatch));
        var prepared = StagePresentationOptionsTests.Prepare(chart); var text = CaptionTexts(prepared, "value").Single(item => item.Id == "series-0-point-1-value-label");
        ScatterBubbleColorRenderingTests.Capture(prepared, $"cone-pattern-halo-{orientation}-{width}-{mode}");
        Assert.True(text.Stroke.HasValue); Assert.True(text.StrokeWidth > 0); Assert.True(ChartColorMath.ContrastRatio(text.Color, text.Stroke!.Value) >= 4.5);
        var svg = SvgText(prepared, text.Id!); Assert.Equal(text.Color.ToCss(), svg.Attribute("fill")!.Value);
        Assert.Equal(text.Stroke!.Value.ToCss(), svg.Attribute("stroke")!.Value); Assert.Equal("stroke", svg.Attribute("paint-order")!.Value);
        var scene = prepared.Scene; var noHalo = scene.Nodes.Select(node => ReferenceEquals(node, text)
            ? (VisualSceneNode)new VisualSceneText(text.Text, text.X, text.Baseline, text.Color, text.Alignment, text.Role, text.Id, text.Paint) : node).ToArray();
        var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size, noHalo, scene.Diagnostics, scene.Regions, scene.DataProjection));
        var actual = prepared.ToRgba(); Assert.True(actual.Pixels.Where((value, index) => value != without.Pixels[index]).Count() > 4,
            "The readability halo must paint native glyph pixels, not just an SVG attribute.");
        Assert.Equal(3, chart.Series[0].Points.Count); Assert.Equal(new[] { 100d, 75, 25 }, chart.Series[0].Points.Select(point => point.Y));
    }

    [Theory]
    [InlineData(ChartOrientation.Vertical, VisualThemeMode.Light)]
    [InlineData(ChartOrientation.Vertical, VisualThemeMode.Dark)]
    [InlineData(ChartOrientation.Horizontal, VisualThemeMode.Light)]
    [InlineData(ChartOrientation.Horizontal, VisualThemeMode.Dark)]
    public void ZeroFunnelStagesKeepSourceFactsWhenTheRealZeroStrokePreventsAnInsideCaption(ChartOrientation orientation, VisualThemeMode mode) {
        var chart = Component(false, ChartFunnelForm.StageBars, orientation, mode, "value"); chart.Series[0].Points[0] = new ChartPoint(1, 0);
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"zero-stage-caption-{orientation}-{mode}");
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(path => path.Role == "funnel-segment"));
        Assert.Single(prepared.Scene.Nodes, node => node.Role == "funnel-zero");
        Assert.DoesNotContain(CaptionTexts(prepared, "value"), text => text.Id == "series-0-point-0-value-label");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "funnel.label-hidden");
        Assert.Equal("0", prepared.Regions.Single(region => region.Id == "series-0-point-0-value-label").Label);
        Assert.Equal(0, chart.Series[0].Points[0].Y); Assert.NotEmpty(prepared.ToPng());
    }

    private static Chart Component(bool pyramid, ChartFunnelForm form, ChartOrientation orientation, VisualThemeMode mode, string component) {
        var chart = StagePresentationOptionsTests.Create(pyramid, form, orientation, 900, 800).WithTheme(VisualTheme.Graphite(), mode);
        chart.ConfigureFunnel(options => {
            options.LabelContent = ChartStageLabelContent.Separate; options.CategoryLabel.Visible = options.ValueLabel.Visible = options.RetentionLabel.Visible = options.DropOffLabel.Visible = false;
            var policy = component == "category" ? options.CategoryLabel : component == "retention" ? options.RetentionLabel : component == "dropoff" ? options.DropOffLabel : options.ValueLabel;
            policy.Visible = true; policy.Placement = component == "value" ? ChartDataLabelPlacement.Inside : ChartDataLabelPlacement.Center; policy.Style.FontSize = 11;
        });
        chart.ConfigurePyramid(options => {
            options.LabelContent = ChartStageLabelContent.Separate; options.CategoryLabel.Visible = false; options.ValueLabel.Visible = true;
            options.ValueLabel.Placement = ChartDataLabelPlacement.Inside; options.ValueLabel.Style.FontSize = 11;
        });
        return chart;
    }
    private static VisualSceneText[] CaptionTexts(PreparedVisual prepared, string component) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role == (prepared.Scene.Nodes.OfType<VisualSceneGroup>().Any(group => group.Role == "pyramid-chart") ? "pyramid-" : "funnel-") + component + "-label").ToArray();
    private static XElement SvgText(PreparedVisual prepared, string id) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Single(node => (string?)node.Attribute("data-cfx-source-id") == id).Descendants().Single(node => node.Name.LocalName == "text");
    private static RgbaImage WithoutText(PreparedVisual prepared) {
        var scene = prepared.Scene;
        return VisualSceneRasterRenderer.Render(new VisualScene(scene.Size, scene.Nodes.Where(node => node is not VisualSceneText).ToArray(), scene.Diagnostics, scene.Regions, scene.DataProjection));
    }
    private static ChartColor Pixel(RgbaImage image, ChartRect bounds) {
        var x = (int)Math.Floor(bounds.Left + bounds.Width / 2); var y = (int)Math.Floor(bounds.Top + bounds.Height / 2); var index = (y * image.Width + x) * 4;
        return ChartColor.FromRgba(image.Pixels[index], image.Pixels[index + 1], image.Pixels[index + 2], image.Pixels[index + 3]);
    }
}
