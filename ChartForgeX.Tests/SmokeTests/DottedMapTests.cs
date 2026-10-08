using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Verify = Xunit.Assert;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static VisualSceneEllipse[] MapMarkers(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray();
    private static ChartRect MapTextBounds(VisualSceneText text) => new(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
    private static bool MapOverlap(ChartRect first, ChartRect second) => first.Left < second.Right && first.Right > second.Left && first.Top < second.Bottom && first.Bottom > second.Top;
    private static void AssertMapCaptionsAvoidMarks(PreparedVisual prepared, int minimumLabels) {
        var labels = FamilyLabels(prepared, "dotted-map-data-label").Select(MapTextBounds).ToArray();
        Verify.True(labels.Length >= minimumLabels);
        var halos = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point-halo")
            .Select(mark => new ChartRect(mark.Cx - mark.Rx, mark.Cy - mark.Ry, mark.Rx * 2, mark.Ry * 2)).ToArray();
        for (var index = 0; index < labels.Length; index++) {
            Verify.InRange(labels[index].Left, 0, prepared.Size.Width); Verify.InRange(labels[index].Right, 0, prepared.Size.Width);
            Verify.InRange(labels[index].Top, 0, prepared.Size.Height); Verify.InRange(labels[index].Bottom, 0, prepared.Size.Height);
            Verify.DoesNotContain(halos, halo => MapOverlap(labels[index], halo));
            Verify.DoesNotContain(labels.Skip(index + 1), other => MapOverlap(labels[index], other));
        }
    }

    private static void DottedMapRendersWorldDotsAndPoints() {
        var chart = Chart.Create().WithSize(760, 420).WithTitle("Travel Map").AddDottedMap("Visited", new[] {
            new ChartMapPoint("Indonesia", 113.9213, -.7893, ChartColor.FromHex("#22C55E")),
            new("Spain", -3.7038, 40.4168), new("United States", -98.5795, 39.8283)
        });
        var prepared = PreparedFamily(chart); var source = Verify.Single(FamilyGroups(prepared, "dotted-map"));
        Verify.Equal("equirectangular", source.Metadata["data-cfx-projection"]); Verify.Equal("3", source.Metadata["data-cfx-point-count"]);
        var points = FamilyGroups(prepared, "dotted-map-point-source"); Verify.Equal(3, points.Length);
        Verify.Equal(113.9213, FamilyNumber(points[0], "data-cfx-longitude")); Verify.Equal(-.7893, FamilyNumber(points[0], "data-cfx-latitude"));
        Verify.Equal(3, MapMarkers(prepared).Length); Verify.Equal(ChartColor.FromHex("#22C55E"), MapMarkers(prepared)[0].Fill);
        Verify.Contains(prepared.Regions, region => region.Role == "dotted-map-point" && region.Label!.Contains("Indonesia"));
        Verify.True(prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dotted-map-boundary" && node.Fill.HasValue).Sum(node => node.Commands.Count) > 100);
        Verify.NotEmpty(prepared.ToPng());
        var dark = PreparedFamily(Chart.Create().WithTheme(ChartTheme.ReportDark()).AddDottedMap("Visited", new[] { new ChartMapPoint("Spain", -3.7038, 40.4168) }));
        Verify.Contains(dark.Scene.Nodes, node => node.Role == "dotted-map-land-dot");
        AssertThrows<ArgumentException>(() => Chart.Create().AddDottedMap("Empty", Array.Empty<ChartMapPoint>()), "Map inputs must not be empty.");
        AssertThrows<ArgumentException>(() => new ChartMapPoint(" ", 0, 0), "Map labels must be nonempty.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapPoint("Bad", 181, 0), "Longitude must remain geographic.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapPoint("Bad", 0, 91), "Latitude must remain geographic.");
    }

    private static void DottedMapTrimsPointLabels() {
        var prepared = PreparedFamily(Chart.Create().AddDottedMap("Visited", new[] { new ChartMapPoint("  Spain  ", -3.7038, 40.4168) }));
        var point = Verify.Single(FamilyGroups(prepared, "dotted-map-point-source"));
        Verify.Equal("Spain", point.Metadata["data-cfx-label"]);
        Verify.Contains("Spain;", point.Metadata["aria-label"]);
    }

    private static void DottedMapWorldViewportSuppressesPolarPointsOutsideMapBand() => AssertPolarMapPoints(false);
    private static void DottedMapDataLabelsSuppressPolarPointsOutsideMapBand() => AssertPolarMapPoints(true);
    private static void AssertPolarMapPoints(bool labels) {
        var chart = Chart.Create().WithSize(360, 220).WithDataLabels(labels).AddDottedMap("Extremes", new[] {
            new ChartMapPoint("North Pole", 0, 90), new("South Pole", 0, -90)
        });
        var prepared = PreparedFamily(chart);
        Verify.Equal("0", Verify.Single(FamilyGroups(prepared, "dotted-map")).Metadata["data-cfx-visible-point-count"]);
        Verify.Empty(MapMarkers(prepared)); Verify.Empty(FamilyLabels(prepared, "dotted-map-data-label"));
        var sources = FamilyGroups(prepared, "dotted-map-point-source");
        Verify.Equal(2, sources.Length); Verify.All(sources, source => Verify.Equal("false", source.Metadata["data-cfx-visible"]));
        Verify.Equal(new[] { "North Pole", "South Pole" }, sources.Select(source => source.Metadata["data-cfx-label"]));
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void DottedMapClusteredLabelsUseAlternatePlacements() {
        var chart = Chart.Create().WithSize(520, 300).WithDataLabels().AddDottedMap("Cluster",
            new[] { "A", "B", "C", "D" }.Select(label => new ChartMapPoint(label, 0, 0)));
        var prepared = PreparedFamily(chart); AssertMapCaptionsAvoidMarks(prepared, 4); Verify.NotEmpty(prepared.ToPng());
    }

    private static void DottedMapDenseClustersUseDiagonalLabelPlacements() {
        var chart = Chart.Create().WithSize(520, 300).WithDataLabels().AddDottedMap("Cluster",
            new[] { "Alpha Market", "Beta Market", "Central Market", "Delta Market", "Eastern Market", "Frontier Market" }.Select(label => new ChartMapPoint(label, 0, 0)));
        var prepared = PreparedFamily(chart); AssertMapCaptionsAvoidMarks(prepared, 4);
        Verify.Equal(6, FamilyGroups(prepared, "dotted-map-data-label-source").Length);
        Verify.NotEmpty(prepared.ToPng());
    }

    private static int CountVisibleMapLabels(string svg) => System.Xml.Linq.XDocument.Parse(svg).Descendants()
        .Count(element => (string?)element.Attribute("data-cfx-role") == "dotted-map-data-label");

    private static void DottedMapPreservesMapAspectRatioInTallCards() {
        var chart = Chart.Create().WithSize(420, 620).AddDottedMap("Visited", new[] { new ChartMapPoint("Equator", 0, 0) });
        var prepared = PreparedFamily(chart); var marker = Verify.Single(MapMarkers(prepared));
        Verify.InRange(marker.Cy, prepared.Size.Height * .3, prepared.Size.Height * .7);
        Verify.True(prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dotted-map-boundary" && node.Fill.HasValue).Sum(node => node.Commands.Count) > 100);
        Verify.NotEmpty(prepared.ToPng());
    }

    private static void DottedMapViewportFocusesRegionalMaps() {
        var chart = Chart.Create().WithSize(420, 320).WithMapViewport(ChartMapViewport.Europe()).AddDottedMap("Visited", new[] {
            new ChartMapPoint("Spain", -3.7038, 40.4168), new("Poland", 19.1451, 51.9194), new("United States", -98.5795, 39.8283)
        });
        var prepared = PreparedFamily(chart);
        Verify.Equal("Europe", Verify.Single(FamilyGroups(prepared, "dotted-map")).Metadata["data-cfx-viewport"]);
        Verify.Equal(2, MapMarkers(prepared).Length);
        var sources = FamilyGroups(prepared, "dotted-map-point-source");
        Verify.Equal(new[] { "true", "true", "false" }, sources.Select(source => source.Metadata["data-cfx-visible"]));
        Verify.Contains(prepared.Scene.Nodes, node => node.Role == "dotted-map-boundary");
        Verify.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "dotted-map-land-dot");
        var poland = PreparedFamily(Chart.Create().WithSize(420, 320).WithMapViewport(ChartMapViewport.Poland())
            .AddDottedMap("Cities", new[] { new ChartMapPoint("Warsaw", 21.0122, 52.2297) }));
        Verify.Contains(poland.Scene.Nodes, node => node.Role == "dotted-map-outline");
        Verify.NotEmpty(poland.ToPng());
        var custom = PreparedFamily(Chart.Create().WithMapViewport(new ChartMapViewport("Iberia", -10, 5, 35, 45))
            .AddDottedMap("Cities", new[] { new ChartMapPoint("Madrid", -3.7038, 40.4168) }));
        Verify.Contains(custom.Scene.Nodes, node => node.Role == "dotted-map-land-dot");
        AssertThrows<ArgumentException>(() => new ChartMapViewport(" ", -10, 10, -10, 10), "Viewports need a name.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapViewport("Bad", 10, 10, -10, 10), "Viewport bounds must be ordered.");
    }

    private static void DottedMapRegionalLabelsAvoidMarkerHalos() {
        var chart = Chart.Create().WithSize(360, 260).WithMapViewport(ChartMapViewport.Poland()).WithDataLabels().AddDottedMap("Cities", new[] {
            new ChartMapPoint("Gdansk", 18.6466, 54.3520), new("Warsaw", 21.0122, 52.2297), new("Krakow", 19.9450, 50.0647)
        });
        var prepared = PreparedFamily(chart); AssertMapCaptionsAvoidMarks(prepared, 3); Verify.NotEmpty(prepared.ToPng());
    }

    private static void DottedMapRendersWeightedRevenueMarkers() {
        var chart = Chart.Create().WithSize(520, 320).WithMapViewport(ChartMapViewport.Europe()).WithDataLabels()
            .WithValueFormatter(value => "$" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "k")
            .AddDottedMap("Revenue", new[] {
                new ChartMapPoint("Poland", 19.1451, 51.9194, 142, ChartColor.FromHex("#DC2626")),
                new("Germany", 10.4515, 51.1657, 214, ChartColor.FromHex("#22C55E")), new("Spain", -3.7038, 40.4168, 96, ChartColor.FromHex("#F59E0B"))
            });
        var prepared = PreparedFamily(chart); var source = Verify.Single(FamilyGroups(prepared, "dotted-map"));
        Verify.Equal("96", source.Metadata["data-cfx-min-value"]); Verify.Equal("214", source.Metadata["data-cfx-max-value"]);
        var germany = FamilyGroups(prepared, "dotted-map-point-source")[1];
        Verify.Equal("214", germany.Metadata["data-cfx-value"]); Verify.Equal("$214k", germany.Metadata["data-cfx-formatted-value"]);
        Verify.True(MapMarkers(prepared)[1].Rx > MapMarkers(prepared)[2].Rx);
        Verify.Contains(FamilyLabels(prepared, "dotted-map-data-label"), label => FamilyContent(label).Contains("Germany $214k"));
        AssertMapCaptionsAvoidMarks(prepared, 3); Verify.NotEmpty(prepared.ToPng());
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapPoint("Bad", 0, 0, -1), "Map weights must be nonnegative.");
    }

    private static void DottedMapRendersConnectorRoutes() {
        var chart = Chart.Create().WithSize(520, 300).WithMapViewport(ChartMapViewport.Europe()).AddDottedMap("Visited", new[] {
            new ChartMapPoint("Spain", -3.7038, 40.4168), new("Warsaw", 21.0122, 52.2297), new("Oslo", 10.7522, 59.9139)
        }).AddMapRouteBetweenPoints("Spain to Warsaw", "Spain", "Warsaw", ChartColor.FromHex("#22C55E"))
            .AddMapConnectorBetweenPoints("Warsaw to Oslo", "Warsaw", "Oslo");
        var prepared = PreparedFamily(chart);
        Verify.Equal(2, FamilyGroups(prepared, "dotted-map-connector-source").Length);
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dotted-map-connector").ToArray();
        Verify.Equal(2, paths.Length); Verify.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "dotted-map-connector-arrow"));
        var marker = MapMarkers(prepared)[0]; var start = paths[0].Commands[0];
        Verify.True(Math.Sqrt(Math.Pow(start.X - marker.Cx, 2) + Math.Pow(start.Y - marker.Cy, 2)) > marker.Rx);
        Verify.True(prepared.Scene.Nodes.ToList().IndexOf(paths[0]) < prepared.Scene.Nodes.ToList().IndexOf(marker));
        Verify.Equal(ChartColor.FromHex("#22C55E").R, paths[0].Stroke!.Value.R);
        Verify.NotEmpty(prepared.ToPng());
        Verify.Equal(2, FamilyLabels(PreparedFamily(chart.WithDataLabels()), "dotted-map-connector-label").Length);
        AssertThrows<ArgumentException>(() => Chart.Create().AddMapConnector(" ", 0, 0, 1, 1), "Routes need a label.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddMapConnector("Bad", -181, 0, 1, 1), "Route coordinates must be geographic.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddMapRouteBetweenPoints("Bad", "Spain", "Warsaw"), "Bound routes require source points.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddDottedMap("Visited", new[] { new ChartMapPoint("Spain", -3.7038, 40.4168) }).AddMapRouteBetweenPoints("Bad", "Spain", "Missing"), "Bound routes require known endpoints.");
        var ports = new[] { new ChartMapPoint("Rotterdam", 4.4792, 51.9244), new ChartMapPoint("Singapore", 103.8198, 1.3521) };
        var sea = Chart.Create().WithSize(760, 420).AddDottedMap("Ports", ports)
            .AddMapRoute("Via Suez", new[] { ports[0], new ChartMapPoint("Suez", 32.5498, 29.9668), ports[1] })
            .AddMapRoute("Via Cape", new[] { ports[0], new ChartMapPoint("Cape", 18.4741, -34.3587), ports[1] });
        var seaPrepared = PreparedFamily(sea);
        Verify.All(FamilyGroups(seaPrepared, "dotted-map-connector-source"), route => { Verify.Equal("waypoint", route.Metadata["data-cfx-route-kind"]); Verify.Equal("3", route.Metadata["data-cfx-waypoint-count"]); });
        Verify.All(seaPrepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dotted-map-connector"),
            route => Verify.True(route.Commands.Count >= 3));
        Verify.NotEmpty(seaPrepared.ToPng());
    }
}
