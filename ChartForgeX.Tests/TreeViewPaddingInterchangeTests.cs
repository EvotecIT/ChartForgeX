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
        var original = TreeViewBlock.Create().WithTitle("Portable tree").WithPadding(31, 17, 47, 13)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithHostFrame(hostOwnsFrame)
            .AddItem("root", "Root/").AddItem("child", "Child.txt", "root").WithNaturalSize(520, 300);
        var envelope = VisualArtifact.Create("tree-padding", VisualArtifactKind.VisualBlock, original).ToInterchangeEnvelope();
        var imported = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTreeViewBlock();
        Assert.Equal(original.Options.HostOwnsFrame, imported.Options.HostOwnsFrame);
        Assert.Equal(original.RenderSvg("portable"), imported.RenderSvg("portable"));
        original.WithHostFrame(false); imported.WithHostFrame(false);
        Assert.Equal((31d, 17d, 47d, 13d), Padding(imported));
        original.WithNaturalSize(520, 300); imported.WithNaturalSize(520, 300);
        Assert.Equal(original.RenderSvg("portable"), imported.RenderSvg("portable")); Assert.Equal(original.ToPng(), imported.ToPng());
    }

    private static (double, double, double, double) Padding(IVisualBlock block) =>
        (block.Options.Padding.Left, block.Options.Padding.Top, block.Options.Padding.Right, block.Options.Padding.Bottom);
}
