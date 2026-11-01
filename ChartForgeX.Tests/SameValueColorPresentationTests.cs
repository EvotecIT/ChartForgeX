using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Same-measure color presentation preserves native paint and raw facts while capturing display text once.</summary>
public sealed class SameValueColorPresentationTests {
    [Theory]
    [InlineData(ChartSeriesKind.Heatmap)]
    [InlineData(ChartSeriesKind.HexbinHeatmap)]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    public void PositiveLegacyKeysFormatOnlyObservedValuesAndEmittedSamples(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(800, 440).WithDataLabels(false);
        if (kind == ChartSeriesKind.CalendarHeatmap) chart.AddCalendarHeatmap("Positive", new[] {
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 10),
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 6), 20)
        });
        else if (kind == ChartSeriesKind.HexbinHeatmap) chart.AddHexbinHeatmapRow("Positive", new double?[] { 10, 20 });
        else chart.AddHeatmapRow("Positive", new double?[] { 10, 20 });
        var calls = new Dictionary<double, int>();
        chart.ConfigureHeatmapColorDimension(options => options.ValueFormat = ChartValueFormat.Custom(value => {
            if (value < 10 || value > 20) throw new InvalidOperationException("Only the positive measurement domain can be formatted.");
            calls[value] = calls.GetValueOrDefault(value) + 1;
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }));
        var prepared = Prepare(chart);
        Assert.Contains(10d, calls.Keys); Assert.Contains(20d, calls.Keys); Assert.DoesNotContain(0d, calls.Keys);
        var capturedCalls = calls.ToArray();
        _ = prepared.ToSvg(); _ = prepared.ToPng();
        Assert.Equal(capturedCalls, calls.ToArray());
        Assert.All(calls.Values, count => Assert.Equal(1, count));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Heatmap, "legacy", false)]
    [InlineData(ChartSeriesKind.Heatmap, "positioned", false)]
    [InlineData(ChartSeriesKind.Heatmap, "discrete", false)]
    [InlineData(ChartSeriesKind.HexbinHeatmap, "positioned", false)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, "legacy", false)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, "positioned", false)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, "discrete", false)]
    [InlineData(ChartSeriesKind.TileMap, "legacy", false)]
    [InlineData(ChartSeriesKind.TileMap, "positioned", false)]
    [InlineData(ChartSeriesKind.TileMap, "discrete", true)]
    [InlineData(ChartSeriesKind.RegionMap, "positioned", true)]
    public void CultureAndMeasuredHeadingChangeDisplayWithoutChangingPaintOrRawValues(ChartSeriesKind kind, string scaleKind, bool right) {
        var chart = ColorDimensionPresentationFixtures.Create(kind, scaleKind, 360);
        if (right) chart.WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        var baseline = Prepare(chart);
        var options = ColorDimensionPresentationFixtures.Options(chart, kind);
        options.Name = "Temperature (°C)";
        options.ValueFormat = ChartValueFormat.Number("0.00 '°C'", CultureInfo.GetCultureInfo("pl-PL"));
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        var observed = Marks(document, kind).Where(mark => (string?)mark.Attribute("data-cfx-empty") != "true").ToArray();
        Assert.Equal(3, observed.Length);
        Assert.Equal(new[] { "0", "7.125", "12.5" }.OrderBy(value => value), observed.Select(mark => (string)mark.Attribute("data-cfx-value")!).OrderBy(value => value));
        Assert.All(observed, mark => {
            var value = double.Parse((string)mark.Attribute("data-cfx-value")!, CultureInfo.InvariantCulture);
            Assert.Equal(options.ValueFormat.Format(value), (string?)mark.Attribute("data-cfx-formatted-value"));
            Assert.Equal(options.Name, (string?)mark.Attribute("data-cfx-label-value"));
            Assert.Contains(options.ValueFormat.Format(value), (string?)mark.Attribute("aria-label"));
            Assert.Null(mark.Attribute("data-cfx-color-value"));
        });
        var prefix = Prefix(kind);
        var title = Role(document, prefix + "-title").Single();
        Assert.Equal(options.Name, string.Concat(title.DescendantsAndSelf().Where(element => element.Name.LocalName == "text").Select(element => element.Value)));
        var heading = prepared.Regions.Single(region => region.Role == prefix + "-title");
        var key = prepared.Regions.SingleOrDefault(region => region.Role == prefix);
        if (key != null) {
            Assert.True(heading.Bounds.Left >= key.Bounds.Left && heading.Bounds.Right <= key.Bounds.Right + .000001);
            Assert.True(heading.Bounds.Top >= key.Bounds.Top && heading.Bounds.Bottom <= key.Bounds.Bottom + .000001);
        }
        var swatches = Swatches(prepared, kind);
        Assert.NotEmpty(swatches);
        Assert.All(swatches, swatch => Assert.True(swatch.Bounds.Top >= heading.Bounds.Bottom - .000001));
        Assert.Equal(Swatches(baseline, kind).Select(swatch => swatch.Fill), swatches.Select(swatch => swatch.Fill));
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.All(swatches, swatch => { Assert.True(swatch.Bounds.Height >= 4); Assert.Equal(swatch.Fill!.Value, Pixel(image, swatch.Bounds)); });
        if (kind == ChartSeriesKind.CalendarHeatmap && scaleKind == "legacy") {
            Assert.All(swatches, swatch => Assert.Equal(12, swatch.Bounds.Height, 6));
            Assert.Equal("0,00 °C", string.Concat(Role(document, "calendar-scale-zero-label").SelectMany(group => group.DescendantsAndSelf()).Where(element => element.Name.LocalName == "text").Select(element => element.Value)));
        }
        if (scaleKind == "positioned") {
            var sources = Role(document, prefix + "-stop-source");
            Assert.Equal(new[] { "0", "8", "20" }, sources.Select(source => (string?)source.Attribute("data-cfx-value")));
            Assert.Equal(new[] { "0,00 °C", "8,00 °C", "20,00 °C" }, sources.Select(source => (string?)source.Attribute("data-cfx-formatted-value")));
        } else if (scaleKind == "discrete") {
            var bands = Role(document, prefix == "map-scale" ? "map-scale-step-source" : prefix + "-step");
            Assert.Equal(new[] { "5", "" }, bands.Select(band => (string?)band.Attribute("data-cfx-upper-bound")));
            Assert.Equal(new[] { "Cool < 5,00 °C", "Warm ≥ 5,00 °C" }, bands.Select(band => (string?)band.Attribute("data-cfx-label")));
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "matrix.scale-overflow" or "calendar.scale-overflow");
    }

    [Theory]
    [InlineData(ChartSeriesKind.Heatmap)]
    [InlineData(ChartSeriesKind.HexbinHeatmap)]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    [InlineData(ChartSeriesKind.TileMap)]
    [InlineData(ChartSeriesKind.RegionMap)]
    public void CustomEmptyIsAuthoritativeAndPreparedExportsNeverRerunOrReadSourcePolicy(ChartSeriesKind kind) {
        var chart = ColorDimensionPresentationFixtures.Create(kind);
        var calls = new Dictionary<double, int>(); var frozen = false;
        var options = ColorDimensionPresentationFixtures.Options(chart, kind);
        options.Name = "Temperature";
        options.ValueFormat = ChartValueFormat.Custom(value => {
            if (frozen) throw new InvalidOperationException("Prepared exports must consume captured text.");
            Assert.True(double.IsFinite(value));
            if (calls.Count == 0) {
                options.Name = "Changed during Prepare"; chart.Options.Labels.NoData = "Changed during Prepare";
                options.ValueFormat = ChartValueFormat.Custom(_ => throw new InvalidOperationException("The captured formatter must remain authoritative."));
            }
            calls[value] = calls.GetValueOrDefault(value) + 1;
            return value == 0 ? "" : "t<" + value.ToString(CultureInfo.InvariantCulture) + ">";
        });
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.NotEmpty(calls); Assert.All(calls.Values, count => Assert.Equal(1, count));
        var marks = Marks(XDocument.Parse(svg), kind);
        var zero = marks.Single(mark => (string?)mark.Attribute("data-cfx-value") == "0" && (string?)mark.Attribute("data-cfx-empty") != "true");
        Assert.Equal("", (string?)zero.Attribute("data-cfx-formatted-value"));
        Assert.DoesNotContain("t<0>", (string?)zero.Attribute("aria-label"));
        foreach (var missing in marks.Where(mark => (string?)mark.Attribute("data-cfx-empty") == "true")) {
            Assert.Equal(kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap ? "Not sampled" : null,
                (string?)missing.Attribute("data-cfx-formatted-value"));
            Assert.Contains("Not sampled", (string?)missing.Attribute("aria-label"));
        }
        frozen = true;
        options.Name = "Changed"; options.ValueFormat = ChartValueFormat.Custom(_ => throw new InvalidOperationException());
        chart.Options.Labels.NoData = "Changed";
        if (kind == ChartSeriesKind.CalendarHeatmap) chart.Series[0].UpdateCalendarItem(0, new ChartCalendarHeatmapItem(chart.Series[0].CalendarItems[0].Date, 999));
        else if (kind is ChartSeriesKind.TileMap or ChartSeriesKind.RegionMap) chart.Series[0].UpdateRegionValue(0, new ChartRegionMapItem(chart.Series[0].RegionValues[0].Region, 999));
        else { var first = chart.Series[0].Points[0]; chart.Series[0].Points[0] = new ChartPoint(first.X, 999); }
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.All(calls.Values, count => Assert.Equal(1, count));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Heatmap, "legacy")]
    [InlineData(ChartSeriesKind.HexbinHeatmap, "positioned")]
    [InlineData(ChartSeriesKind.CalendarHeatmap, "legacy")]
    [InlineData(ChartSeriesKind.CalendarHeatmap, "discrete")]
    [InlineData(ChartSeriesKind.TileMap, "positioned")]
    [InlineData(ChartSeriesKind.RegionMap, "discrete")]
    public void NullResetRestoresDefaultHeadingsMarkupAndNativePixels(ChartSeriesKind kind, string scaleKind) {
        var chart = ColorDimensionPresentationFixtures.Create(kind, scaleKind);
        var baseline = Prepare(chart); var svg = baseline.ToSvg(); var png = baseline.ToPng();
        var options = ColorDimensionPresentationFixtures.Options(chart, kind);
        options.Name = "Temperature";
        var named = Marks(XDocument.Parse(Prepare(chart).ToSvg()), kind).First(mark => (string?)mark.Attribute("data-cfx-value") == "7.125");
        Assert.Equal(chart.Options.ValueFormat.Format(7.125), (string?)named.Attribute("data-cfx-formatted-value"));
        options.ValueFormat = ChartValueFormat.Number("0.00 '°C'");
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
        options.Name = null;
        var formatted = Marks(XDocument.Parse(Prepare(chart).ToSvg()), kind).First(mark => (string?)mark.Attribute("data-cfx-value") == "7.125");
        Assert.Equal("7.13 °C", (string?)formatted.Attribute("data-cfx-formatted-value"));
        Assert.Equal(chart.Options.Labels.Value, (string?)formatted.Attribute("data-cfx-label-value"));
        options.ValueFormat = null;
        var reset = Prepare(chart);
        Assert.Equal(svg, reset.ToSvg()); Assert.Equal(png, reset.ToPng());
        var title = Role(XDocument.Parse(svg), Prefix(kind) + "-title");
        // Design rule 9: a numeric heatmap key is always titled, by default with the localized value label.
        if ((kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap) && scaleKind == "discrete" || kind == ChartSeriesKind.Heatmap) Assert.Single(title);
        else Assert.Empty(title);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Heatmap)]
    [InlineData(ChartSeriesKind.HexbinHeatmap)]
    public void ExplicitPointLabelsRetainVisiblePrecedenceOverNumericFormat(ChartSeriesKind kind) {
        var chart = ColorDimensionPresentationFixtures.Create(kind).WithDataLabels().WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always);
        chart.Series[0].WithPointLabel(0, "Caller caption");
        chart.ConfigureHeatmapColorDimension(options => { options.Name = "Temperature"; options.ValueFormat = ChartValueFormat.Custom(value => "T=" + value.ToString(CultureInfo.InvariantCulture)); });
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(prepared.Regions, region => region.Role == "data-label" && region.Label == "Caller caption");
        var mark = Marks(document, kind).Single(element => (string?)element.Attribute("data-cfx-point") == "0");
        Assert.Equal("T=7.125", (string?)mark.Attribute("data-cfx-formatted-value"));
        Assert.Equal("7.125", (string?)mark.Attribute("data-cfx-value"));
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static string Prefix(ChartSeriesKind kind) => kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap ? "map-scale"
        : kind == ChartSeriesKind.CalendarHeatmap ? "calendar-scale" : "heatmap-scale";
    private static XElement[] Role(XDocument document, string role) => document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    private static XElement[] Marks(XDocument document, ChartSeriesKind kind) => Role(document, kind switch {
        ChartSeriesKind.TileMap => "tile-map-region-source", ChartSeriesKind.RegionMap => "region-map-region-source",
        ChartSeriesKind.CalendarHeatmap => "calendar-cell", ChartSeriesKind.HexbinHeatmap => "hexbin-cell", _ => "heatmap-cell"
    });
    private static VisualSceneRectangle[] Swatches(PreparedVisual prepared, ChartSeriesKind kind) => prepared.Scene.Nodes.OfType<VisualSceneRectangle>()
        .Where(node => node.Role == (Prefix(kind) == "map-scale" ? "map-scale-step" : Prefix(kind) + "-swatch")
            || ((kind is ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap or ChartSeriesKind.CalendarHeatmap) && node.Role == null && node.Fill.HasValue && node.Bounds.Height <= 12 && node.Bounds.Width > 0))
        .ToArray();
    private static ChartColor Pixel(RgbaImage image, ChartRect bounds) {
        var offset = ((int)(bounds.Top + bounds.Height / 2) * image.Width + (int)(bounds.Left + bounds.Width / 2)) * 4;
        return ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
    }
}
