using System.Text.RegularExpressions;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeTransitionExportFactsTests {
    [Theory, MemberData(nameof(NativeTransitionExportDefaultControls.Cases), MemberType = typeof(NativeTransitionExportDefaultControls))]
    public void SameEmissionCapturesExactPngModeAndIndependentlyResolvedAdaptivePrefixes(string scope, int width, bool dark, bool export) {
        var section = NativeTransitionExportDefaultControls.Section(scope, width, dark, export);
        NativeTransitionExportDefaultControls.Save(section, "same-emission-" + width + "-" + dark + "-" + export, new { scope, width, dark, export });
        var wide = Assert.IsType<VisualSvgEmission>(section.WideEmission); var narrow = Assert.IsType<VisualSvgEmission>(section.NarrowEmission);
        var embedded = Regex.Matches(section.Html, @"<svg[\s\S]*?</svg>").Select(match => match.Value).ToArray();
        Assert.Equal(2, embedded.Length); Assert.Equal(embedded[0], wide.Svg); Assert.Equal(embedded[1], narrow.Svg);
        Assert.Equal(VisualSvgOptions.NamespaceFromExternalId(scope), wide.Prefix);
        Assert.Equal(VisualSvgOptions.NamespaceFromExternalId(scope + "-narrow"), narrow.Prefix);
        if (scope.Contains(':')) Assert.NotEqual(wide.Prefix + "-narrow", narrow.Prefix);
        Assert.Equal(export, wide.CapturePngText); Assert.Equal(export, narrow.CapturePngText);
        var actual = export ? section.PreparedVisual.ForInteractivePngExport() : section.PreparedVisual;
        Assert.Equal(wide.Svg, actual.ToSvg(section.SvgOptions));
        var narrowOptions = new VisualSvgOptions(narrow.Prefix, section.SvgOptions.ColorVariables, section.SvgOptions.LinkTarget, section.SvgOptions.Responsive)
            .WithPatternTiles(section.SvgOptions.PatternTiles);
        var actualNarrow = export ? section.NarrowPreparedVisual!.ForInteractivePngExport() : section.NarrowPreparedVisual!;
        Assert.Equal(narrow.Svg, actualNarrow.ToSvg(narrowOptions));
    }
    [Fact]
    public void AutomaticNativeNamespaceIsReturnedFromTheSameOwnerWithoutScrapingOrSemanticIdChanges() {
        var section = NativeTransitionExportDefaultControls.Section("native-native-scope", 760, false, true);
        var normal = section.PreparedVisual.ExportSvgEmission(new VisualSvgOptions());
        Assert.Equal(section.PreparedVisual.ToSvg(new VisualSvgOptions()), normal.Svg);
        Assert.Contains(normal.Prefix + "-node-", normal.Svg, StringComparison.Ordinal);
        Assert.Contains("data-cfx-series-key=\"reviewed\"", normal.Svg, StringComparison.Ordinal);
        Assert.Contains("data-cfx-source-id=\"series-0", normal.Svg, StringComparison.Ordinal);
        Assert.False(normal.CapturePngText);
    }
}
