using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    public static IEnumerable<object[]> HierarchyCaptionPolicySelections() {
        foreach (var (kind, variant) in new[] { (ChartSeriesKind.Sunburst, "curved-labels"), (ChartSeriesKind.Treemap, "caption-policies") })
            foreach (var compact in new[] { false, true }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
                yield return new object[] { kind, compact ? "compact-" + variant : variant, mode };
    }

    [Theory]
    [MemberData(nameof(HierarchyCaptionPolicySelections))]
    public void HierarchyCaptionPoliciesHaveCompleteCompiledDownloadsAndNativeBothThemePoses(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        var selection = new ExampleFamilySelection(kind, variant);
        Assert.Contains(selection, ExampleRecipes.FamilySelections(kind));
        Assert.Contains(selection, ExampleRecipes.FamilySelections(kind, curated: true));
        Assert.Contains(selection, ExampleRecipes.StandaloneFamilySelections());
        var source = Source(selection.Recipe, variant);
        Assert.Contains("public static class " + selection.Recipe.Factory, source);
        Assert.Equal(1, source.Split("public static class ", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source);
        var chart = Assert.IsType<Chart>(selection.Recipe.Create(variant, mode));
        var width = variant.StartsWith("compact-", StringComparison.Ordinal) ? 420 : 1100;
        var prepared = chart.Prepare(HierarchyCaptionPolicyTests.Context(width, mode));
        ScatterBubbleColorRenderingTests.Capture(prepared, "recipe-" + kind + "-" + variant + "-" + mode);
        Assert.Equal(kind, Assert.Single(chart.Series).Kind);
        if (kind == ChartSeriesKind.Sunburst) {
            Assert.True(chart.Options.Sunburst.CurvedLabels);
            Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Metadata.ContainsKey("data-cfx-caption-placement"));
        } else {
            Assert.Equal(11, chart.Options.Treemap.MinimumLabelFontSize); Assert.Equal(TextTrimming.Ellipsis, chart.Options.Treemap.LabelTrimming);
            Assert.Contains(prepared.Scene.Nodes, node => node.Role == "treemap-group-label");
        }
        Assert.Equal(chart.Series[0].HierarchyItems.Count, prepared.Regions.Count(region => region.Role is "sunburst-segment" or "treemap-tile" or "treemap-group"));
        Assert.NotEmpty(prepared.ToSvg()); Assert.NotEmpty(prepared.ToPng());
    }
}
