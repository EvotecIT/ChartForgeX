using System.Globalization;
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

public sealed class MultiRingDepthTests {
    [Fact]
    public void PassiveCalloutIntentLowersOnlyOverlayGroupsToNonTargetableSvg() {
        var prepared = Prepare(Fixture(false, 900, true));
        var callouts = prepared.Scene.Nodes.OfType<VisualSceneGroup>()
            .Where(group => group.Role == "radial-data-label-source").ToArray();
        Assert.NotEmpty(callouts);
        Assert.All(callouts, group => {
            Assert.Equal("true", group.Metadata["data-cfx-passive-overlay"]);
            Assert.False(group.Metadata.ContainsKey("pointer-events"));
        });
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.All(svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-radial-callout") == "overlay"),
            node => Assert.Equal("none", (string?)node.Attribute("pointer-events")));
        var sources = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radial-point").ToArray();
        Assert.Equal(4, sources.Length);
        Assert.All(sources, node => Assert.Null(node.Attribute("pointer-events")));
    }

    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 900)]
    [InlineData(true, 900)]
    public void SharedExplosionFrameRetainsExactBandRatiosOffsetsSourceIdentityAndNativePaint(bool dark, int width) {
        var chart = Fixture(dark, width, false).WithDataLabels(false);
        chart.Series[0].WithPointSliceOffset(0, .3); chart.Series[1].WithPointSliceOffset(1, .15);
        var prepared = Prepare(chart); var slices = prepared.Scene.Nodes.OfType<VisualSceneSlice>().ToArray(); Assert.Equal(4, slices.Length);
        var center = slices.Single(mark => mark.Id == "series-1-point-0"); var full = center.Outer;
        foreach (var mark in slices) {
            var inner = mark.Id!.StartsWith("series-0-", StringComparison.Ordinal);
            Assert.Equal(full * (inner ? .48 : 1), mark.Outer, 9); Assert.Equal(full * (inner ? .18 : .62), mark.Inner, 9);
            var offset = mark.Id == "series-0-point-0" ? .3 : mark.Id == "series-1-point-1" ? .15 : 0;
            var mid = mark.Start + mark.Sweep / 2;
            Assert.Equal(center.Cx + Math.Cos(mid) * mark.Outer * offset, mark.Cx, 9);
            Assert.Equal(center.Cy + Math.Sin(mid) * mark.Outer * offset, mark.Cy, 9);
        }
        var xml = XDocument.Parse(prepared.ToSvg()); var points = xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radial-point").ToArray();
        Assert.Equal(4, points.Length); Assert.Equal(2, points.Select(node => (string?)node.Attribute("data-cfx-series-key")).Distinct().Count());
        Assert.All(points, point => Assert.NotNull(point.Attribute("data-cfx-observation-id")));
        Assert.Equal("0.3", points.Single(point => (string?)point.Attribute("data-cfx-series") == "0" && (string?)point.Attribute("data-cfx-point") == "0").Attribute("data-cfx-slice-offset-ratio")!.Value);
        var image = PngReader.Decode(prepared.ToPng()); Assert.Equal(width, image.Width);
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
        NumericRadialEncodedGeometryTests.Capture(chart, "multiring-explosion-" + width + (dark ? "-dark" : "-light"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredOverlapUsesActualSectorGeometryAndExistingSourcePaintOrder(bool dark) {
        var innerColor = ChartColor.FromHex("#C4342B"); var outerColor = ChartColor.FromHex("#286BB3");
        var inner = new ChartSeries(new ChartSeriesId("inner"), "Inner", ChartSeriesKind.Donut, new[] { new ChartPoint(1, 1) })
            .WithRadialBand(new ChartRadialBand(.2, .65)).WithPointSliceOffset(0, .35).WithColor(innerColor);
        var outer = new ChartSeries(new ChartSeriesId("outer"), "Outer", ChartSeriesKind.Donut, new[] { new ChartPoint(1, 1) })
            .WithRadialBand(new ChartRadialBand(.7, 1)).WithColor(outerColor);
        var chart = Chart.Create().WithSize(620, 500).WithHeader(false).WithLegend(false).WithDataLabels(false).WithDonutCenterLabel(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(inner).AddSeries(outer);
        var before = Prepare(chart); var mark = before.Scene.Nodes.OfType<VisualSceneSlice>().Single(slice => slice.Id == "series-1-point-0");
        var innerMark = before.Scene.Nodes.OfType<VisualSceneSlice>().Single(slice => slice.Id == "series-0-point-0");
        Assert.Equal(.65 * mark.Outer, innerMark.Outer, 9); Assert.Equal(mark.Cy + innerMark.Outer * .35, innerMark.Cy, 9);
        var x = (int)Math.Round(mark.Cx); var y = (int)Math.Round(mark.Cy + mark.Outer * .8);
        Assert.Equal(outerColor, Pixel(before.ToPng(), x, y));
        chart.MoveSeries(inner, 1); var reordered = Prepare(chart);
        Assert.Equal(innerColor, Pixel(reordered.ToPng(), x, y)); Assert.Equal(outerColor, Pixel(before.ToPng(), x, y));
        var xml = XDocument.Parse(reordered.ToSvg());
        Assert.Equal(new[] { "outer", "inner" }, xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radial-point")
            .Select(node => node.Attribute("data-cfx-series-key")!.Value));
        NumericRadialEncodedGeometryTests.Capture(chart, "multiring-overlap-" + (dark ? "dark" : "light"));
    }

    [Theory]
    [InlineData(ChartDataLabelPlacement.Outside, ChartDataLabelConnectorStyle.Straight)]
    [InlineData(ChartDataLabelPlacement.Left, ChartDataLabelConnectorStyle.Elbow)]
    [InlineData(ChartDataLabelPlacement.Right, ChartDataLabelConnectorStyle.Curve)]
    [InlineData(ChartDataLabelPlacement.Above, ChartDataLabelConnectorStyle.Curve)]
    [InlineData(ChartDataLabelPlacement.Below, ChartDataLabelConnectorStyle.Elbow)]
    public void InnerAndOuterExternalCaptionsUseOneCollisionOwnerAndActualConnectorOrigins(ChartDataLabelPlacement placement, ChartDataLabelConnectorStyle style) {
        var chart = Fixture(false, 900, false); chart.Options.DataLabelConnectorStyle = style;
        foreach (var series in chart.Series) { series.DataLabelPlacement = placement; series.WithDataLabels(); }
        var prepared = Prepare(chart); AssertCalloutAssociations(prepared);
        var groups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Metadata.ContainsKey("data-cfx-radial-callout")).ToArray();
        Assert.Equal(4, groups.Length);
        Assert.Contains(groups, group => group.Metadata["data-cfx-series"] == "0" && group.Metadata["data-cfx-label-dropped"] == "false");
        Assert.Contains(groups, group => group.Metadata["data-cfx-series"] == "1" && group.Metadata["data-cfx-label-dropped"] == "false");
    }

    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 900)]
    [InlineData(true, 900)]
    public void MixedPlacementAutomaticBandsRetainBoundedCalloutsAndDetachedFullSourceFacts(bool dark, int width) {
        var chart = Fixture(dark, width, true); chart.Series[0].DataLabelPlacement = ChartDataLabelPlacement.Outside;
        chart.Series[1].DataLabelPlacement = ChartDataLabelPlacement.Above; chart.Series[0].WithPointSliceOffset(0, .12);
        var prepared = Prepare(chart); AssertCalloutAssociations(prepared);
        var savedSvg = prepared.ToSvg(); var savedPng = prepared.ToPng(); var envelope = chart.ToVisualArtifact().ToInterchangeEnvelope();
        using var facts = JsonDocument.Parse(envelope.Extensions["chart.series.0.radial.sourceFacts"]);
        Assert.Equal(.12, facts.RootElement[0].GetProperty("offsetRatio").GetDouble()); Assert.Equal("alpha", facts.RootElement[0].GetProperty("observationId").GetString());
        Assert.Equal(2, facts.RootElement.GetArrayLength()); Assert.Null(chart.Series[0].RadialBand);
        chart.Series[0].WithPointSliceOffset(0, .35); chart.Series[0].Points[0] = new ChartPoint(1, 20);
        Assert.Equal(savedSvg, prepared.ToSvg()); Assert.Equal(savedPng, prepared.ToPng());
        Assert.Equal(2, facts.RootElement[0].GetProperty("value").GetDouble());
        NumericRadialEncodedGeometryTests.Capture(chart, "multiring-mixed-lanes-" + width + (dark ? "-dark" : "-light"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie, ChartDataLabelConnectorStyle.Straight)]
    [InlineData(ChartSeriesKind.Donut, ChartDataLabelConnectorStyle.Elbow)]
    [InlineData(ChartSeriesKind.Donut, ChartDataLabelConnectorStyle.Curve)]
    public void RemovingNewCompositionOptInsRestoresTheExactOrdinaryOneRingExports(ChartSeriesKind kind, ChartDataLabelConnectorStyle style) {
        var source = new ChartSeries(new ChartSeriesId("original"), "Original", kind, new[] { new ChartPoint(1, 2), new ChartPoint(2, 1) });
        var chart = Chart.Create().WithSize(700, 480).WithHeader(false).WithLegend(false).WithDataLabels()
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).WithXLabels("Alpha", "Beta").AddSeries(source);
        chart.Options.DataLabelConnectorStyle = style; source.DataLabelPlacement = ChartDataLabelPlacement.Outside;
        var original = Prepare(chart); var svg = original.ToSvg(); var png = original.ToPng();
        source.RadialBand = new ChartRadialBand(.18, .48); source.WithPointSliceOffset(0, .15);
        var second = new ChartSeries(new ChartSeriesId("second"), "Second", kind, new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) }).WithRadialBand(new ChartRadialBand(.62, 1));
        chart.AddSeries(second); Assert.NotEqual(svg, Prepare(chart).ToSvg());
        chart.RemoveSeries(second); source.RadialBand = null; source.UseDefaultSliceOffset(0);
        Assert.Equal(svg, Prepare(chart).ToSvg()); Assert.Equal(png, Prepare(chart).ToPng());
        Assert.Equal(svg, original.ToSvg()); Assert.Equal(png, original.ToPng());
    }

    [Fact]
    public void OffsetSourceFactsFollowExistingInsertRemoveIdentityLifecycleAndClearWithoutStaleMetadata() {
        var chart = Fixture(false, 700, false); var series = chart.Series[0]; series.WithPointSliceOffset(0, .2);
        var artifact = chart.ToVisualArtifact(); var captured = artifact.ToInterchangeEnvelope();
        series.Points.Insert(0, new ChartPoint(-1, 1)); Assert.Equal(.2, series.PointSliceOffsets[1]); Assert.Equal("alpha", series.ObservationIds[1].Value);
        series.Points.RemoveAt(0); Assert.Equal(.2, series.PointSliceOffsets[0]);
        using var facts = JsonDocument.Parse(artifact.ToInterchangeEnvelope().Extensions["chart.series.0.radial.sourceFacts"]);
        Assert.Equal(.2, facts.RootElement[0].GetProperty("offsetRatio").GetDouble());
        series.UseDefaultSliceOffset(0); Assert.DoesNotContain("chart.series.0.radial.sourceFacts", artifact.ToInterchangeEnvelope().Extensions.Keys);
        Assert.Contains("offsetRatio", captured.Extensions["chart.series.0.radial.sourceFacts"]);
    }

    [Fact]
    public void AggregateOtherKeepsDefaultPositionAndEveryRawContributorOffset() {
        var chart = Fixture(false, 800, false).WithDataLabels(false); var series = chart.Series[0];
        series.Points.Clear(); foreach (var value in new[] { 10d, 3, 2, 1 }) series.Points.Add(new ChartPoint(series.Points.Count + 1, value));
        for (var point = 1; point < 4; point++) series.WithPointSliceOffset(point, point * .08);
        chart.Options.MaximumPieSlices = 2; var prepared = Prepare(chart);
        Assert.Contains(prepared.Scene.Diagnostics, diagnostic => diagnostic.Code == "radial.aggregate-offsets");
        var other = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Single(mark => mark.Id == "series-0-point-other");
        var outer = prepared.Scene.Nodes.OfType<VisualSceneSlice>().First(mark => mark.Id!.StartsWith("series-1-", StringComparison.Ordinal));
        Assert.Equal(outer.Cx, other.Cx); Assert.Equal(outer.Cy, other.Cy);
        var group = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(mark => mark.Role == "radial-point" && mark.Metadata["data-cfx-series"] == "0" && mark.Metadata["data-cfx-point"] == "-1");
        Assert.Equal("1,2,3", group.Metadata["data-cfx-source-points"]); Assert.DoesNotContain("data-cfx-slice-offset-ratio", group.Metadata.Keys);
        using var facts = JsonDocument.Parse(chart.ToVisualArtifact().ToInterchangeEnvelope().Extensions["chart.series.0.radial.sourceFacts"]);
        Assert.Equal(4, facts.RootElement.GetArrayLength());
        for (var point = 1; point < 4; point++) Assert.Equal(point * .08, facts.RootElement[point].GetProperty("offsetRatio").GetDouble());
    }

    [Fact]
    public void HiddenAndSubpixelSourcesDoNotInventOffsetsOrStealAnotherRingsEnvelope() {
        var chart = Fixture(false, 800, false).WithDataLabels(false); chart.Series[0].WithPointSliceRadiusRatio(0, 0).WithPointSliceOffset(0, .35);
        var prepared = Prepare(chart); Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualSceneSlice>().Count());
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "radial-point" && group.Metadata["data-cfx-series"] == "0" && group.Metadata["data-cfx-point"] == "0");
        var source = chart.Series[0].Points.ToArray(); var bounds = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Single(mark => mark.Id == "series-1-point-0");
        chart.Series[0].UseDefaultSliceOffset(0); var restored = Prepare(chart);
        var same = restored.Scene.Nodes.OfType<VisualSceneSlice>().Single(mark => mark.Id == "series-1-point-0");
        Assert.Equal(bounds.Cx, same.Cx); Assert.Equal(bounds.Cy, same.Cy); Assert.Equal(bounds.Outer, same.Outer, 9); Assert.Equal(source, chart.Series[0].Points);
    }

    internal static void AssertCalloutAssociations(PreparedVisual prepared) {
        var labels = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Metadata.TryGetValue("data-cfx-radial-callout", out var mode) && mode == "overlay").ToArray();
        var kept = labels.Where(label => label.Metadata["data-cfx-label-dropped"] == "false").ToArray(); Assert.NotEmpty(kept);
        var boxes = kept.Select(label => new ChartRect(Number(label, "left"), Number(label, "top"), Number(label, "width"), Number(label, "height"))).ToArray();
        for (var index = 0; index < boxes.Length; index++) for (var other = 0; other < index; other++)
            Assert.False(boxes[index].Left < boxes[other].Right && boxes[index].Right > boxes[other].Left && boxes[index].Top < boxes[other].Bottom && boxes[index].Bottom > boxes[other].Top);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneSlice>().ToDictionary(mark => mark.Id!);
        foreach (var label in kept) {
            var id = label.Metadata["data-cfx-label-for"]; Assert.Contains(id, marks.Keys);
            var connector = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "data-label-connector" && path.Id == id + "-connector");
            var mark = marks[id]; var mid = mark.Start + mark.Sweep / 2;
            Assert.Equal(mark.Cx + Math.Cos(mid) * mark.Outer, connector.Commands[0].X, 8);
            Assert.Equal(mark.Cy + Math.Sin(mid) * mark.Outer, connector.Commands[0].Y, 8);
            var shape = new LabelMarkShape(VisualSceneGeometry.Flatten(connector, 1), false, connector.StrokeWidth + 2);
            for (var other = 0; other < kept.Length; other++) if (kept[other] != label) Assert.False(shape.Intersects(boxes[other]));
        }
        static double Number(VisualSceneGroup group, string suffix) => double.Parse(group.Metadata["data-cfx-label-" + suffix], CultureInfo.InvariantCulture);
    }

    internal static Chart Fixture(bool dark, int width, bool automatic) {
        var chart = Chart.Create().WithSize(width, 540).WithHeader(false).WithLegend(false).WithDonutCenterLabel(false)
            .WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).WithXLabels("Alpha", "Beta")
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        if (automatic) chart.WithAutomaticRadialBands(new ChartRadialBandAllocation(.18, .14));
        for (var index = 0; index < 2; index++) {
            var series = new ChartSeries(new ChartSeriesId("ring-" + index), "Ring " + index, ChartSeriesKind.Donut,
                index == 0 ? new[] { new ChartPoint(1, 2), new ChartPoint(2, 1) } : new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) })
                .WithObservationIds(new[] { new ChartObservationId("alpha"), new ChartObservationId("beta") });
            if (!automatic) series.WithRadialBand(index == 0 ? new ChartRadialBand(.18, .48) : new ChartRadialBand(.62, 1));
            series.DataLabelPlacement = ChartDataLabelPlacement.Outside; chart.AddSeries(series);
        }
        return chart;
    }

    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static ChartColor Pixel(byte[] png, int x, int y) {
        var image = PngReader.Decode(png); var offset = (y * image.Width + x) * 4;
        return ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
    }
}
