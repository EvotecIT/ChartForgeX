using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BuiltInRegionMapTests {
    [Theory]
    [InlineData("world", 249, ChartMapGeoJsonProjection.Miller)]
    [InlineData("world-mercator", 249, ChartMapGeoJsonProjection.WebMercator)]
    [InlineData("us-states", 51, ChartMapGeoJsonProjection.UnitedStatesAlbers)]
    [InlineData("us-states-lambert", 51, ChartMapGeoJsonProjection.UnitedStatesLambert)]
    [InlineData("us-states-mercator", 51, ChartMapGeoJsonProjection.UnitedStatesMercator)]
    [InlineData("us-states-miller", 51, ChartMapGeoJsonProjection.UnitedStatesMiller)]
    [InlineData("continents", 6, ChartMapGeoJsonProjection.Miller)]
    [InlineData("africa", 54, ChartMapGeoJsonProjection.Miller)]
    [InlineData("asia", 55, ChartMapGeoJsonProjection.Miller)]
    [InlineData("europe", 51, ChartMapGeoJsonProjection.Miller)]
    [InlineData("north-america", 40, ChartMapGeoJsonProjection.Miller)]
    [InlineData("oceania", 26, ChartMapGeoJsonProjection.Miller)]
    [InlineData("south-america", 14, ChartMapGeoJsonProjection.Miller)]
    [InlineData("spain", 52, ChartMapGeoJsonProjection.Miller)]
    [InlineData("russia", 83, ChartMapGeoJsonProjection.Miller)]
    [InlineData("canada", 13, ChartMapGeoJsonProjection.Miller)]
    [InlineData("iraq", 18, ChartMapGeoJsonProjection.Miller)]
    [InlineData("brazil", 27, ChartMapGeoJsonProjection.Miller)]
    [InlineData("france", 13, ChartMapGeoJsonProjection.Miller)]
    [InlineData("germany", 16, ChartMapGeoJsonProjection.Miller)]
    [InlineData("belgium", 11, ChartMapGeoJsonProjection.Miller)]
    [InlineData("netherlands", 12, ChartMapGeoJsonProjection.Miller)]
    [InlineData("united-kingdom", 4, ChartMapGeoJsonProjection.Miller)]
    public void BuiltInMapsShipGeometryWithADeclaredProjectionAndRecordedSource(string id, int regions, ChartMapGeoJsonProjection projection) {
        var definition = ChartMapCatalog.Get(id);
        var entry = ChartMapCatalog.GetEntry(id);

        Assert.Equal(regions, definition.Regions.Count);
        Assert.Equal(projection, definition.GeographicProjection);
        Assert.Same(definition, ChartMapCatalog.Get(id));
        Assert.True(entry.IsEmbedded);
        Assert.False(string.IsNullOrWhiteSpace(entry.Source));
        Assert.StartsWith("https://", entry.SourceUrl, StringComparison.Ordinal);
        Assert.StartsWith("Public domain", entry.License, StringComparison.Ordinal);
        Assert.All(definition.Regions, region => Assert.True(region.HasLabel && LargestRingArea(region.Path) > 0, region.Code));
        Assert.Equal(projection, entry.CreateOptions().Projection);
    }

    [Theory]
    [InlineData("world", "SS", "SSD", "South Sudan")]
    [InlineData("world", "PS", "PSE", "Palestine")]
    [InlineData("world", "EH", "ESH", "Western Sahara")]
    [InlineData("africa", "SS", "SSD", "South Sudan")]
    public void CountriesKeepIsoCodesWhenNaturalEarthUsesItsOwnCode(string id, string code, string alpha3, string name) {
        var definition = ChartMapCatalog.Get(id);

        Assert.True(definition.TryResolveRegion(code, out var fromCode));
        Assert.True(definition.TryResolveRegion(alpha3, out var fromAlias));
        Assert.True(definition.TryResolveRegion(name, out var fromName));
        Assert.Equal(code, fromCode);
        Assert.Equal(code, fromAlias);
        Assert.Equal(code, fromName);
    }

    [Fact]
    public void EmbeddedEntriesMatchTheCatalogAndLoadAsDefinitions() {
        Assert.Equal(23, ChartMapCatalog.EmbeddedEntries().Count);
        Assert.Equal(ChartMapCatalog.EmbeddedEntries().Select(entry => entry.Id), ChartMapCatalog.All().Select(definition => definition.Id));
        Assert.All(ChartMapCatalog.ExternalEntries(), entry => Assert.False(string.IsNullOrWhiteSpace(entry.License)));
    }

    [Theory]
    [InlineData("world", "PL", "POL", "Poland")]
    [InlineData("world", "GB", "GBR", "United Kingdom")]
    [InlineData("world", "AU", "AUS", "Australia")]
    [InlineData("russia", "RU-MOW", "Moscow", "Moskva")]
    [InlineData("russia", "RU-MOS", "Moscow Oblast", "Moskovskaya")]
    [InlineData("europe", "DE", "DEU", "Germany")]
    [InlineData("us-states-mercator", "TX", "US-TX", "Texas")]
    [InlineData("us-states", "CA", "US-CA", "California")]
    [InlineData("spain", "ES-M", "M", "Madrid")]
    [InlineData("canada", "CA-ON", "ON", "Ontario")]
    [InlineData("france", "FR-IDF", "IDF", "Île-de-France")]
    [InlineData("united-kingdom", "GB-SCT", "SCT", "Scotland")]
    [InlineData("continents", "EU", "EU", "Europe")]
    public void RegionsResolveByCodeAliasAndName(string id, string code, string alias, string name) {
        var definition = ChartMapCatalog.Get(id);

        Assert.True(definition.TryResolveRegion(alias, out var fromAlias));
        Assert.True(definition.TryResolveRegion(name, out var fromName));
        Assert.Equal(code, fromAlias);
        Assert.Equal(code, fromName);
    }

    [Fact]
    public void RussiaLeavesCrimeaToUkraine() {
        var russia = ChartMapCatalog.Get("russia");

        Assert.False(russia.TryResolveRegion("UA-43", out _));
        Assert.False(russia.TryResolveRegion("Crimea", out _));
        Assert.True(ChartMapCatalog.Get("world").TryResolveRegion("UA", out _));
    }

    [Fact]
    public void CroppedContinentsUseTheirWindowAsBounds() {
        var europe = ChartMapCatalog.Get("europe");
        var topLeft = ChartGeographicProjection.Project(-25, 72, ChartMapGeoJsonProjection.Miller);
        var bottomRight = ChartGeographicProjection.Project(45, 34, ChartMapGeoJsonProjection.Miller);

        Assert.Equal(topLeft.X, europe.Bounds.Left, 6);
        Assert.Equal(topLeft.Y, europe.Bounds.Top, 6);
        Assert.Equal(bottomRight.X, europe.Bounds.Right, 6);
        Assert.Equal(bottomRight.Y, europe.Bounds.Bottom, 6);
    }

    [Fact]
    public void CountryLabelsUseTheCuratedMainlandPoint() {
        var france = ChartMapCatalog.Get("world").Regions.Single(region => region.Code == "FR");

        // French Guiana and the overseas departments must not drag the label off the mainland.
        Assert.InRange(france.Label.X, -2, 6);
        Assert.InRange(-france.Label.Y, 50, 60);
    }

    [Theory]
    [InlineData("us-states", ChartMapGeoJsonProjection.UnitedStatesAlbers)]
    [InlineData("us-states-lambert", ChartMapGeoJsonProjection.UnitedStatesLambert)]
    [InlineData("us-states-mercator", ChartMapGeoJsonProjection.UnitedStatesMercator)]
    [InlineData("us-states-miller", ChartMapGeoJsonProjection.UnitedStatesMiller)]
    public void UnitedStatesProjectionsPlaceCitiesInsideTheirState(string id, ChartMapGeoJsonProjection projection) {
        var definition = ChartMapCatalog.Get(id);

        AssertInside(definition, "CO", ChartGeographicProjection.Project(-104.99, 39.74, projection));
        AssertInside(definition, "NY", ChartGeographicProjection.Project(-73.76, 42.65, projection));
        AssertInside(definition, "AK", ChartGeographicProjection.Project(-149.9, 61.2, projection));
        AssertInside(definition, "HI", ChartGeographicProjection.Project(-157.86, 21.31, projection));
    }

    [Fact]
    public void UnitedStatesCompositesCannotCrossTheAntimeridian() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChartMapViewport.AcrossAntimeridian("Pacific", 150, -150, -10, 60, ChartMapGeoJsonProjection.UnitedStatesAlbers));
    }

    [Theory]
    [InlineData("world", "miller")]
    [InlineData("us-states", "united-states-albers")]
    [InlineData("us-states-lambert", "united-states-lambert")]
    public void MarkersAndRoutesComposeOnBuiltInMaps(string id, string projection) {
        var definition = ChartMapCatalog.Get(id);
        var chart = Chart.Create().WithSize(800, 500)
            .AddRegionMap("Coverage", definition, new[] { new ChartRegionMapItem(definition.Regions[0].Code, 40), new ChartRegionMapItem(definition.Regions[1].Code, 80) })
            .AddDottedMap("Offices", new[] {
                new ChartMapPoint("Denver", -104.99, 39.74, 20),
                new ChartMapPoint("New York", -74.0, 40.71, 80, ChartColor.FromHex("#DC2626"))
            })
            .AddMapRoute("Denver to New York", -104.99, 39.74, -74.0, 40.71);

        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var nodes = prepared.Scene.Nodes;
        var map = Assert.Single(nodes.OfType<VisualSceneGroup>(), node => node.Role == "geographic-map");
        Assert.Equal(projection, map.Metadata["data-cfx-projection"]);
        Assert.Equal(definition.Regions.Count, nodes.OfType<VisualScenePath>().Count(node => node.Role == "region-map-region"));
        var markers = nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").ToArray();
        Assert.Equal(2, markers.Length);
        Assert.True(markers[1].Rx > markers[0].Rx);
        Assert.True(markers[1].Cx > markers[0].Cx, "New York should sit east of Denver.");
        Assert.Equal(1, nodes.OfType<VisualScenePath>().Count(node => node.Role == "dotted-map-connector"));
        Assert.Contains("data-cfx-projection=\"" + projection + "\"", prepared.ToSvg(), StringComparison.Ordinal);
    }

    // Shoelace area of the largest ring in an M/L/Z path; zero means the region would draw nothing.
    private static double LargestRingArea(string path) {
        var largest = 0d;
        foreach (var ring in path.Split(new[] { 'M' }, StringSplitOptions.RemoveEmptyEntries)) {
            var numbers = Regex.Matches(ring, @"-?\d+(?:\.\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
            var area = 0d;
            for (var i = 0; i + 3 < numbers.Length; i += 2) area += numbers[i] * numbers[i + 3] - numbers[i + 2] * numbers[i + 1];
            if (numbers.Length >= 6) area += numbers[^2] * numbers[1] - numbers[0] * numbers[^1];
            largest = Math.Max(largest, Math.Abs(area / 2));
        }

        return largest;
    }

    private static void AssertInside(ChartMapDefinition definition, string code, ChartPoint point) {
        var path = definition.Regions.Single(region => region.Code == code).Path;
        var numbers = Regex.Matches(path, @"-?\d+(?:\.\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
        for (var i = 0; i + 1 < numbers.Length; i += 2) {
            left = Math.Min(left, numbers[i]); right = Math.Max(right, numbers[i]);
            top = Math.Min(top, numbers[i + 1]); bottom = Math.Max(bottom, numbers[i + 1]);
        }

        Assert.InRange(point.X, left, right);
        Assert.InRange(point.Y, top, bottom);
    }
}
