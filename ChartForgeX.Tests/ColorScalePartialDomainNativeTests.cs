using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Native marks, keys and raw observations use the same independently resolved continuous endpoints.</summary>
public sealed partial class ColorScalePartialDomainNativeTests {
    [Theory]
    [InlineData("tile-map", true, false, 360)]
    [InlineData("region-map", false, true, 800)]
    [InlineData("heatmap", true, true, 360)]
    [InlineData("hexbin", false, false, 800)]
    [InlineData("calendar", false, true, 800)]
    [InlineData("scatter", true, true, 360)]
    [InlineData("bubble", false, false, 800)]
    [InlineData("treemap", true, true, 800)]
    [InlineData("sunburst", false, false, 360)]
    public void PartialBoundsKeepMeasuredNativeKeysMappedMarksAndRawFacts(string family, bool fixMinimum, bool gradient, int width) {
        var scale = ChartColorScale.Sequential(Low, High).WithValueRange(fixMinimum ? 0 : null, fixMinimum ? null : 40).WithNoDataColor(Missing);
        var chart = Create(family, new double?[] { 10, 20, 30, null }, width, dark: width == 360);
        SetScale(chart, family, scale);
        Dimension(chart, family).LegendStyle = gradient ? ChartColorLegendStyle.Gradient : ChartColorLegendStyle.Swatches;
        if (family == "region-map") chart.WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg()); var image = Image(prepared);
        var prefix = Prefix(family); var key = Role(document, prefix).Single();
        var low = fixMinimum ? 0 : 10; var high = fixMinimum ? 30 : 40;
        Assert.Equal(low.ToString(CultureInfo.InvariantCulture), (string?)key.Attribute("data-cfx-min-value"));
        Assert.Equal(high.ToString(CultureInfo.InvariantCulture), (string?)key.Attribute("data-cfx-max-value"));
        Assert.Equal((low + 15).ToString(CultureInfo.InvariantCulture), (string?)key.Attribute("data-cfx-midpoint-value"));
        if (gradient) {
            var node = prepared.Scene.Nodes.OfType<VisualSceneGradient>().Single(item => item.Role == prefix + "-gradient");
            Assert.Equal(new[] { 0d, 1 }, node.Stops.Select(stop => stop.Offset));
            Assert.Equal(new[] { Low, High }, node.Stops.Select(stop => stop.Color));
            var box = Assert.IsType<VisualSceneRectangle>(node.Shape).Bounds;
            AssertPixel(image, box.Left + box.Width / 2, box.Top + box.Height / 2, scale.ColorFor(low + 15, 10, 30), tolerance: 3);
            Assert.Contains(document.Descendants(), element => element.Name.LocalName == "linearGradient");
        } else {
            var sourceRole = family is "tile-map" or "region-map" or "treemap" or "sunburst" ? prefix + "-step-source" : prefix + "-step";
            var samples = Role(document, sourceRole).Select(node => Number(node, "data-cfx-value")).ToArray();
            Assert.Equal(Enumerable.Range(0, 5).Select(index => low + index * 7.5), samples);
            var swatchRole = family is "tile-map" or "region-map" or "treemap" or "sunburst" ? prefix + "-step" : prefix + "-swatch";
            var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == swatchRole).ToArray();
            Assert.Equal(5, swatches.Length);
            for (var index = 0; index < swatches.Length; index++) {
                var expected = scale.ColorFor(samples[index], 10, 30); Assert.Equal(expected, swatches[index].Fill);
                var box = swatches[index].Bounds; AssertPixel(image, box.Left + box.Width / 2, box.Top + box.Height / 2, expected);
            }
        }
        AssertRawMarks(prepared, document, image, family, scale, 10, 30);
        AssertKeyBounds(prepared, prefix, width, 480);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "matrix.scale-overflow" or "cartesian.color-scale-overflow");
        ScatterBubbleColorRenderingTests.Capture(prepared, "partial-domain-" + family);
    }

    [Theory]
    [InlineData("tile-map", false)]
    [InlineData("heatmap", true)]
    [InlineData("bubble", false)]
    [InlineData("treemap", true)]
    public void CollapsedPartialKeyAndMarksUseTheFixedEndpointsPaintWithoutChangingRawValues(string family, bool fixMinimum) {
        var scale = ChartColorScale.Sequential(Low, High).WithValueRange(fixMinimum ? 40 : null, fixMinimum ? null : 10);
        var chart = Create(family, new double?[] { 20, 25, 30 }); SetScale(chart, family, scale);
        Dimension(chart, family).LegendStyle = ChartColorLegendStyle.Gradient;
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg()); var image = Image(prepared); var prefix = Prefix(family);
        var boundary = fixMinimum ? "40" : "10"; var key = Role(document, prefix).Single();
        Assert.Equal(boundary, (string?)key.Attribute("data-cfx-min-value")); Assert.Equal(boundary, (string?)key.Attribute("data-cfx-max-value"));
        var solid = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == prefix + "-gradient");
        var expected = fixMinimum ? Low : High; Assert.Equal(expected, solid.Fill);
        AssertPixel(image, solid.Bounds.Left + solid.Bounds.Width / 2, solid.Bounds.Top + solid.Bounds.Height / 2, expected);
        AssertRawMarks(prepared, document, image, family, scale, 20, 30);
        if (family == "tile-map") Assert.All(Role(document, SourceRole(family)).Where(source => source.Attribute("data-cfx-level") != null),
            source => Assert.Equal("4", (string?)source.Attribute("data-cfx-level")));
        ScatterBubbleColorRenderingTests.Capture(prepared, "partial-collapse-" + family);
    }

    [Theory]
    [InlineData("tile-map", true)]
    [InlineData("region-map", false)]
    [InlineData("heatmap", false)]
    [InlineData("hexbin", true)]
    [InlineData("scatter", true)]
    [InlineData("bubble", false)]
    [InlineData("treemap", true)]
    [InlineData("sunburst", false)]
    public void AllMissingObservationsDoNotInventAnInferredExtentButAuthoredPairsRemainDrawable(string family, bool fixMinimum) {
        var automatic = ChartColorScale.Diverging(Low, ChartColor.White, High, 0).WithNoDataColor(Missing).WithLabels(null, "Center", null);
        var partial = automatic.WithValueRange(fixMinimum ? 15 : null, fixMinimum ? null : -15);
        var chart = Create(family, new double?[] { null, null, null }, 360); SetScale(chart, family, partial);
        if (family == "region-map") chart.WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        var calls = 0;
        Dimension(chart, family).ValueFormat = ChartValueFormat.Custom(value => { calls++; return value.ToString(CultureInfo.InvariantCulture); });
        foreach (var scale in new[] { partial, partial.WithValueRange(null, null) }) {
            SetScale(chart, family, scale);
            var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg()); var prefix = Prefix(family);
            ScatterBubbleColorRenderingTests.Capture(prepared, "partial-missing-" + family + (ReferenceEquals(scale, partial) ? "-partial" : "-automatic"));
            var key = Role(document, prefix).Single();
            Assert.Null(key.Attribute("data-cfx-min-value")); Assert.Null(key.Attribute("data-cfx-max-value")); Assert.Null(key.Attribute("data-cfx-midpoint-value"));
            Assert.Empty(Role(document, prefix + "-step")); Assert.Empty(Role(document, prefix + "-step-source"));
            Assert.Contains("No data", key.Value, StringComparison.Ordinal);
            var missingRole = family is "treemap" or "sunburst" ? prefix + "-missing" : prefix + "-missing-swatch";
            var swatch = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == missingRole);
            Assert.Equal(Missing, swatch.Fill); AssertPixel(Image(prepared), swatch.Bounds.Left + swatch.Bounds.Width / 2, swatch.Bounds.Top + swatch.Bounds.Height / 2, Missing);
            Assert.All(Role(document, SourceRole(family)), source => Assert.True(string.IsNullOrEmpty((string?)source.Attribute(ValueAttribute(family)))));
            if (family is "tile-map" or "region-map") {
                var source = Role(document, "map-series").Single(); Assert.Null(source.Attribute("data-cfx-min-value")); Assert.Null(source.Attribute("data-cfx-max-value"));
            }
            if (family is "heatmap" or "hexbin") {
                var source = Role(document, family == "heatmap" ? "heatmap" : "hexbin-heatmap").Single(); Assert.Null(source.Attribute("data-cfx-min")); Assert.Null(source.Attribute("data-cfx-max"));
            }
        }
        Assert.Equal(0, calls);
        Dimension(chart, family).ValueFormat = null; SetScale(chart, family, automatic.WithValueRange(-10, 10));
        var fixedDocument = XDocument.Parse(Prepare(chart).ToSvg()); var fixedKey = Role(fixedDocument, Prefix(family)).Single();
        Assert.Equal("-10", (string?)fixedKey.Attribute("data-cfx-min-value")); Assert.Equal("10", (string?)fixedKey.Attribute("data-cfx-max-value"));
        Assert.NotEmpty(Role(fixedDocument, Prefix(family) + "-step"));
    }

    [Theory]
    [InlineData("calendar")]
    [InlineData("scatter")]
    public void EmptySourceDoesNotCreateNumericBoundsOrMeasurements(string family) {
        var chart = Create(family, Array.Empty<double?>()); SetScale(chart, family, ChartColorScale.Sequential(Low, High).WithValueRange(15, null));
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        Assert.Empty(Role(document, Prefix(family))); Assert.Empty(Role(document, SourceRole(family)));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData("tile-map")]
    [InlineData("scatter")]
    public void ResetRestoresExactAutomaticSvgAndNativePngAndPreparedScenesStayDetached(string family) {
        var automatic = ChartColorScale.Sequential(Low, High).WithNoDataColor(Missing);
        var chart = Create(family, new double?[] { 10, 20, 30, null }); SetScale(chart, family, automatic);
        var baseline = Prepare(chart); var svg = baseline.ToSvg(); var png = baseline.ToPng();
        var partial = automatic.WithValueRange(0, null); SetScale(chart, family, partial);
        var changed = Prepare(chart); Assert.NotEqual(svg, changed.ToSvg()); var captured = changed.ToSvg();
        SetScale(chart, family, partial.WithValueRange(null, null));
        var reset = Prepare(chart); Assert.Equal(svg, reset.ToSvg()); Assert.Equal(png, reset.ToPng()); Assert.Equal(captured, changed.ToSvg());
    }

    [Theory]
    [InlineData(ChartSeriesKind.TileMap, false, false)]
    [InlineData(ChartSeriesKind.TileMap, true, false)]
    [InlineData(ChartSeriesKind.TileMap, false, true)]
    [InlineData(ChartSeriesKind.TileMap, true, true)]
    [InlineData(ChartSeriesKind.Scatter, false, false)]
    [InlineData(ChartSeriesKind.Scatter, true, false)]
    [InlineData(ChartSeriesKind.Scatter, false, true)]
    [InlineData(ChartSeriesKind.Scatter, true, true)]
    public void ExecutablePartialRangeExamplesKeepTheirPinnedConstraintAcrossViewportAndTheme(ChartSeriesKind kind, bool compact, bool dark) {
        var variant = compact ? "compact-partial-color-range" : "partial-color-range";
        var chart = V2GalleryModels.Create(kind, variant, dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(compact ? 360 : 800, compact ? 360 : 440).WithDataLabels(false).WithPngSupersampling(1)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        var key = Role(document, kind == ChartSeriesKind.TileMap ? "map-scale" : "color-scale-0").Single();
        Assert.Equal(kind == ChartSeriesKind.TileMap ? "0" : "55", (string?)key.Attribute("data-cfx-min-value"));
        Assert.Equal(kind == ChartSeriesKind.TileMap ? "76" : "100", (string?)key.Attribute("data-cfx-max-value"));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "matrix.scale-overflow" or "cartesian.color-scale-overflow");
        Assert.NotEmpty(prepared.ToPng());
        ScatterBubbleColorRenderingTests.Capture(prepared, "partial-example-" + kind + "-" + variant + (dark ? "-dark" : "-light"));
    }

    private static void AssertRawMarks(PreparedVisual prepared, XDocument document, ChartForgeX.Raster.RgbaImage image,
        string family, ChartColorScale scale, double min, double max) {
        var observed = Role(document, SourceRole(family)).Where(source => double.TryParse((string?)source.Attribute(ValueAttribute(family)), NumberStyles.Float, CultureInfo.InvariantCulture, out _)).ToArray();
        Assert.Equal(new[] { min, (min + max) / 2, max }, observed.Select(source => Number(source, ValueAttribute(family))).OrderBy(value => value));
        foreach (var source in observed) {
            var value = Number(source, ValueAttribute(family)); var expected = scale.ColorFor(value, min, max);
            var shape = source.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == MarkRole(family));
            var paint = family == "bubble" ? ChartColorMath.WithOpacity(expected, ChartVisualPrimitives.BubbleFillOpacity) : expected;
            Assert.Equal(paint.ToCss(), (string?)shape.Attribute("fill"));
            if (family is "scatter" or "bubble") {
                AssertPixel(image, Number(shape, "cx"), Number(shape, "cy"), paint);
                Assert.True(source.Attribute("data-cfx-y") != null);
                if (family == "bubble") Assert.Equal("36", (string?)source.Attribute("data-cfx-size"));
            } else if (family is "heatmap" or "hexbin" or "calendar" or "tile-map" or "treemap") {
                var box = prepared.Regions.Single(region => region.Id == (string?)source.Attribute("data-cfx-source-id")).Bounds;
                AssertPixel(image, box.Left + box.Width / 2, box.Top + box.Height / 2, expected);
            }
        }
    }
}
