using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2NumericRadialAxisPolicyTests {
    [Theory]
    [InlineData(ChartSeriesKind.RadialBar, "compact-axis-labels")]
    [InlineData(ChartSeriesKind.RadialColumn, "axis-labels")]
    [InlineData(ChartSeriesKind.RadialBar, "compact-all-labels")]
    [InlineData(ChartSeriesKind.RadialColumn, "all-labels")]
    public void ExecutableLabelPolicyExamplesUseTheSharedSvgAndNativePngSurface(ChartSeriesKind kind, string variant) {
        var chart = V2GalleryModels.Create(kind, variant);
        var compact = variant.StartsWith("compact", StringComparison.Ordinal);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(compact ? 360 : 800, compact ? 360 : 440)),
            theme: VisualTheme.Graphite(), themeMode: VisualThemeMode.Dark));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "radial-category-label");
        Assert.Equal(kind == ChartSeriesKind.RadialBar ? "radial-bar" : "radial-column", NumericRadialSeriesTests.Marks(prepared.Scene)[0].Role);
        Assert.Contains("<svg", prepared.ToSvg(), StringComparison.Ordinal); Assert.True(prepared.ToPng().Length > 64);
    }
}
