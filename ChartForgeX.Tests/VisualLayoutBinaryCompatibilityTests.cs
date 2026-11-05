using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualLayoutBinaryCompatibilityTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoArgumentConstructor_PublishedSignature_PreservesFixedViewport(bool independentEdges) {
        var size = new VisualSize(640, 400);
        var padding = independentEdges ? new ChartPadding(12, 16, 20, 24) : ChartPadding.All(18);
        var paddingType = independentEdges ? typeof(ChartPadding) : typeof(double);
        var constructor = typeof(VisualLayoutOptions).GetConstructor(new[] { typeof(VisualSize), paddingType });
        Assert.NotNull(constructor);
        var layout = Assert.IsType<VisualLayoutOptions>(constructor.Invoke(new object[] { size, independentEdges ? (object)padding : 18D }));

        Assert.Equal(640, layout.Size.Width);
        Assert.Equal(400, layout.Size.Height);
        Assert.Equal(padding.Left, layout.PaddingEdges.Left);
        Assert.Equal(padding.Top, layout.PaddingEdges.Top);
        Assert.Equal(padding.Right, layout.PaddingEdges.Right);
        Assert.Equal(padding.Bottom, layout.PaddingEdges.Bottom);
        Assert.False(layout.FitHeightToContent);
        Assert.True(new VisualLayoutOptions(size, padding, fitHeightToContent: true).FitHeightToContent);
        Assert.True(new VisualLayoutOptions(size, fitHeightToContent: true).FitHeightToContent);
        Assert.Equal(24, new VisualLayoutOptions(size).Padding);
    }
}
