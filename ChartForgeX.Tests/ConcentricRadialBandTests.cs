using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ConcentricRadialBandTests {
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(800, 500)));
    private static Chart Rings(bool outerFirst = false) {
        var inner = new ChartSeries(new ChartSeriesId("prior"), "Prior", ChartSeriesKind.Donut,
            new[] { new ChartPoint(1, 75), new ChartPoint(2, 25) }).WithRadialBand(new ChartRadialBand(.2, .5))
            .WithObservationIds(new[] { new ChartObservationId("A"), new ChartObservationId("B") });
        inner.DataLabelPlacement = ChartDataLabelPlacement.Inside;
        var outer = new ChartSeries(new ChartSeriesId("current"), "Current", ChartSeriesKind.Pie,
            new[] { new ChartPoint(1, 1), new ChartPoint(2, 1) }).WithRadialBand(new ChartRadialBand(.65, 1))
            .WithObservationIds(new[] { new ChartObservationId("A"), new ChartObservationId("B") });
        var chart = Chart.Create().WithSize(800, 500).WithLegend(false).WithXLabels("A", "B");
        return outerFirst ? chart.AddSeries(outer).AddSeries(inner) : chart.AddSeries(inner).AddSeries(outer);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndependentRingsKeepGeometryTotalsStableSourceIdsAndOneInnermostCenter(bool outerFirst) {
        var chart = Rings(outerFirst); var prepared = chart.Prepare(Context());
        var innerIndex = outerFirst ? 1 : 0; var outerIndex = 1 - innerIndex;
        var slices = prepared.Scene.Nodes.OfType<VisualSceneSlice>().ToArray(); Assert.Equal(4, slices.Length);
        var inner = slices.Where(slice => slice.Id!.StartsWith("series-" + innerIndex + "-", StringComparison.Ordinal)).ToArray();
        var outer = slices.Where(slice => slice.Id!.StartsWith("series-" + outerIndex + "-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(Math.PI * 1.5, inner[0].Sweep, 10); Assert.Equal(Math.PI, outer[0].Sweep, 10);
        Assert.All(inner, slice => { Assert.Equal(outer[0].Outer * .5, slice.Outer, 8); Assert.Equal(outer[0].Outer * .2, slice.Inner, 8); });
        Assert.All(outer, slice => Assert.Equal(slice.Outer * .65, slice.Inner, 8));
        var svg = XDocument.Parse(prepared.ToSvg());
        var points = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radial-point").ToArray();
        Assert.Equal(4, points.Length);
        Assert.Equal(new[] { "current", "prior" }, points.Select(node => node.Attribute("data-cfx-series-key")!.Value).Distinct().OrderBy(value => value));
        Assert.Equal(2, points.Count(node => (string?)node.Attribute("data-cfx-observation-id") == "A"));
        var center = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "donut-center");
        Assert.Equal("series-" + innerIndex + "-center", center.Id); Assert.Equal("Prior", center.Metadata["data-cfx-center-caption"]); Assert.Equal("100", center.Metadata["data-cfx-center-value"]);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Pie, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Donut, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Donut, VisualThemeMode.Dark)]
    public void ClearingAnExplicitSingleBandRestoresTheOrdinaryDefaultExports(ChartSeriesKind kind, VisualThemeMode mode) {
        var series = new ChartSeries(new ChartSeriesId("observed"), "Observed", kind,
            new[] { new ChartPoint(1, 3), new ChartPoint(2, 1) });
        var chart = Chart.Create().WithSize(520, 360).WithTheme(VisualTheme.Graphite(), mode)
            .WithLegend(false).WithHeader(false).AddSeries(series);
        var baseline = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = baseline.ToSvg(); var png = baseline.ToPng();
        series.WithRadialBand(new ChartRadialBand(.1, .7));
        var changed = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.NotEqual(svg, changed.ToSvg());
        series.WithRadialBand(null);
        var restored = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(svg, restored.ToSvg()); Assert.Equal(png, restored.ToPng());
        Assert.Equal(svg, baseline.ToSvg()); Assert.Equal(png, baseline.ToPng());
    }

    [Theory]
    [InlineData(390, 360, VisualThemeMode.Dark)]
    [InlineData(900, 500, VisualThemeMode.Light)]
    public void NativeSvgAndPngPaintIndependentBandsAndRetainAnEmptyAnnularGap(int width, int height, VisualThemeMode mode) {
        var chart = Rings().WithSize(width, height).WithTheme(VisualTheme.Graphite(), mode)
            .WithHeader(false).WithDataLabels(false).WithDonutCenterLabel(false);
        var innerColor = ChartColor.FromHex("#2468AC"); var outerColor = ChartColor.FromHex("#D45A32");
        chart.Series[0].Color = innerColor; chart.Series[1].Color = outerColor;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var slices = prepared.Scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        var outer = slices.Single(slice => slice.Id == "series-1-point-0");
        var inner = slices.Single(slice => slice.Id == "series-0-point-0");
        var image = PngReader.Decode(prepared.ToPng());
        ChartColor Pixel(double ratio) {
            var x = (int)Math.Round(outer.Cx + outer.Outer * ratio); var y = (int)Math.Round(outer.Cy);
            var offset = (y * image.Width + x) * 4;
            return ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
        }
        Assert.Equal(innerColor, inner.Fill); Assert.Equal(outerColor, outer.Fill);
        Assert.Equal(innerColor, Pixel(.35)); Assert.Equal(outerColor, Pixel(.825));
        Assert.Equal(Pixel(0), Pixel(.575)); Assert.NotEqual(innerColor, Pixel(.575)); Assert.NotEqual(outerColor, Pixel(.575));
        Assert.Throws<ArgumentOutOfRangeException>(() => prepared.ToPng(new VisualRenderOptions(pixelBudget: (long)width * height * 4 - 1)));
        var svg = XDocument.Parse(prepared.ToSvg());
        foreach (var (series, color) in new[] { ("prior", innerColor), ("current", outerColor) })
            Assert.All(svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radial-point"
                && (string?)node.Attribute("data-cfx-series-key") == series),
                point => Assert.Contains(point.Descendants(), node => (string?)node.Attribute("fill") == color.ToCss()));
        NumericRadialEncodedGeometryTests.Capture(chart, "concentric-" + width + "-" + mode.ToString().ToLowerInvariant());
    }

    [Fact]
    public void EmptyOrTooThinInnerBandsDoNotCoverOtherRingsOrBorrowTheirCenterTotals() {
        var chart = Rings(); chart.Series[0].Points.Clear();
        var empty = chart.Prepare(Context());
        Assert.Equal(2, empty.Scene.Nodes.OfType<VisualSceneSlice>().Count());
        Assert.Contains(empty.Scene.Diagnostics, item => item.Code == "radial.no-data");
        Assert.DoesNotContain(empty.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "donut-center");
        chart.Series[0].Points.Add(new ChartPoint(1, 9)); chart.Series[0].RadialBand = new ChartRadialBand(.2, .20001);
        var thin = chart.Prepare(Context());
        Assert.Equal(2, thin.Scene.Nodes.OfType<VisualSceneSlice>().Count());
        Assert.Contains(thin.Scene.Diagnostics, item => item.Code == "radial.band-insufficient-space");
        Assert.Contains(VisualRadialCompiler.LegendEntries(chart, Context().Theme.Resolve(Context().ThemeMode)), entry => entry.Label.StartsWith("Prior:", StringComparison.Ordinal));
        Assert.NotEmpty(thin.ToPng()); Assert.Single(chart.Series[0].Points);
    }

    [Fact]
    public void AggregationLegendAndOverflowStayIndependentPerBandAndPreparedExportsAreDetached() {
        var chart = Rings();
        chart.Series[0].Points.Add(new ChartPoint(3, 5)); chart.Series[1].Points.Add(new ChartPoint(3, 2));
        chart.Options.MaximumPieSlices = 2;
        var prepared = chart.Prepare(Context());
        var legend = VisualRadialCompiler.LegendEntries(chart, Context().Theme.Resolve(Context().ThemeMode));
        Assert.Equal(4, legend.Count); Assert.Equal(2, legend.Count(entry => entry.Label.StartsWith("Prior:", StringComparison.Ordinal)));
        Assert.Equal(2, legend.Count(entry => entry.Label.StartsWith("Current:", StringComparison.Ordinal)));
        var others = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "radial-point" && group.Metadata["data-cfx-point"] == "-1").ToArray();
        Assert.Equal(2, others.Length); Assert.Equal(2, others.Select(group => group.Metadata["data-cfx-series-key"]).Distinct().Count());
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[0].RadialBand = new ChartRadialBand(.15, .45); chart.Series[1].Points[0] = new ChartPoint(1, 100);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        var overflow = Rings(); overflow.Series[1].Points[0] = new ChartPoint(1, 1e308); overflow.Series[1].Points[1] = new ChartPoint(2, 1e308);
        var overflowing = overflow.Prepare(Context());
        Assert.Contains(overflowing.Scene.Diagnostics, item => item.Code == "radial.total-overflow");
        Assert.DoesNotContain("NaN", overflowing.ToSvg(), StringComparison.Ordinal); Assert.DoesNotContain("Infinity", overflowing.ToSvg(), StringComparison.Ordinal);
        Assert.All(overflowing.Scene.Nodes.OfType<VisualSceneSlice>(), slice => Assert.InRange(slice.Sweep, 0, Math.PI * 2));
    }

    [Theory]
    [InlineData(ChartDataLabelPlacement.Outside)]
    [InlineData(ChartDataLabelPlacement.Left)]
    [InlineData(ChartDataLabelPlacement.Right)]
    [InlineData(ChartDataLabelPlacement.Above)]
    [InlineData(ChartDataLabelPlacement.Below)]
    public void InnerActiveExternalLabelsRejectInsteadOfCrossingOuterBands(ChartDataLabelPlacement placement) {
        var chart = Rings().WithDataLabels(); chart.Series[0].DataLabelPlacement = placement;
        Assert.Contains("outermost", Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context())).Message);
        chart.Series[0].ShowDataLabels = false; chart.Series[1].DataLabelPlacement = placement;
        Assert.NotEmpty(chart.Prepare(Context()).ToPng());
    }

    [Fact]
    public void InvalidBandsOverlapExplosionAndWrongFamiliesRejectWithoutChangingSourceFacts() {
        foreach (var values in new[] { (double.NaN, 1d), (0d, double.PositiveInfinity), (-.1, 1d), (.5, .5), (.5, .4), (.5, 1.1) })
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRadialBand(values.Item1, values.Item2));
        var wrong = ChartSeries.CreateLine(new ChartSeriesId("line"), "Line", new[] { new ChartPoint(1, 2) });
        Assert.Throws<InvalidOperationException>(() => wrong.WithRadialBand(new ChartRadialBand(.2, .5))); Assert.Null(wrong.RadialBand);
        var chart = Rings(); var source = chart.Series[0].Points.ToArray();
        chart.Series[0].RadialBand = new ChartRadialBand(.5, .8);
        Assert.Contains("overlap", Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context())).Message); Assert.Equal(source, chart.Series[0].Points);
        chart.Series[0].RadialBand = new ChartRadialBand(.2, .5); chart.Series[1].RadialBand = null;
        Assert.Contains("every series", Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context())).Message);
        chart.Series[1].RadialBand = new ChartRadialBand(.65, 1); chart.Series[0].WithPointSliceOffset(0, .1);
        Assert.Contains("Exploded", Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context())).Message);
    }

    [Fact]
    public void CommonInterchangeCarriesExplicitBandFactsWithoutClaimingTypedChartSemantics() {
        var chart = Rings(); var artifact = chart.ToVisualArtifact("rings"); var envelope = artifact.ToInterchangeEnvelope();
        Assert.Equal("prior", envelope.Extensions["chart.series.0.id"]); Assert.Equal("current", envelope.Extensions["chart.series.1.id"]);
        Assert.Equal("0.2", envelope.Extensions["chart.series.0.radial.innerRatio"]); Assert.Equal("1", envelope.Extensions["chart.series.1.radial.outerRatio"]);
        var roundTrip = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson());
        Assert.Equal(envelope.Extensions, roundTrip.Extensions);
        var prepared = chart.Prepare(Context()); var captured = prepared.ToArtifact("rings", VisualArtifactKind.Chart, envelope);
        var original = captured.ToInterchangeJson(); envelope.Extensions["chart.series.0.radial.innerRatio"] = "changed";
        chart.Series[0].RadialBand = new ChartRadialBand(.1, .4);
        Assert.Equal(original, captured.ToInterchangeJson()); Assert.NotEmpty(captured.ToSvg()); Assert.NotEmpty(captured.ToPng());
    }
}
