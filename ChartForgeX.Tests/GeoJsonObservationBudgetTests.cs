using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GeoJsonObservationBudgetTests {
    [Fact]
    public void MultipartNamesAreBoundedBeforeExpansionAndBudgetsAccumulateAcrossFeatures() {
        var positions = string.Join(",", Enumerable.Repeat("[0,0]", 2000));
        var large = Feature("MultiPoint", "[" + positions + "]", new string('n', 4096));
        _ = ChartMapGeoJson.ToMapPoints(Feature("Point", "[0,0]", "Warm"));
        RejectBeforeExpansion(() => ChartMapGeoJson.ToMapPoints(large));
        var pair = Feature("MultiPoint", "[[0,0],[1,1]]", "Ports");
        var collection = "{\"type\":\"FeatureCollection\",\"features\":[" + pair + "," + pair + "]}";
        var exact = new ChartMapGeoJsonOptions { MaximumObservations = 4, MaximumExpandedLabelCharacters = 36 };
        Assert.Equal(new[] { "Ports [1]", "Ports [2]", "Ports [1]", "Ports [2]" }, ChartMapGeoJson.ToMapPoints(collection, exact).Select(p => p.Label));
        exact.MaximumExpandedLabelCharacters = 35;
        Assert.Contains("MaximumExpandedLabelCharacters", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapPoints(collection, exact)).Message);
        exact.MaximumExpandedLabelCharacters = 36; exact.MaximumObservations = 3;
        Assert.Contains("MaximumObservations", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapPoints(collection, exact)).Message);
    }

    [Fact]
    public void RouteBudgetIncludesConnectorAndEveryWaypointLabelAcrossMultipartLines() {
        var input = Feature("MultiLineString", "[[[0,0],[1,1]],[[2,2],[3,3]]]", "R");
        // Two five-character route labels and four nine-character waypoint labels.
        var options = new ChartMapGeoJsonOptions { MaximumObservations = 4, MaximumExpandedLabelCharacters = 46 };
        var routes = ChartMapGeoJson.ToMapConnectors(input, options);
        Assert.Equal(new[] { "R [1]", "R [2]" }, routes.Select(r => r.Label));
        Assert.Equal(new[] { "R [1] [1]", "R [1] [2]", "R [2] [1]", "R [2] [2]" }, routes.SelectMany(r => r.RoutePoints).Select(p => p.Label));
        options.MaximumExpandedLabelCharacters = 45;
        Assert.Contains("MaximumExpandedLabelCharacters", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapConnectors(input, options)).Message);
        options.MaximumExpandedLabelCharacters = 46; options.MaximumObservations = 3;
        Assert.Contains("MaximumObservations", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapConnectors(input, options)).Message);
        var longLine = Feature("LineString", "[" + string.Join(",", Enumerable.Repeat("[0,0]", 2000)) + "]", new string('n', 4096));
        _ = ChartMapGeoJson.ToMapConnectors(Feature("LineString", "[[0,0],[1,1]]", "Warm"));
        RejectBeforeExpansion(() => ChartMapGeoJson.ToMapConnectors(longLine));
    }

    [Fact]
    public void CoordinateFiltersSkipExpansionButDoNotHideInvalidGeometryAndLimitsArePositive() {
        var options = new ChartMapGeoJsonOptions { MaximumObservations = 1, MaximumExpandedLabelCharacters = 1 };
        options.WithCoordinateBounds(10, 20, 10, 20);
        Assert.Empty(ChartMapGeoJson.ToMapPoints(Feature("MultiPoint", "[[0,0],[1,1]]", "Outside"), options));
        Assert.Empty(ChartMapGeoJson.ToMapConnectors(Feature("LineString", "[[0,0],[1,1]]", "Outside"), options));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartMapGeoJson.ToMapPoints(Feature("Point", "[181,0]", "Outside"), options));
        Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapConnectors(Feature("LineString", "[[0,0]]", "Outside"), options));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaximumObservations = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaximumExpandedLabelCharacters = -1);
    }

    private static string Feature(string kind, string coordinates, string name) =>
        "{\"type\":\"Feature\",\"properties\":{\"name\":\"" + name + "\"},\"geometry\":{\"type\":\"" + kind + "\",\"coordinates\":" + coordinates + "}}";

    private static void RejectBeforeExpansion(Func<object> import) {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var error = Record.Exception(() => { _ = import(); });
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Contains("MaximumExpandedLabelCharacters", Assert.IsType<ArgumentException>(error).Message);
        // Copying this fixture's name into 2,000 labels needs over 16 MiB before accounting for parsing.
        // The 4 MiB ceiling allows parser overhead but catches a budget check moved after expansion.
        Assert.InRange(allocated, 0, 4 * 1024 * 1024);
    }
}
