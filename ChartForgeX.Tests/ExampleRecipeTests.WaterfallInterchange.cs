using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    public static IEnumerable<object[]> PortableWaterfallSelections() {
        foreach (var variant in new[] { "portable", "compact-portable", "portable-horizontal", "compact-portable-horizontal" })
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) yield return new object[] { variant, mode };
    }

    [Theory]
    [MemberData(nameof(PortableWaterfallSelections))]
    public void PortableWaterfallRecipesExecuteTheSaveLoadEditWorkflowAndRetainStandaloneDeclarations(string variant, VisualThemeMode mode) {
        var selection = new ExampleFamilySelection(ChartSeriesKind.Waterfall, variant);
        Assert.Contains(selection, ExampleRecipes.FamilySelections(ChartSeriesKind.Waterfall));
        Assert.Contains(selection, ExampleRecipes.FamilySelections(ChartSeriesKind.Waterfall, curated: true));
        Assert.Contains(selection, ExampleRecipes.StandaloneFamilySelections());
        var source = Source(selection.Recipe, variant); Assert.DoesNotContain("{{", source); Assert.DoesNotContain("ChartForgeX.Examples", source);
        var chart = Assert.IsType<Chart>(selection.Recipe.Create(variant, mode)); var series = Assert.Single(chart.Series);
        Assert.True(series.IsTypedWaterfall); Assert.Equal(7, series.WaterfallItems.Count); Assert.Equal(-125d, series.WaterfallItems[3].Value!.Value);
        Assert.Equal("cost-change", series.ObservationIds[3].Value); Assert.Equal("cost-checkpoint", series.ObservationIds[4].Value);
        Assert.Equal(35, ChartWaterfallSteps.Create(series)[^1].Value);
        var compact = variant.StartsWith("compact-", StringComparison.Ordinal); var width = compact ? 390 : 900;
        chart.WithSize(width, 440).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var svg = prepared.ToSvg("portable-waterfall"); var png = prepared.ToPng();
        var document = XDocument.Parse(svg); var image = RasterImageDecoder.Decode(png);
        Assert.Equal(width, image.Width); Assert.Equal(440, image.Height);
        Assert.Equal(4, document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "point"));
        Assert.Equal(2, document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "waterfall-subtotal"));
        Assert.Single(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "waterfall-total");
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "recipe-waterfall-" + variant + "-" + mode);
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png);
        }
        Assert.Equal(VisualArtifactInterchangeFamily.Waterfall, chart.ToVisualArtifact().ToInterchangeEnvelope().Family);
    }
}
