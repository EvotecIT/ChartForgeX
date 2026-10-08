using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2FrameSurfaceTests {
    [Fact]
    public void ShadowUsesAvailablePaddingWithoutMovingContentOrEscapingTheCanvas() {
        var plain = Build(0, 12, out var plainContent);
        var shadowed = Build(.22, 12, out var shadowContent);
        Assert.Equal(plainContent, shadowContent);
        var shadow = shadowed.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "frame-card-shadow").ToArray();
        Assert.NotEmpty(shadow);
        Assert.All(shadow, node => {
            Assert.True(node.Bounds.Left >= 0 && node.Bounds.Top >= 0 && node.Bounds.Right <= 160 && node.Bounds.Bottom <= 120);
            Assert.Equal(124, node.Fill!.Value.R);
            Assert.True(node.Fill.Value.A > 0);
        });
        var card = Assert.Single(shadowed.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "frame-card");
        Assert.True(card.Bounds.Left <= shadowContent.Left && card.Bounds.Top <= shadowContent.Top);
        Assert.Equal(7, card.Radius);
        var root = XDocument.Parse(VisualSceneSvgRenderer.Render(shadowed));
        Assert.Equal(shadow.Length, root.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "frame-card-shadow"));
        Assert.False(VisualSceneRasterRenderer.Render(plain).Pixels.SequenceEqual(VisualSceneRasterRenderer.Render(shadowed).Pixels));
    }

    [Fact]
    public void ZeroAuthoredPaddingKeepsCardCoordinatesAndReportsUnavailableShadowRoom() {
        var plain = Build(0, 0, out var plainContent);
        var shadowed = Build(.22, 0, out var shadowContent);
        Assert.Equal(plainContent, shadowContent);
        Assert.DoesNotContain(shadowed.Nodes, node => node.Role == "frame-card-shadow");
        Assert.Contains(shadowed.Diagnostics, diagnostic => diagnostic.Code == "frame.card-shadow-no-room");
        Assert.Equal(VisualSceneRasterRenderer.Render(plain).Pixels, VisualSceneRasterRenderer.Render(shadowed).Pixels);
    }

    private static VisualScene Build(double opacity, double padding, out ChartRect content) {
        var tokens = new VisualDesignTokens { Background = ChartColor.White, ElevatedSurface = ChartColor.White };
        var theme = new VisualTheme(tokens, tokens, cardRadius: 7, cardShadowOpacity: opacity,
            cardShadowColor: ChartColor.FromHex("#7C3AED"));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(160, 120), padding), theme,
            frame: new VisualFrame(showLegend: false, showSurface: false, showCard: true));
        var builder = new VisualSceneBuilder(context.Layout.Size, new FontSpec());
        content = VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
        return builder.Build();
    }
}
