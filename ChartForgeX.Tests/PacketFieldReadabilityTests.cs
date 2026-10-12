using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PacketFieldReadabilityTests {
    [Fact]
    public void WrappedCaptionShortensAtReadableSizeInsteadOfShrinkingBelowEightPixels() {
        var packet = PacketLayoutBlock.Create().WithBitsPerRow(16).WithBitWidth(18).WithRowHeight(40).WithFieldGap(3)
            .AddField(0, 3, "Flags").AddField(4, 17, "Payload").AddField(18, 23, "Tail");
        packet.Options.Theme.FontFamily = "Consolas, DejaVu Sans Mono, monospace";
        packet.Options.Theme.DataLabelFontSize = 32;
        var captions = XDocument.Parse(packet.ToSvg()).Descendants().Where(node => node.Name.LocalName == "text"
            && (string?)node.Attribute("data-cfx-role") == "visual-text").ToArray();
        Assert.Equal(4, captions.Length);
        Assert.All(captions, node => Assert.True(double.Parse(node.Attribute("font-size")!.Value, CultureInfo.InvariantCulture) >= 8));
        Assert.Equal("Payload", packet.Fields[1].Label);
        Assert.NotEmpty(captions[2].Value);
        Assert.NotEqual("Payload", captions[2].Value);
        Assert.NotEmpty(packet.ToPng());
    }
}
