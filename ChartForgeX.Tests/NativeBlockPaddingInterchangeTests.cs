using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeBlockPaddingInterchangeTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RailroadConfiguredPaddingSurvivesSerializationAndLaterFrameOwnershipChanges(bool hostOwnsFrame) {
        var original = RailroadDiagramBlock.Create().WithTitle("Portable grammar").WithPadding(31, 17, 47, 13)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithHostFrame(hostOwnsFrame)
            .AddRule("root", RailroadExpression.Terminal("Literal")).WithNaturalSize();
        var envelope = VisualArtifact.Create("railroad-padding", VisualArtifactKind.VisualBlock, original).ToInterchangeEnvelope();
        var imported = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToRailroadDiagramBlock();
        Assert.Equal(original.Options.HostOwnsFrame, imported.Options.HostOwnsFrame);
        Assert.Equal(original.RenderSvg("portable"), imported.RenderSvg("portable"));
        original.WithHostFrame(false); imported.WithHostFrame(false);
        Assert.Equal((31d, 17d, 47d, 13d), Padding(imported));
        original.WithNaturalSize(); imported.WithNaturalSize();
        Assert.Equal(original.RenderSvg("portable"), imported.RenderSvg("portable")); Assert.Equal(original.ToPng(), imported.ToPng());
        original.WithHostFrame(true); imported.WithHostFrame(true);
        Assert.Equal((0d, 0d, 0d, 0d), Padding(imported));
    }

    private static (double, double, double, double) Padding(IVisualBlock block) =>
        (block.Options.Padding.Left, block.Options.Padding.Top, block.Options.Padding.Right, block.Options.Padding.Bottom);
}
