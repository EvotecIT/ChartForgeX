using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    [Fact]
    public void StandaloneSelectionsRetainOrdinaryOptionsAndEveryAdvertisedFamilyVariant() {
        var standalone = ExampleRecipes.StandaloneFamilySelections().ToArray();
        var selections = standalone.ToHashSet();
        Assert.Equal(standalone.Length, selections.Count);
        foreach (var kind in Enum.GetValues<ChartSeriesKind>()) {
            foreach (var variant in new[] { "wide", "compact", "sparse", "options", "compact-options" })
                Assert.Contains(new ExampleFamilySelection(kind, variant), selections);
            foreach (var curated in new[] { false, true })
                foreach (var advertised in ExampleRecipes.FamilySelections(kind, curated))
                    Assert.Contains(advertised, selections);
        }
        foreach (var option in ExampleRecipes.OptionVariants) {
            Assert.Contains(new ExampleFamilySelection(option.Key.Kind, option.Key.Variant), selections);
            Assert.Contains(new ExampleFamilySelection(option.Key.Kind, "compact-" + option.Key.Variant), selections);
        }
    }

    [Fact]
    public void AdvertisedVariantsCreateTheirDeclaredFamilyIncludingDenseAndScalarForms() {
        foreach (var kind in Enum.GetValues<ChartSeriesKind>()) {
            foreach (var selection in ExampleRecipes.FamilySelections(kind)) {
                var chart = Assert.IsType<Chart>(selection.Recipe.Create(selection.Variant));
                Assert.Contains(chart.Series, series => series.Kind == selection.Kind);
            }
        }
    }

    [Theory]
    [InlineData("license-cost", "wide", VisualThemeMode.Light, 760, 460)]
    [InlineData("dashboard-premium-trend", "wide", VisualThemeMode.Light, 920, 360)]
    [InlineData("dashboard-premium-trend", "snippet", VisualThemeMode.Light, 840, 360)]
    [InlineData("control-coverage-heatmap", "wide", VisualThemeMode.Dark, 980, 560)]
    [InlineData("travel-dotted-map", "wide", VisualThemeMode.Dark, 980, 560)]
    public void ConfiguredLegacyRecipesRetainNativeViewportAndExportDimensions(string name, string variant, VisualThemeMode mode, int width, int height) {
        var chart = Assert.IsType<Chart>(ExampleRecipes.Named(name).Create(variant, mode));
        Assert.Equal(width, chart.Options.Size.Width);
        Assert.Equal(height, chart.Options.Size.Height);
        Assert.Equal(mode, chart.Options.ThemeMode);
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Equal(width, (int?)svg.Root!.Attribute("width"));
        Assert.Equal(height, (int?)svg.Root.Attribute("height"));
        var png = chart.ToPng();
        Assert.Equal(width, png[16] << 24 | png[17] << 16 | png[18] << 8 | png[19]);
        Assert.Equal(height, png[20] << 24 | png[21] << 16 | png[22] << 8 | png[23]);
    }

    [Fact]
    public void ConfiguredLegacyRecipesKeepCurrencyPercentagesTrendValuesAndNamedCoordinates() {
        var cost = LicenseCostRecipe.Create();
        Assert.Equal("License Cost Trend", cost.Title);
        Assert.Equal(new[] { 820000d, 970000, 1010000, 1210000 }, cost.Series[0].Points.Select(point => point.Y));
        Assert.Equal(new[] { 860000d, 940000, 1050000, 1160000 }, cost.Series[1].Points.Select(point => point.Y));
        Assert.Equal("$820,000", cost.Options.ValueFormatter!(820000));

        var coverage = ControlCoverageRecipe.Create();
        Assert.Equal(ChartHeatmapScale.Semantic, coverage.Options.HeatmapScale);
        Assert.Equal(new[] { "Primary domains", "Parked domains", "Regional domains", "Acquired domains" }, coverage.Series.Select(series => series.Name));
        Assert.Equal(new[] { 96d, 88, 74, 63, 58, 92 }, coverage.Series[0].Points.Select(point => point.Y));
        Assert.Equal("96%", coverage.Options.ValueFormatter!(96));

        var trend = DashboardPremiumTrendRecipe.Create();
        var snippet = DashboardPremiumTrendRecipe.Create("snippet");
        Assert.False(trend.Options.ShowHeader);
        Assert.False(snippet.Options.ShowLegend);
        Assert.Equal(new[] { 24d, 36, 48, 58, 46, 68, 76 }, trend.Series[0].Points.Select(point => point.Y));
        Assert.Equal(trend.Series.SelectMany(series => series.Points).Select(point => point.Y),
            snippet.Series.SelectMany(series => series.Points).Select(point => point.Y));

        var map = TravelDottedMapRecipe.Create();
        var locations = Assert.Single(map.Series).MapPoints;
        Assert.Equal(new[] { "Indonesia", "Spain", "United States", "Norway" }, locations.Select(point => point.Label));
        Assert.Equal(113.9213, locations[0].Longitude);
        Assert.Equal(-0.7893, locations[0].Latitude);
        Assert.Equal(2, map.Options.MapConnectors.Count);
    }
}
