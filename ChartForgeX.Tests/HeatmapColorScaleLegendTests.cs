using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Checks measured keys, exact numeric intervals, native paint and calendar missing-day separation.</summary>
public sealed class HeatmapColorScaleLegendTests {
    private static readonly ChartColor Low = ChartColor.FromHex("#DAE8F8"), Middle = ChartColor.FromHex("#6F9ECE"), High = ChartColor.FromHex("#1C5CAB");

    [Theory]
    [InlineData(ChartSeriesKind.Heatmap, 360, false, "discrete")]
    [InlineData(ChartSeriesKind.HexbinHeatmap, 800, true, "discrete")]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 360, true, "discrete")]
    [InlineData(ChartSeriesKind.Heatmap, 800, true, "sequential")]
    [InlineData(ChartSeriesKind.HexbinHeatmap, 360, false, "sequential")]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 800, false, "sequential")]
    [InlineData(ChartSeriesKind.Heatmap, 360, true, "diverging")]
    [InlineData(ChartSeriesKind.HexbinHeatmap, 800, false, "diverging")]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 360, false, "diverging")]
    public void CanonicalLegendsFitAndMatchSvgAndNativeCellPaint(ChartSeriesKind kind, int width, bool dark, string mode) {
        var chart = HeatmapColorScaleTests.Create(kind, 0, 10, 20).WithSize(width, 400)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.ConfigureLabels(labels => labels.Value = "wartość");
        var scale = mode == "discrete" ? ChartColorScale.Discrete(new[] {
            new ChartColorBand(10, Low, "Niski"), new ChartColorBand(20, Middle, "Średni"), new ChartColorBand(null, High, "Wysoki")
        }) : mode == "diverging" ? ChartColorScale.Diverging(Low, Middle, High, 10).WithValueRange(0, 20).WithLabels("Low", "Target", "High")
            : ChartColorScale.Sequential(new[] { Low, ChartColor.FromHex("#B2CDE8"), Middle, ChartColor.FromHex("#4680BC"), High }).WithValueRange(0, 20);
        chart.WithHeatmapColorScale(scale);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var document = XDocument.Parse(prepared.ToSvg());
        var prefix = kind == ChartSeriesKind.CalendarHeatmap ? "calendar-scale" : "heatmap-scale";
        var steps = Role(document, prefix + "-step");
        Assert.Equal(mode, Role(document, prefix).Single().Attribute("data-cfx-scale-mode")?.Value);
        if (mode == "discrete") {
            Assert.Equal(new[] { "Niski · < 10", "Średni · 10 ≤ wartość < 20", "Wysoki · ≥ 20" }, steps.Select(step => (string?)step.Attribute("data-cfx-label")));
            Assert.Equal(new[] { "", "10", "20" }, steps.Select(step => (string?)step.Attribute("data-cfx-lower-bound")));
            Assert.Equal(new[] { "10", "20", "" }, steps.Select(step => (string?)step.Attribute("data-cfx-upper-bound")));
            Assert.All(steps, step => Assert.Equal("true", (string?)step.Attribute("data-cfx-upper-exclusive")));
        } else {
            Assert.Equal(new[] { "0", "5", "10", "15", "20" }, steps.Select(step => (string?)step.Attribute("data-cfx-value")));
            Assert.Equal("0", (string?)Role(document, prefix).Single().Attribute("data-cfx-min-value"));
            Assert.Equal("20", (string?)Role(document, prefix).Single().Attribute("data-cfx-max-value"));
            if (mode == "diverging") Assert.Contains(prepared.Regions, region => region.Label == "Target · 10");
        }
        var expected = mode == "discrete" ? scale.Bands.Select(band => band.Color).ToArray()
            : new[] { 0d, 5, 10, 15, 20 }.Select(value => scale.ColorFor(value, 0, 20)).ToArray();
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == prefix + "-swatch").ToArray();
        Assert.Equal(expected, swatches.Select(swatch => swatch.Fill!.Value));
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        for (var index = 0; index < swatches.Length; index++) Assert.Equal(expected[index], Pixel(image, swatches[index].Bounds));
        var cellRoles = new[] { "heatmap-cell", "hexbin-cell", "calendar-cell" };
        var filled = prepared.Regions.Where(region => cellRoles.Contains(region.Role) && region.Label?.EndsWith(": No data", StringComparison.Ordinal) != true).ToArray();
        Assert.Equal(3, filled.Length);
        for (var index = 0; index < filled.Length; index++) Assert.Equal(scale.ColorFor(index * 10, 0, 20), Pixel(image, filled[index].Bounds));
        var key = prepared.Regions.Single(region => region.Role == prefix);
        Assert.True(filled.Max(cell => cell.Bounds.Bottom) <= key.Bounds.Top);
        Assert.All(prepared.Regions.Where(region => region.Role == prefix + "-label"), label => {
            Assert.True(label.Bounds.Top >= key.Bounds.Top && label.Bounds.Bottom <= key.Bounds.Bottom + .000001);
            Assert.True(label.Bounds.Left >= key.Bounds.Left && label.Bounds.Right <= key.Bounds.Right + .000001);
        });
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "matrix.scale-overflow");
        chart.ConfigureHeatmapColorDimension(options => options.ShowLegend = false);
        Assert.DoesNotContain(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Regions, region => region.Role == prefix);
        var hiddenContext = VisualExportRequest.ForChart(chart.ConfigureHeatmapColorDimension(options => options.ShowLegend = true)).Context;
        var frameHidden = new VisualRenderContext(hiddenContext.Layout, hiddenContext.Theme, hiddenContext.ThemeMode, new VisualFrame(showLegend: false), hiddenContext.Font);
        Assert.DoesNotContain(chart.Prepare(frameHidden).Regions, region => region.Role == prefix);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompactGalleryBandsRenderTheirCompleteIntervalsAtReadableSizeBelowTheHeader(bool dark) {
        const string family = "CFX Heatmap Legend Carlito";
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var chart = V2GalleryModels.Create(ChartSeriesKind.HexbinHeatmap, "compact-color-scale", mode);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 360)),
            VisualTheme.Graphite(), mode, new VisualFrame("Service coverage in hexagonal cells", "Named numeric intervals keep their exact boundaries"),
            FontSpec.FromFamily(family)));
        var document = XDocument.Parse(prepared.ToSvg());
        var labelGroups = Role(document, "heatmap-scale-label");
        var captions = labelGroups.Where(group => ((string?)group.Attribute("data-cfx-source-id"))?.StartsWith("heatmap-scale-label-band-", StringComparison.Ordinal) == true)
            .SelectMany(group => group.Descendants().Where(element => element.Name.LocalName == "text")).ToArray();
        Assert.Equal(new[] { "Low · < 40", "Medium · 40 ≤ value < 80", "High · ≥ 80" }, captions.Select(caption => caption.Value));
        Assert.All(captions, caption => Assert.True(double.Parse(caption.Attribute("font-size")!.Value, CultureInfo.InvariantCulture) >= 10));
        var missing = Assert.Single(labelGroups, group => (string?)group.Attribute("data-cfx-source-id") == "heatmap-scale-label-missing");
        Assert.Equal("No data", Assert.Single(missing.Descendants(), element => element.Name.LocalName == "text").Value);
        var key = prepared.Regions.Single(region => region.Role == "heatmap-scale").Bounds;
        var text = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "heatmap-scale-label").ToArray();
        Assert.Equal(4, text.Length);
        var boxes = text.Select(node => new ChartRect(node.LineLeft(node.Text.Lines[0]), node.Baseline - node.Text.Ascent,
            node.Text.Metrics.Width, node.Text.Metrics.Height)).ToArray();
        for (var index = 0; index < text.Length; index++) {
            Assert.True(text[index].Text.Size >= 10);
            Assert.InRange(boxes[index].Left, key.Left - .01, key.Right); Assert.InRange(boxes[index].Right, key.Left, key.Right + .01);
            Assert.InRange(boxes[index].Top, key.Top - .01, key.Bottom); Assert.InRange(boxes[index].Bottom, key.Top, key.Bottom + .01);
            for (var other = 0; other < index; other++) Assert.False(boxes[index].Left < boxes[other].Right && boxes[index].Right > boxes[other].Left
                && boxes[index].Top < boxes[other].Bottom && boxes[index].Bottom > boxes[other].Top, "Full band captions and NoData must not overlap.");
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "matrix.scale-overflow" or "text.overflow");
        var colors = chart.Options.HeatmapColorScale!.Bands.Select(band => band.Color).ToArray();
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "heatmap-scale-swatch").ToArray();
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(colors, swatches.Select(swatch => swatch.Fill!.Value));
        for (var index = 0; index < swatches.Length; index++) {
            Assert.Equal(12, swatches[index].Bounds.Height, 6);
            Assert.Equal(colors[index], Pixel(image, swatches[index].Bounds));
            Assert.InRange(swatches[index].Bounds.Top, key.Top, key.Bottom);
            Assert.InRange(swatches[index].Bounds.Bottom, key.Top, key.Bottom + .01);
        }
        var missingSwatch = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "heatmap-scale-missing-swatch");
        Assert.True(missingSwatch.Bounds.Height > 0);
        Assert.InRange(missingSwatch.Bounds.Left, key.Left, key.Right); Assert.InRange(missingSwatch.Bounds.Right, key.Left, key.Right + .01);
        Assert.InRange(missingSwatch.Bounds.Top, key.Top, key.Bottom); Assert.InRange(missingSwatch.Bounds.Bottom, key.Top, key.Bottom + .01);
        // The no-data key wears the shared missing-data hatch over its fill (design rule 9).
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "heatmap-scale-missing-swatch-hatch");
        ScatterBubbleColorRenderingTests.Capture(prepared, "legacy-key-compact-hexbin-" + (dark ? "dark" : "light"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Heatmap, 360, false, 1, 0, -10, 30, 1, false)]
    [InlineData(ChartSeriesKind.HexbinHeatmap, 800, true, 3, 20, 0, 30, 3, false)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 360, false, 1, 10, 0, 40, 1, false)]
    [InlineData(ChartSeriesKind.Heatmap, 360, false, 1, -20, 0, 40, 0, false)]
    [InlineData(ChartSeriesKind.HexbinHeatmap, 800, true, 3, 60, 0, 40, 4, false)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 360, true, 1, -20, 0, 40, 0, false)]
    [InlineData(ChartSeriesKind.Heatmap, 360, false, 1, 0, -10, 30, 1, true)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 360, false, 1, -20, 0, 40, 0, true)]
    public void DivergingMidpointCaptionsFollowTheirNeutralSwatchAndKeepCompleteReadableText(ChartSeriesKind kind,
        int width, bool inferred, int lowCount, double midpoint, double min, double max, int midpointStep, bool threeColors) {
        const string family = "CFX Heatmap Midpoint Carlito";
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        var scale = threeColors ? ChartColorScale.Diverging(Low, Middle, High, midpoint)
            : lowCount == 1 ? ChartColorScale.Diverging(new[] { Low }, Middle, new[] { ChartColor.FromHex("#4680BC"), ChartColor.FromHex("#286DB4"), High }, midpoint)
            : ChartColorScale.Diverging(new[] { Low, ChartColor.FromHex("#B2CDE8"), ChartColor.FromHex("#8DB3DA") }, Middle, new[] { High }, midpoint);
        if (!inferred) scale = scale.WithValueRange(min, max);
        scale = scale.WithLabels(null, "Target", null);
        var effectiveMidpoint = scale.EffectiveMidpoint(min, max);
        var chart = HeatmapColorScaleTests.Create(kind, min, effectiveMidpoint, max).WithSize(width, 400).WithHeatmapColorScale(scale);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 400)),
            VisualTheme.Graphite(), VisualThemeMode.Light, new VisualFrame(), FontSpec.FromFamily(family)));
        var prefix = kind == ChartSeriesKind.CalendarHeatmap ? "calendar-scale" : "heatmap-scale";
        var document = XDocument.Parse(prepared.ToSvg()); var steps = Role(document, prefix + "-step");
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == prefix + "-swatch").ToArray();
        Assert.Equal(5, swatches.Length);
        Assert.Equal(effectiveMidpoint.ToString("R", CultureInfo.InvariantCulture), (string?)steps[midpointStep].Attribute("data-cfx-value"));
        Assert.Equal(Middle, swatches[midpointStep].Fill!.Value);
        var center = swatches[midpointStep].Bounds.Left + swatches[midpointStep].Bounds.Width / 2;
        var caption = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == prefix + "-label-midpoint");
        var captionRegion = prepared.Regions.Single(region => region.Id == caption.Id);
        var svgCaption = Role(document, prefix + "-label").Single(group => (string?)group.Attribute("data-cfx-source-id") == caption.Id)
            .Descendants().Single(element => element.Name.LocalName == "text");
        Assert.Equal("Target · " + effectiveMidpoint.ToString("R", CultureInfo.InvariantCulture), svgCaption.Value);
        Assert.Equal(center, caption.X, 6);
        var svgCenter = double.Parse(svgCaption.Attribute("x")!.Value, CultureInfo.InvariantCulture) + caption.Text.Metrics.Width / 2;
        Assert.InRange(Math.Abs(svgCenter - center), 0, .001);
        Assert.True(caption.Text.Size >= 8);
        Assert.True(caption.Text.Metrics.Width <= captionRegion.Bounds.Width);
        var key = prepared.Regions.Single(region => region.Role == prefix);
        Assert.True(captionRegion.Bounds.Left >= key.Bounds.Left && captionRegion.Bounds.Right <= key.Bounds.Right + .000001);
        Assert.True(captionRegion.Bounds.Bottom <= key.Bounds.Bottom + .000001);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "matrix.scale-overflow" or "text.overflow");
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        for (var index = 0; index < swatches.Length; index++) {
            var value = double.Parse(steps[index].Attribute("data-cfx-value")!.Value, CultureInfo.InvariantCulture);
            Assert.Equal(scale.ColorFor(value, min, max), Pixel(image, swatches[index].Bounds));
            Assert.Equal(12, swatches[index].Bounds.Height, 6);
        }
    }

    [Fact]
    public void SampledMidpointCaptionFollowsItsFractionalValuePosition() {
        var chart = HeatmapColorScaleTests.Create(ChartSeriesKind.Heatmap, 0, 3.2, 10).WithSize(360, 400)
            .WithHeatmapColorScale(ChartColorScale.Diverging(Low, Middle, High, 3.2).WithLabels(null, "Target", null));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "heatmap-scale-swatch").ToArray();
        var text = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == "heatmap-scale-label-midpoint");
        Assert.Equal(swatches[0].Bounds.Left + 1.78 * swatches[0].Bounds.Width, text.X, 6);
        Assert.Equal("Target · 3.2", Assert.Single(text.Text.Lines).Text);
        Assert.Equal("3.2", (string?)Role(XDocument.Parse(prepared.ToSvg()), "heatmap-scale").Single().Attribute("data-cfx-midpoint-value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SequentialAndConstantMidpointCaptionsKeepTheWholeKeyCenter(bool constant) {
        var scale = constant ? ChartColorScale.Diverging(new[] { Low }, Middle, new[] { Low, Middle, High })
            : ChartColorScale.Sequential(new[] { Low, Middle, High, ChartColor.Black });
        var chart = HeatmapColorScaleTests.Create(ChartSeriesKind.Heatmap, constant ? 7 : 0, constant ? 7 : 10)
            .WithHeatmapColorScale(scale.WithLabels(null, "Target", null));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var key = prepared.Regions.Single(region => region.Role == "heatmap-scale");
        var caption = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == "heatmap-scale-label-midpoint");
        Assert.Equal(key.Bounds.Left + key.Bounds.Width / 2, caption.X, 6);
    }

    [Fact]
    public void CalendarMissingPaintRemainsSeparateFromZeroAndHonorsOnlyAnAuthoredNoDataColor() {
        var chart = HeatmapColorScaleTests.Create(ChartSeriesKind.CalendarHeatmap, 0, 10).WithSize(500, 320);
        var original = XDocument.Parse(chart.ToSvg());
        var defaultMissing = HeatmapColorScaleTests.Fill(Role(original, "calendar-cell").First(cell => (string?)cell.Attribute("data-cfx-empty") == "true"));
        var scale = ChartColorScale.Sequential(Low, High).WithValueRange(0, 10);
        chart.WithHeatmapColorScale(scale);
        var document = XDocument.Parse(chart.ToSvg());
        var missing = Role(document, "calendar-cell").Where(cell => (string?)cell.Attribute("data-cfx-empty") == "true").ToArray();
        Assert.NotEmpty(missing); Assert.All(missing, cell => Assert.Equal(defaultMissing, HeatmapColorScaleTests.Fill(cell)));
        Assert.Equal(Low.ToHex(), HeatmapColorScaleTests.Fill(HeatmapColorScaleTests.Cells(document)[0]));
        Assert.Empty(Role(document, "calendar-scale-zero-label"));
        Assert.Single(Role(document, "calendar-scale-empty"));
        var customMissing = ChartColor.FromHex("#B3B5B7");
        chart.WithHeatmapColorScale(scale.WithNoDataColor(customMissing));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); document = XDocument.Parse(prepared.ToSvg());
        Assert.All(Role(document, "calendar-cell").Where(cell => (string?)cell.Attribute("data-cfx-empty") == "true"), cell => {
            Assert.Equal(customMissing.ToHex(), HeatmapColorScaleTests.Fill(cell)); Assert.Null(cell.Attribute("data-cfx-value"));
        });
        var swatch = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "calendar-scale-missing-swatch");
        Assert.Equal(customMissing, Pixel(RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1))), swatch.Bounds));
        Assert.Equal("1", (string?)Role(document, "calendar-heatmap").Single().Attribute("data-cfx-zero-count"));
    }

    [Fact]
    public void ExplicitScalesKeepRampAndPointPaintRolesForSvgVariablesWithoutChangingPng() {
        var chart = HeatmapColorScaleTests.Create(ChartSeriesKind.Heatmap, 0, 10).WithHeatmapColorScale(ChartColorScale.Sequential(Low, High));
        chart.Series[0].WithPointColor(1, High);
        var png = chart.ToPng();
        chart.WithSvgColorVariables(new SvgColorVariables().Add("--ramp-low", Low, SvgColorRole.Ramp)
            .Add("--ramp-high", High, SvgColorRole.Ramp).Add("--explicit-high", High, SvgColorRole.Series));
        var cells = HeatmapColorScaleTests.Cells(chart);
        Assert.Contains("var(--ramp-low,", HeatmapColorScaleTests.Fill(cells[0]));
        Assert.Contains("var(--explicit-high,", HeatmapColorScaleTests.Fill(cells[1]));
        Assert.Equal(png, chart.ToPng());
    }

    private static XElement[] Role(XDocument document, string role) => document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    private static ChartColor Pixel(RgbaImage image, ChartRect bounds) {
        var offset = ((int)(bounds.Top + bounds.Height / 2) * image.Width + (int)(bounds.Left + bounds.Width / 2)) * 4;
        return ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
    }
}
