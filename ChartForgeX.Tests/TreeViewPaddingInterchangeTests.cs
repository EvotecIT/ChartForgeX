using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TreeViewPaddingInterchangeTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TreeConfiguredPaddingSurvivesSerializationAndLaterFrameOwnershipChanges(bool hostOwnsFrame) {
        var original = TreeViewBlock.Create().Title("Portable tree").Padding(31, 17, 47, 13)
            .Theme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).HostOwnsFrame(hostOwnsFrame)
            .AddItem("root", "Root/").AddItem("child", "Child.txt", "root").NaturalSize(520, 300);
        var envelope = VisualArtifact.Create("tree-padding", VisualArtifactKind.VisualBlock, original).ToInterchangeEnvelope();
        var imported = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTreeViewBlock();
        Assert.Equal(original.Options.HostOwnsFrame, imported.Options.HostOwnsFrame);
        Assert.Equal(original.RenderSvg("portable"), imported.RenderSvg("portable"));
        original.HostOwnsFrame(false); imported.HostOwnsFrame(false);
        Assert.Equal((31d, 17d, 47d, 13d), Padding(imported));
        original.NaturalSize(520, 300); imported.NaturalSize(520, 300);
        Assert.Equal(original.RenderSvg("portable"), imported.RenderSvg("portable")); Assert.Equal(original.ToPng(), imported.ToPng());
    }

    private static (double, double, double, double) Padding(IVisualBlock block) =>
        (block.Options.Padding.Left, block.Options.Padding.Top, block.Options.Padding.Right, block.Options.Padding.Bottom);
}
