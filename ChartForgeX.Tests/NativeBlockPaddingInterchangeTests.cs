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
        var original = RailroadDiagramBlock.Create().Title("Portable grammar").Padding(31, 17, 47, 13)
            .Theme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).HostOwnsFrame(hostOwnsFrame)
            .AddRule("root", RailroadExpression.Terminal("Literal")).NaturalSize();
        var envelope = VisualArtifact.Create("railroad-padding", VisualArtifactKind.VisualBlock, original).ToInterchangeEnvelope();
        var imported = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToRailroadDiagramBlock();
        Assert.Equal(original.Options.HostOwnsFrame, imported.Options.HostOwnsFrame);
        Assert.Equal(original.RenderSvg("portable"), imported.RenderSvg("portable"));
        original.HostOwnsFrame(false); imported.HostOwnsFrame(false);
        Assert.Equal((31d, 17d, 47d, 13d), Padding(imported));
        original.NaturalSize(); imported.NaturalSize();
        Assert.Equal(original.RenderSvg("portable"), imported.RenderSvg("portable")); Assert.Equal(original.ToPng(), imported.ToPng());
        original.HostOwnsFrame(); imported.HostOwnsFrame();
        Assert.Equal((0d, 0d, 0d, 0d), Padding(imported));
    }

    private static (double, double, double, double) Padding(IVisualBlock block) =>
        (block.Options.Padding.Left, block.Options.Padding.Top, block.Options.Padding.Right, block.Options.Padding.Bottom);
}
