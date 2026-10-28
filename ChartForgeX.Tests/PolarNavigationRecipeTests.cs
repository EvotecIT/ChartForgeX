using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>The real host recipe preserves independently captured windows for partial source requests.</summary>
public sealed class PolarNavigationRecipeTests {
    [Theory]
    [InlineData("angle", false)] [InlineData("angle", true)]
    [InlineData("radius", false)] [InlineData("radius", true)]
    [InlineData("secondaryRadius", false)] [InlineData("secondaryRadius", true)]
    public void OneCoordinateWindowRequestRetainsEveryUnselectedSourceDomain(string coordinate, bool fullSelectedDomain) {
        var chart = PolarNavigationTests.Model(); var initial = PolarNavigationTests.Window();
        var basis = PolarNavigationRecipe.PrepareSection(chart, initial);
        var originalPoints = chart.Series.Select(series => series.Points.ToArray()).ToArray();
        var selected = coordinate switch {
            "angle" => fullSelectedDomain ? new ChartAxisWindow(0, Math.PI * 2) : new ChartAxisWindow(5.8, 6.6),
            "radius" => fullSelectedDomain ? new ChartAxisWindow(0, 100) : new ChartAxisWindow(30, 70),
            _ => fullSelectedDomain ? new ChartAxisWindow(1, 1000) : new ChartAxisWindow(20, 80)
        };
        var expectedView = new ChartPolarViewport(coordinate == "angle" ? selected : initial.Angle,
            coordinate == "radius" ? selected : initial.Radius, coordinate == "secondaryRadius" ? selected : initial.SecondaryRadius);
        var expected = PolarNavigationRecipe.PrepareSection(chart, expectedView);
        foreach (var drawing in new[] { "wide", "narrow" }) {
            using var request = JsonDocument.Parse(JsonSerializer.Serialize(new {
                coordinateSystem = "polar", kind = "window", axes = new[] { coordinate }, drawing,
                sourceViewport = new Dictionary<string, object> {
                    [coordinate] = new { minimum = selected.Minimum, maximum = selected.Maximum }
                }
            }));
            var actual = PolarNavigationRecipe.PrepareReply(chart, basis, request.RootElement);
            foreach (var narrow in new[] { false, true }) {
                var current = Drawing(actual, narrow); var before = Drawing(basis, narrow).PolarProjection!;
                var projection = current.PolarProjection!;
                var retainedAngle = coordinate == "angle" ? selected : before.AngleWindow;
                AssertWindow(retainedAngle, projection.AngleWindow);
                foreach (var side in new[] { ChartAxisSide.Primary, ChartAxisSide.Secondary }) {
                    var name = side == ChartAxisSide.Primary ? "radius" : "secondaryRadius";
                    var retained = name == coordinate ? selected : before.GetRadiusWindow(side);
                    AssertWindow(retained, projection.GetRadiusWindow(side));
                }
                Assert.Equal(SourceFacts(Drawing(basis, narrow)), SourceFacts(current));
                var svg = current.ToSvg(); var png = current.ToPng();
                Assert.Equal(Drawing(expected, narrow).ToSvg(), svg); Assert.Equal(Drawing(expected, narrow).ToPng(), png);
                if (drawing == "wide") PolarViewportContractTests.Capture("polar-host-partial-window-" + coordinate + "-" + fullSelectedDomain + "-" + narrow, svg, png);
            }
        }
        AssertWindow(initial.Angle!, basis.PreparedVisual.PolarProjection!.AngleWindow);
        AssertWindow(initial.Radius!, basis.PreparedVisual.PolarProjection.GetRadiusWindow());
        AssertWindow(initial.SecondaryRadius!, basis.PreparedVisual.PolarProjection.GetRadiusWindow(ChartAxisSide.Secondary));
        for (var index = 0; index < originalPoints.Length; index++) Assert.Equal(originalPoints[index], chart.Series[index].Points);
    }
    private static PreparedVisual Drawing(HtmlChartSection section, bool narrow) => narrow ? section.NarrowPreparedVisual! : section.PreparedVisual;
    private static void AssertWindow(ChartAxisWindow expected, ChartAxisWindow actual) {
        Assert.Equal(expected.Minimum, actual.Minimum); Assert.Equal(expected.Maximum, actual.Maximum);
        Assert.Equal(expected.MaximumInclusive, actual.MaximumInclusive);
    }
    private static string[] SourceFacts(PreparedVisual prepared) => PolarViewportContractTests.Sources(prepared).Select(group => string.Join("|",
        new[] { "data-cfx-series", "data-cfx-point", "data-cfx-observation-id", "data-cfx-angle", "data-cfx-value", "data-cfx-break-before", "data-cfx-axis" }
            .Select(key => key + "=" + (group.Metadata.TryGetValue(key, out var value) ? value : string.Empty)))).ToArray();
}
