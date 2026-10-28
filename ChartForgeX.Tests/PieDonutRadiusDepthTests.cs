using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PieDonutRadiusDepthTests {
    [Theory]
    [InlineData(ChartSeriesKind.Pie, false, 390)]
    [InlineData(ChartSeriesKind.Pie, true, 390)]
    [InlineData(ChartSeriesKind.Pie, false, 900)]
    [InlineData(ChartSeriesKind.Pie, true, 900)]
    [InlineData(ChartSeriesKind.Donut, false, 390)]
    [InlineData(ChartSeriesKind.Donut, true, 390)]
    [InlineData(ChartSeriesKind.Donut, false, 900)]
    [InlineData(ChartSeriesKind.Donut, true, 900)]
    public void BandRelativeObservationRadiiPreserveAnglesSourceIdentitiesAndNativePaint(ChartSeriesKind kind, bool dark, int width) {
        var chart = Fixture(kind, dark, width, false); var prepared = Prepare(chart);
        var slices = prepared.Scene.Nodes.OfType<VisualSceneSlice>().OrderBy(mark => mark.Id).ToArray(); Assert.Equal(3, slices.Length);
        var full = slices[2].Outer; var inner = slices[2].Inner;
        Assert.Equal(inner + (full - inner) * .35, slices[0].Outer, 9);
        Assert.Equal(inner + (full - inner) * .7, slices[1].Outer, 9);
        Assert.Equal(Math.PI, slices[0].Sweep, 10); Assert.Equal(Math.PI * .75, slices[1].Sweep, 10); Assert.Equal(Math.PI * .25, slices[2].Sweep, 10);
        Assert.All(slices, mark => Assert.Equal(inner, mark.Inner));
        var xml = XDocument.Parse(prepared.ToSvg()); var points = xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radial-point").ToArray();
        Assert.Equal(3, points.Length);
        Assert.Equal(new[] { "category-A", "category-B", "category-C" }, points.Select(node => (string?)node.Attribute("data-cfx-observation-id")).OrderBy(value => value));
        Assert.Equal(new[] { "0.35", "0.7", "1" }, points.OrderBy(node => (string?)node.Attribute("data-cfx-point")).Select(node => (string?)node.Attribute("data-cfx-slice-radius-ratio")));
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
        var png = PngReader.Decode(prepared.ToPng());
        ChartColor Pixel(double radius) {
            var x = (int)Math.Round(slices[0].Cx + radius); var y = (int)Math.Round(slices[0].Cy);
            var offset = (y * png.Width + x) * 4;
            return ChartColor.FromRgba(png.Pixels[offset], png.Pixels[offset + 1], png.Pixels[offset + 2], png.Pixels[offset + 3]);
        }
        Assert.Equal(slices[0].Fill, Pixel((inner + slices[0].Outer) / 2));
        Assert.NotEqual(slices[0].Fill, Pixel((slices[0].Outer + full) / 2));
        Assert.Equal(8, chart.Series[0].Points.Sum(point => point.Y));
        Assert.Equal(new double?[] { .35, .7, 1 }, chart.Series[0].PointSliceRadiusRatios);
        NumericRadialEncodedGeometryTests.Capture(chart, "slice-radius-" + kind + "-" + width + (dark ? "-dark" : "-light"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Pie, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Donut, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Donut, VisualThemeMode.Dark)]
    public void ClearingAllOptInsRestoresExactDefaultSvgPngWhilePreparedExportsStayDetached(ChartSeriesKind kind, VisualThemeMode mode) {
        var series = Source("observed", kind); var chart = Chart.Create().WithSize(600, 420).WithTheme(VisualTheme.Graphite(), mode)
            .WithLegend(false).WithHeader(false).AddSeries(series);
        var original = Prepare(chart); var svg = original.ToSvg(); var png = original.ToPng();
        series.WithPointSliceRadiusRatio(0, 1); Assert.Equal(png, Prepare(chart).ToPng());
        series.WithPointSliceRadiusRatio(0, .5); Assert.NotEqual(svg, Prepare(chart).ToSvg()); Assert.False(png.SequenceEqual(Prepare(chart).ToPng()));
        series.UseDefaultSliceRadius(0); chart.WithAutomaticRadialBands(new ChartRadialBandAllocation(.2, .04));
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
        chart.WithAutomaticRadialBands(null); Assert.Equal(svg, Prepare(chart).ToSvg()); Assert.Equal(png, Prepare(chart).ToPng());
        Assert.Equal(svg, original.ToSvg()); Assert.Equal(png, original.ToPng());
    }

    [Fact]
    public void StructuralEditsMoveRadiiWithTheirObservationAndCompleteReplacementClearsThem() {
        var series = Source("observed", ChartSeriesKind.Pie).WithPointSliceRadiusRatio(0, .25).WithPointSliceRadiusRatio(2, .75);
        var radii = series.PointSliceRadiusRatios;
        Assert.Throws<NotSupportedException>(() => ((IList<double?>)radii)[0] = .5);
        series.Points.Insert(0, new ChartPoint(-1, 2));
        Assert.Equal(new double?[] { null, .25, null, .75 }, radii); Assert.Equal("category-A", series.ObservationIds[1].Value);
        series.Points.RemoveAt(2); Assert.Equal(new double?[] { null, .25, .75 }, radii); Assert.Equal("category-C", series.ObservationIds[2].Value);
        series.Points[1] = new ChartPoint(1, 20); Assert.Equal(.25, radii[1]); Assert.Equal("category-A", series.ObservationIds[1].Value);
        series.Points.RemoveAt(0); Assert.Equal(new double?[] { .25, .75 }, radii);
        var chart = Chart.Create().AddSeries(series); Assert.Equal(2, Prepare(chart).Scene.Nodes.OfType<VisualSceneSlice>().Count());
        series.Points.Clear(); series.Points.Add(new ChartPoint(1, 7));
        Assert.Empty(radii); Assert.Empty(series.ObservationIds); Assert.Single(Prepare(chart).Scene.Nodes.OfType<VisualSceneSlice>());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    public void ZeroAndSubpixelRadiiKeepPositiveFactsWithoutInventingPaintOrLabels(ChartSeriesKind kind) {
        var chart = Fixture(kind, false, 600, false).WithDataLabels(); var series = chart.Series[0];
        var fullRadius = Prepare(chart).Scene.Nodes.OfType<VisualSceneSlice>().Single(mark => mark.Id == "series-0-point-2").Outer;
        series.WithPointSliceRadiusRatio(0, 0).WithPointSliceRadiusRatio(1, 1e-10).WithPointSliceOffset(0, .35);
        var prepared = Prepare(chart); Assert.Equal(fullRadius, Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>()).Outer);
        Assert.Contains(prepared.Scene.Diagnostics, diagnostic => diagnostic.Code == "radial.slice-insufficient-space");
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.DoesNotContain(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "radial-point"
            && ((string?)element.Attribute("data-cfx-point") is "0" or "1"));
        var saved = chart.ToVisualArtifact().ToInterchangeEnvelope();
        using var facts = JsonDocument.Parse(saved.Extensions["chart.series.0.radial.sourceFacts"]);
        Assert.Equal(4, facts.RootElement[0].GetProperty("value").GetDouble()); Assert.Equal(0, facts.RootElement[0].GetProperty("radiusRatio").GetDouble());
        Assert.Equal("category-A", facts.RootElement[0].GetProperty("observationId").GetString());
        Assert.True(prepared.ToPng().Length > 100);
        series.Points.Clear(); series.Points.Add(new ChartPoint(1, 0)); series.WithPointSliceRadiusRatio(0, .5);
        Assert.Contains(Prepare(chart).Scene.Diagnostics, diagnostic => diagnostic.Code == "radial.no-data");
    }

    [Fact]
    public void OtherRejectsDifferentRadiiOnlyWhenAggregationActuallyCombinesThemAndAcceptsIdenticalPolicies() {
        var series = new ChartSeries(new ChartSeriesId("aggregate"), "Aggregate", ChartSeriesKind.Pie,
            new[] { new ChartPoint(1, 10), new ChartPoint(2, 3), new ChartPoint(3, 2), new ChartPoint(4, 1) });
        series.WithPointSliceRadiusRatio(1, .5).WithPointSliceRadiusRatio(2, .5).WithPointSliceRadiusRatio(3, .5);
        var chart = Chart.Create().WithLegend(false).AddSeries(series); chart.Options.MaximumPieSlices = 2;
        var same = Prepare(chart); var svg = same.ToSvg();
        var marks = same.Scene.Nodes.OfType<VisualSceneSlice>().ToArray(); Assert.Equal(2, marks.Length);
        Assert.Equal(marks[0].Outer * .5, marks[1].Outer, 9);
        series.WithPointSliceRadiusRatio(2, .25);
        var rejection = Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        Assert.Contains("radial.aggregate-radius", rejection.Message); Assert.Contains("aggregate", rejection.Message); Assert.Contains("1,2,3", rejection.Message);
        Assert.Equal(svg, same.ToSvg()); Assert.Equal(.25, series.PointSliceRadiusRatios[2]);
        chart.Options.MaximumPieSlices = 4; Assert.Equal(4, Prepare(chart).Scene.Nodes.OfType<VisualSceneSlice>().Count());
        series.UseDefaultSliceRadius(1).WithPointSliceRadiusRatio(2, 1).UseDefaultSliceRadius(3); chart.Options.MaximumPieSlices = 2;
        var explicitFull = Prepare(chart).ToPng(); series.UseDefaultSliceRadius(2);
        Assert.Equal(explicitFull, Prepare(chart).ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    public void OneObservationUsesTheSharedClosedSliceForAnAuthoredRadiusAndRestoresItsOriginalPixels(ChartSeriesKind kind) {
        var series = new ChartSeries(new ChartSeriesId("only"), "Only", kind, new[] { new ChartPoint(1, 9) });
        var chart = Chart.Create().WithSize(500, 380).WithHeader(false).WithLegend(false).WithDataLabels(false).WithDonutCenterLabel(false).AddSeries(series);
        var original = Prepare(chart); var full = Assert.Single(original.Scene.Nodes.OfType<VisualSceneSlice>());
        series.WithPointSliceRadiusRatio(0, .5); var changed = Prepare(chart); var slice = Assert.Single(changed.Scene.Nodes.OfType<VisualSceneSlice>());
        Assert.Equal(Math.PI * 2, slice.Sweep, 10); Assert.Equal(full.Inner + (full.Outer - full.Inner) * .5, slice.Outer, 9);
        Assert.False(original.ToPng().SequenceEqual(changed.ToPng()));
        Assert.Single(XDocument.Parse(changed.ToSvg()).Descendants(), element => element.Name.LocalName == "path" && (string?)element.Attribute("data-cfx-source-id") == "series-0-point-0");
        series.UseDefaultSliceRadius(0); Assert.Equal(original.ToSvg(), Prepare(chart).ToSvg()); Assert.Equal(original.ToPng(), Prepare(chart).ToPng());
    }

    [Fact]
    public void InvalidRadiusInputsAndNegativeValuesRejectWithoutChangingStoredOverrides() {
        var series = Source("observed", ChartSeriesKind.Donut).WithPointSliceRadiusRatio(0, .5);
        foreach (var value in new[] { -.01, 1.01, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => series.WithPointSliceRadiusRatio(0, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.WithPointSliceRadiusRatio(3, .5)); Assert.Equal(.5, series.PointSliceRadiusRatios[0]);
        var wrong = ChartSeries.CreateLine(new ChartSeriesId("line"), "Line", new[] { new ChartPoint(1, 2) });
        Assert.Throws<InvalidOperationException>(() => wrong.WithPointSliceRadiusRatio(0, .5)); Assert.Empty(wrong.PointSliceRadiusRatios);
        var chart = Chart.Create().AddSeries(series); series.Points[0] = new ChartPoint(1, -1);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart)); Assert.Equal(.5, series.PointSliceRadiusRatios[0]);
    }

    internal static ChartSeries Source(string id, ChartSeriesKind kind) => new ChartSeries(new ChartSeriesId(id), id, kind,
        new[] { new ChartPoint(1, 4), new ChartPoint(2, 3), new ChartPoint(3, 1) })
        .WithObservationIds(new[] { new ChartObservationId("category-A"), new ChartObservationId("category-B"), new ChartObservationId("category-C") });
    internal static Chart Fixture(ChartSeriesKind kind, bool dark, int width, bool labels) {
        var source = Source("observed", kind).WithPointSliceRadiusRatio(0, .35).WithPointSliceRadiusRatio(1, .7).WithPointSliceRadiusRatio(2, 1);
        return Chart.Create().WithSize(width, width < 500 ? 440 : 500).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithHeader(false).WithLegend(false).WithDataLabels(labels).WithXLabels("Alpha", "Beta", "Gamma").AddSeries(source);
    }
    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
