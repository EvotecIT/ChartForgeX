using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;
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

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void NativeDiagramCardUsesTheSharedRadiusWithoutChangingOpaqueCanvasOrSquareCardSemantics(double radius) {
        var shared = Build(0, 12, out _, radius);
        var sharedCard = Assert.Single(shared.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "frame-card");
        var theme = VisualTheme.Graphite().Configure(builder => builder.CardRadius = radius);
        var packet = PacketLayoutBlock.Create().WithSize(160, 120).WithTheme(theme).WithBitsPerRow(8).AddField(0, 7, "Payload");
        foreach (var transparent in new[] { false, true }) {
            packet.WithTransparentBackground(transparent);
            var svg = XDocument.Parse(packet.ToSvg());
            var card = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "visual-card");
            Assert.Equal(sharedCard.Radius, double.Parse(card.Attribute("rx")!.Value, CultureInfo.InvariantCulture));
            Assert.Equal(sharedCard.Bounds.X, double.Parse(card.Attribute("x")!.Value, CultureInfo.InvariantCulture));
            Assert.Equal(sharedCard.Bounds.Y, double.Parse(card.Attribute("y")!.Value, CultureInfo.InvariantCulture));
            Assert.Equal(sharedCard.Bounds.Width, double.Parse(card.Attribute("width")!.Value, CultureInfo.InvariantCulture));
            Assert.Equal(sharedCard.Bounds.Height, double.Parse(card.Attribute("height")!.Value, CultureInfo.InvariantCulture));
            Assert.Equal(!transparent, svg.Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "visual-background"));
            var image = RasterImageDecoder.Decode(packet.ToPng());
            Assert.Equal(transparent && radius == 3 ? 0 : 255, image.Pixels[3]);
            Assert.Equal(255, image.Pixels[(image.Height / 2 * image.Width + image.Width / 2) * 4 + 3]);
        }
        packet.WithTransparentBackground().WithCard(false);
        Assert.DoesNotContain(XDocument.Parse(packet.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "visual-card");
        Assert.Equal(0, RasterImageDecoder.Decode(packet.ToPng()).Pixels[3]);
    }

    private static VisualScene Build(double opacity, double padding, out ChartRect content, double radius = 7) {
        var tokens = new VisualDesignTokens { Background = ChartColor.White, ElevatedSurface = ChartColor.White };
        var theme = new VisualTheme(tokens, tokens, cardRadius: radius, cardShadowOpacity: opacity,
            cardShadowColor: ChartColor.FromHex("#7C3AED"));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(160, 120), padding), theme,
            frame: new VisualFrame(showLegend: false, showSurface: false, showCard: true));
        var builder = new VisualSceneBuilder(context.Layout.Size, new FontSpec());
        content = VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
        return builder.Build();
    }
}
