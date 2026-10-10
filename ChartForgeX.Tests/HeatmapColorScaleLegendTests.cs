using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
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
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
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
        chart.WithHeatmapScaleLegend(false);
        Assert.DoesNotContain(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Regions, region => region.Role == prefix);
        var hiddenContext = VisualExportRequest.ForChart(chart.WithHeatmapScaleLegend()).Context;
        var frameHidden = new VisualRenderContext(hiddenContext.Layout, hiddenContext.Theme, hiddenContext.ThemeMode, new VisualFrame(showLegend: false), hiddenContext.Font);
        Assert.DoesNotContain(chart.Prepare(frameHidden).Regions, region => region.Role == prefix);
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
