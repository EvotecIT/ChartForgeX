using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MapLabelClusterDepthTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    public void GridBadgesRetainEveryRawIdentityFeatureWeightColorAndViewportFact(string variant, VisualThemeMode mode) {
        var chart = MapNavigationDepthRecipe.Create(variant, mode);
        var prepared = Prepare(chart); Capture(prepared, "map-navigation-" + variant + "-" + mode);
        var sources = Groups(prepared, "dotted-map-point-source"); Assert.Equal(5, sources.Length);
        Assert.Equal(new[] { "west", "intake", "processing", "dispatch", "outside" }, sources.Select(source => source.Metadata["data-cfx-observation-id"]));
        Assert.Equal(new[] { "10", "20", "30", "40", "80" }, sources.Select(source => source.Metadata["data-cfx-value"]));
        Assert.Equal(new[] { "-5", "0", "15", "25", "80" }, sources.Select(source => source.Metadata["data-cfx-color-value"]));
        Assert.All(sources, source => { Assert.Equal("10", source.Metadata["data-cfx-size-min-value"]); Assert.Equal("80", source.Metadata["data-cfx-size-max-value"]); });
        var cluster = Assert.Single(Groups(prepared, "dotted-map-cluster-source"));
        Assert.Equal("3", cluster.Metadata["data-cfx-source-count"]); Assert.Equal("1,2,3", cluster.Metadata["data-cfx-source-points"]);
        Assert.Equal("map-grid-cluster", cluster.Metadata["data-cfx-derived"]); Assert.False(cluster.Metadata.ContainsKey("data-cfx-observation-id"));
        Assert.Equal(chart.Options.Labels.SampleCount, cluster.Metadata["data-cfx-label-value"]);
        foreach (var source in sources.Skip(1).Take(3)) {
            Assert.Equal("true", source.Metadata["data-cfx-clustered"]); Assert.Equal("false", source.Metadata["data-cfx-visible"]);
            Assert.Equal("true", source.Metadata["data-cfx-viewport-visible"]); Assert.True(source.Metadata.ContainsKey("data-cfx-feature-properties"));
            Assert.Equal(cluster.Metadata["data-cfx-cluster-grid"], source.Metadata["data-cfx-cluster-grid"]);
        }
        Assert.Equal("false", sources[4].Metadata["data-cfx-visible"]); Assert.False(sources[4].Metadata.ContainsKey("data-cfx-clustered"));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "dotted-map-point");
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "dotted-map-cluster");
        Assert.Equal("3", string.Join("", Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "dotted-map-cluster-count").Text.Lines.Select(line => line.Text)));
        var exported = XDocument.Parse(prepared.ToSvg()); Assert.DoesNotContain(exported.Descendants(), node => node.Name.LocalName == "script");
        Assert.Equal(5, exported.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "dotted-map-point-source"));
        var image = RasterImageDecoder.Decode(prepared.ToPng()); Assert.Equal(variant == "compact" ? 390 : 800, image.Width); Assert.Equal(600, image.Height);
    }

    [Theory]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    public void SharedCalloutsRetainFullNamesBoundLeaderReachAndPreventCaptionIntersections(string variant, VisualThemeMode mode) {
        var chart = MapNavigationDepthRecipe.Create(variant, mode); var prepared = Prepare(chart);
        var callouts = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Metadata.ContainsKey("data-cfx-callout")).ToArray();
        Assert.True(callouts.Length >= 3);
        var accepted = callouts.Where(group => group.Metadata["data-cfx-label-dropped"] == "false").ToArray(); Assert.NotEmpty(accepted);
        var boxes = new List<ChartRect>();
        foreach (var group in accepted) {
            double Read(string field) => double.Parse(group.Metadata[field], CultureInfo.InvariantCulture);
            var x = Read("data-cfx-anchor-x") - Read("data-cfx-leader-x"); var y = Read("data-cfx-anchor-y") - Read("data-cfx-leader-y");
            Assert.InRange(Math.Sqrt(x * x + y * y), 0, chart.Options.MapCallouts!.MaximumDistance + 1e-8);
            var box = new ChartRect(Read("data-cfx-label-left"), Read("data-cfx-label-top"), Read("data-cfx-label-width"), Read("data-cfx-label-height"));
            Assert.DoesNotContain(boxes, previous => previous.Left < box.Right && box.Left < previous.Right && previous.Top < box.Bottom && box.Top < previous.Bottom);
            boxes.Add(box);
        }
        Assert.Contains(callouts, group => group.Metadata["data-cfx-full-label"] == "West service zone");
        Assert.Contains(callouts, group => group.Metadata["data-cfx-full-label"] == "North service zone");
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "dotted-map-label-leader");
    }

    [Fact]
    public void CountBadgeGeometryIsOrderIndependentAndPreparedFactsStayDetached() {
        var chart = MapNavigationDepthRecipe.Create(); var prepared = Prepare(chart);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var old = Assert.Single(Groups(prepared, "dotted-map-cluster-source"));
        var series = chart.Series[1]; var points = series.MapPoints.ToArray();
        series.ReplaceMapPoints(new[] { points[0], points[3], points[2], points[1], points[4] });
        series.WithObservationIds(new[] { new ChartObservationId("west"), new ChartObservationId("dispatch"), new ChartObservationId("processing"), new ChartObservationId("intake"), new ChartObservationId("outside") });
        var reordered = Prepare(chart); var cluster = Assert.Single(Groups(reordered, "dotted-map-cluster-source"));
        Assert.Equal(old.Metadata["data-cfx-cluster-center-x"], cluster.Metadata["data-cfx-cluster-center-x"]);
        Assert.Equal(old.Metadata["data-cfx-cluster-center-y"], cluster.Metadata["data-cfx-cluster-center-y"]);
        Assert.Equal(old.Metadata["data-cfx-derived-identity"], cluster.Metadata["data-cfx-derived-identity"]);
        Assert.Equal(png, reordered.ToPng()); Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        series.WithMapClustering(null);
        var expanded = Prepare(chart); Assert.Empty(Groups(expanded, "dotted-map-cluster-source"));
        Assert.Equal(4, expanded.Scene.Nodes.OfType<VisualSceneEllipse>().Count(node => node.Role == "dotted-map-point"));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void ClustersNeverCrossDatasetBoundariesAndEmptyOrBelowThresholdInputsKeepTheirRawFacts() {
        var first = ChartSeries.CreateDottedMap(new ChartSeriesId("first"), "First", new[] { new ChartMapPoint("A", 10, 50) }).WithMapClustering(new ChartMapClustering());
        var second = ChartSeries.CreateDottedMap(new ChartSeriesId("second"), "Second", new[] { new ChartMapPoint("B", 10, 50) }).WithMapClustering(new ChartMapClustering());
        var chart = Chart.Create().WithSize(520, 360).WithMapViewport(ChartMapViewport.Europe()).AddSeries(first).AddSeries(second);
        var prepared = Prepare(chart); Assert.Empty(Groups(prepared, "dotted-map-cluster-source"));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Count(node => node.Role == "dotted-map-point"));
        first.ReplaceMapPoints(new[] { new ChartMapPoint("A", 10, 50), new ChartMapPoint("A2", 10, 50) }).WithMapClustering(new ChartMapClustering(48, 3));
        Assert.Empty(Groups(Prepare(chart), "dotted-map-cluster-source"));
        first.ReplaceMapPoints(Array.Empty<ChartMapPoint>()); second.ReplaceMapPoints(Array.Empty<ChartMapPoint>());
        var empty = Prepare(chart); Assert.Empty(Groups(empty, "dotted-map-cluster-source"));
        Assert.Equal(2, empty.Diagnostics.Count(diagnostic => diagnostic.Code == "map.no-data"));
    }

    [Fact]
    public void OptInPoliciesValidateAndClearingRestoresDefaultSceneBytes() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartMapClustering(0)); Assert.Throws<ArgumentOutOfRangeException>(() => new ChartMapClustering(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartMapClustering(48, 1)); Assert.Throws<ArgumentOutOfRangeException>(() => new ChartMapClustering(48, 2, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartMapCallouts(0)); Assert.Throws<ArgumentOutOfRangeException>(() => new ChartMapCallouts(48, (ChartMapRegionLabel)999));
        Assert.Throws<InvalidOperationException>(() => ChartSeries.CreateLine(new ChartSeriesId("line"), "Line", new[] { new ChartPoint(0, 1) }).WithMapClustering(new ChartMapClustering()));
        var chart = MapNavigationDepthRecipe.Create(); var sites = chart.Series[1]; var route = sites.MapConnectors[0];
        sites.WithMapClustering(null).ReplaceMapConnectors(new[] { route.WithEndpointClearance(null) }); chart.Options.MapCallouts = null;
        var baseline = Prepare(chart); var svg = baseline.ToSvg(); var png = baseline.ToPng();
        sites.WithMapClustering(new ChartMapClustering()).ReplaceMapConnectors(new[] { route }); chart.Options.MapCallouts = new ChartMapCallouts(); _ = Prepare(chart);
        sites.WithMapClustering(null).ReplaceMapConnectors(new[] { route.WithEndpointClearance(null) }); chart.Options.MapCallouts = null;
        Assert.Equal(svg, Prepare(chart).ToSvg()); Assert.Equal(png, Prepare(chart).ToPng());
    }

    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    internal static VisualSceneGroup[] Groups(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == role).ToArray();
    internal static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg()); File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
    }
}
