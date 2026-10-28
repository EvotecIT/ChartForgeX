using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    public static IEnumerable<object[]> PieDonutDepthSelections() {
        foreach (var kind in new[] { ChartSeriesKind.Pie, ChartSeriesKind.Donut })
            foreach (var variant in kind == ChartSeriesKind.Pie ? new[] { "variable-radius", "compact-variable-radius" }
                : new[] { "variable-radius", "compact-variable-radius", "automatic-rings", "compact-automatic-rings" })
                foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) yield return new object[] { kind, variant, mode };
    }

    [Theory]
    [MemberData(nameof(PieDonutDepthSelections))]
    public void PieDonutDepthDownloadsRetainTheirAuthoredRadiiAndCompleteSourceOrderedRings(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        var selection = new ExampleFamilySelection(kind, variant);
        Assert.Contains(selection, ExampleRecipes.FamilySelections(kind)); Assert.Contains(selection, ExampleRecipes.FamilySelections(kind, curated: true));
        Assert.Contains(selection, ExampleRecipes.StandaloneFamilySelections());
        var source = Source(selection.Recipe, variant); Assert.DoesNotContain("{{", source); Assert.DoesNotContain("ChartForgeX.Examples", source);
        Assert.DoesNotContain("Interactivity.Html", source); Assert.Contains("WithPointSliceRadiusRatio", source);
        var chart = Assert.IsType<Chart>(selection.Recipe.Create(variant, mode)); var automatic = variant.EndsWith("automatic-rings", StringComparison.Ordinal);
        Assert.Equal(automatic ? new[] { 100d, 150, 220 } : new[] { 160d }, chart.Series.Select(series => series.Points.Sum(point => point.Y)));
        Assert.All(chart.Series, series => { Assert.Equal(kind, series.Kind); Assert.Null(series.RadialBand); Assert.Equal(3, series.ObservationIds.Count); });
        Assert.Equal(new double?[] { .35, .7, 1 }, chart.Series[^1].PointSliceRadiusRatios);
        Assert.Equal(automatic, chart.Options.RadialBandAllocation != null);
        var width = variant.StartsWith("compact-", StringComparison.Ordinal) ? 390 : 900;
        chart.WithSize(width, automatic ? 600 : 480).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal(automatic ? 9 : 3, prepared.Scene.Nodes.OfType<VisualSceneSlice>().Count());
        for (var index = 0; index < chart.Series.Count; index++) {
            var mark = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), slice => slice.Id == "series-" + index + "-point-0");
            Assert.Equal(Math.PI * 2 * chart.Series[index].Points[0].Y / chart.Series[index].Points.Sum(point => point.Y), mark.Sweep, 9);
        }
        Assert.Equal(width, RasterImageDecoder.Decode(png).Width);
        var document = XDocument.Parse(svg); Assert.DoesNotContain(document.Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
        var artifact = chart.ToVisualArtifact(); var envelope = artifact.ToInterchangeEnvelope(); Assert.Equal(VisualArtifactInterchangeFamily.None, envelope.Family);
        for (var index = 0; index < chart.Series.Count; index++) {
            using var facts = JsonDocument.Parse(envelope.Extensions["chart.series." + index + ".radial.sourceFacts"]);
            Assert.Equal(3, facts.RootElement.GetArrayLength());
            Assert.Equal("subscriptions", facts.RootElement[0].GetProperty("observationId").GetString());
        }
        Assert.DoesNotContain("<script", artifact.ToHtmlPage(), StringComparison.OrdinalIgnoreCase);
        NumericRadialEncodedGeometryTests.Capture(chart, "recipe-" + kind + "-" + variant + "-" + mode);
    }
}
