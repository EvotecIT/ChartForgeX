using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the downloadable public-package recipe boundary and the data retained during extraction.</summary>
public sealed class ExampleRecipeTests {
    [Theory]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.HorizontalBar)]
    [InlineData(ChartSeriesKind.Lollipop)]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    [InlineData(ChartSeriesKind.PolarArea)]
    [InlineData(ChartSeriesKind.ProgressRing)]
    [InlineData(ChartSeriesKind.Radar)]
    public void SparseIndependentMarksOmitCategoriesAndRetainTheirValues(ChartSeriesKind kind) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var chart = Assert.IsType<Chart>(ExampleRecipes.ForKind(kind, "sparse").Create("sparse", mode));
            var source = chart.Series.First();
            Assert.Equal(new[] { 1d, 3, 6 }, source.Points.Select(point => point.X));
            Assert.Equal(new[] { 27d, 41, 33 }, source.Points.Select(point => point.Y));
            Assert.All(chart.Series.SelectMany(series => series.Points), point => Assert.False(point.BreakBefore));
            var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440)), themeMode: mode));
            Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group =>
                group.Metadata.TryGetValue("data-cfx-series-key", out var id) && id == source.Id.Value);
            Assert.Equal(3, source.SourcePointCount);
        }
    }

    [Fact]
    public void EveryFamilyDownloadContainsOnlyItsCompiledFactoryAndDefaultFontPath() {
        Assert.Equal(Enum.GetValues<ChartSeriesKind>().Length, ExampleRecipes.ChartFamilies.Count());
        foreach (var entry in ExampleRecipes.ChartFamilies) {
            var source = Source(entry.Value);
            Assert.Contains("var model = " + entry.Value.Factory + ".Create(\"wide\", mode);", source);
            Assert.Contains("public static class " + entry.Value.Factory, source);
            Assert.Equal(1, source.Split("public static class ", StringSplitOptions.None).Length - 1);
            Assert.DoesNotContain("V2GalleryModels", source);
            Assert.DoesNotContain("ChartForgeX.Examples", source);
            Assert.DoesNotContain("FontRegistry.Register", source);
            Assert.DoesNotContain("{{", source);
            var chart = Assert.IsType<Chart>(entry.Value.Create());
            Assert.Contains(chart.Series, series => series.Kind == entry.Key);
        }
    }

    [Fact]
    public void DeepColorOptionsUseFocusedSourcesAndPreserveIndependentMeasurements() {
        var ordinary = ExampleRecipes.ForKind(ChartSeriesKind.Scatter);
        var option = ExampleRecipes.ForKind(ChartSeriesKind.Scatter, "color-dimension");
        Assert.NotEqual(ordinary.Factory, option.Factory);
        var source = Source(option, "color-dimension");
        Assert.Contains("public static class ScatterColorDimensionRecipe", source);
        Assert.DoesNotContain("public static class ScatterRecipe", source);
        var chart = Assert.IsType<Chart>(option.Create("color-dimension"));
        var observations = chart.Series.Single(series => series.Name == "Recorded batches");
        Assert.Equal(new[] { 18d, 30, 24, 44, 32 }, observations.Points.Select(point => point.Y));
        Assert.Equal("Temperature", observations.ColorDimension.Name);
        Assert.Equal("pl-PL", observations.ColorDimension.ValueFormat!.Culture.Name);
        Assert.Equal("Processing time (minutes)", chart.YAxisTitle);
    }

    [Theory]
    [InlineData(ChartSeriesKind.RegionMap, false, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.RegionMap, true, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.RegionMap, false, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.RegionMap, true, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.TileMap, false, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.TileMap, true, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.TileMap, false, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.TileMap, true, VisualThemeMode.Dark)]
    public void MapColorRecipesKeepSignedTemperaturesZeroAndMissingRegions(ChartSeriesKind kind, bool compact, VisualThemeMode mode) {
        var variant = compact ? "compact-color-dimension" : "color-dimension";
        var recipe = ExampleRecipes.ForKind(kind, variant);
        var chart = Assert.IsType<Chart>(recipe.Create(variant, mode)).WithSize(compact ? 360 : 800, compact ? 360 : 440)
            .WithTheme(VisualTheme.Graphite(), mode).WithTitle("Temperature by state")
            .WithSubtitle("Temperature captions use their own format and culture");
        Assert.Equal("Temperature", chart.Options.MapColorDimension.Name);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg());
        var role = kind == ChartSeriesKind.RegionMap ? "region-map-region-source" : "tile-map-region-source";
        var sources = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role)
            .ToDictionary(element => element.Attribute("data-cfx-region")!.Value);
        Assert.Equal("-25", sources["CA"].Attribute("data-cfx-value")!.Value);
        Assert.Equal("-25,0 °C", sources["CA"].Attribute("data-cfx-formatted-value")!.Value);
        Assert.Equal("0", sources["NY"].Attribute("data-cfx-value")!.Value);
        Assert.Equal("false", sources["NY"].Attribute("data-cfx-empty")!.Value);
        Assert.Equal("55", sources["CO"].Attribute("data-cfx-value")!.Value);
        Assert.Equal("true", sources["AK"].Attribute("data-cfx-empty")!.Value);
        Assert.Equal("Not sampled", sources["AK"].Attribute("data-cfx-formatted-value")!.Value);
        Assert.Equal("state-temperatures", sources["CA"].Attribute("data-cfx-series-key")!.Value);
        ScatterBubbleColorRenderingTests.Capture(prepared, "signed-map-" + kind.ToString().ToLowerInvariant()
            + (compact ? "-compact-" : "-wide-") + mode.ToString().ToLowerInvariant());
    }

    [Fact]
    public void GalleryTypographyIsAnExplicitCompleteAlternative() {
        var recipe = ExampleRecipes.ForKind(ChartSeriesKind.Bar);
        var ordinary = Source(recipe);
        var reproduction = ExampleRecipeSource.Emit(recipe, "wide", "Orders", "North and South", VisualThemeMode.Dark,
            360, 360, galleryTypography: true);
        Assert.DoesNotContain("fonts/", ordinary);
        Assert.Contains("FontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Regular.ttf\", 400);", reproduction);
        Assert.Contains("FontSpec.FromFamily(\"CFX Proof Carlito\")", reproduction);
        Assert.Contains("var mode = VisualThemeMode.Dark;", reproduction);
        Assert.Contains("new VisualSize(360, 360)", reproduction);
        Assert.Contains("public static class BarRecipe", reproduction);
    }

    [Fact]
    public void TypedIntervalsAndChartOwnedScheduleDependenciesKeepTheirFacts() {
        var range = RangeBarRecipe.Create("horizontal-endpoint-labels");
        Assert.Equal(ChartOrientation.Horizontal, range.Options.RangeBar.Orientation);
        Assert.Contains(range.Series.SelectMany(series => series.Intervals), interval => interval.Start > interval.End);
        var gantt = GanttRecipe.Create();
        Assert.Equal(new[] { "discovery", "implementation", "validation", "delivery" }, gantt.Series.Select(series => series.Id.Value));
        Assert.Equal(3, gantt.GanttDependencies.Count);
        var prepared = gantt.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440))));
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(svg.Descendants(), element => element.Value.Contains("Discovery", StringComparison.Ordinal));
        Assert.Equal(800, (int?)svg.Root!.Attribute("width"));
        var png = prepared.ToPng();
        Assert.Equal(800, png[16] << 24 | png[17] << 16 | png[18] << 8 | png[19]);
        Assert.Equal(440, png[20] << 24 | png[21] << 16 | png[22] << 8 | png[23]);
    }

    [Fact]
    public void ViewportProgramsUseThePublicPreparationWindowWithoutDroppingOutsideObservations() {
        var recipe = ExampleRecipes.Named("line-viewport");
        var chart = Assert.IsType<Chart>(recipe.Create("x-viewport"));
        var observed = chart.Series.Single(series => series.Id.Value == "observed");
        Assert.Contains(observed.Points, point => point.X == 1.99);
        Assert.Contains(observed.Points, point => point.X == 8.01);
        Assert.Contains(observed.Points, point => point.BreakBefore);
        var source = ExampleRecipeSource.Emit(recipe, "x-viewport", "Requests", "X 2–8", VisualThemeMode.Light, 800, 440, viewport: true);
        Assert.Contains("model.Prepare(context, new ChartCartesianViewport(x: new ChartXViewport(2, 8)))", source);
        Assert.DoesNotContain("ViewportExample", source);
    }

    private static string Source(ExampleRecipe recipe, string variant = "wide") =>
        ExampleRecipeSource.Emit(recipe, variant, "Orders", "North and South", VisualThemeMode.Light, 800, 440);
}
