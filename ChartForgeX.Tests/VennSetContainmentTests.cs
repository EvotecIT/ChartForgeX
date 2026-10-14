using ChartForgeX.Themes;
using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VennSetContainmentTests {
    [Theory]
    [InlineData(780, 460)]
    [InlineData(640, 240)]
    [InlineData(320, 180)]
    public void ThreeSetsKeepTheirPaintInsideThePaddedSurface(int width, int height) {
        var block = VennDiagramBlock.Create().WithSize(width, height).WithTitle("Overlap").WithCard(false)
            .AddSet("API", "API", 60).AddSet("UI", "UI", 55).AddSet("Ops", "Operations", 45);
        block.Options.Theme = block.Options.Theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: 21, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
        var svg = XDocument.Parse(block.ToSvg());
        var circles = svg.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == "venn-set").ToArray();
        Assert.Equal(3, circles.Length);
        foreach (var circle in circles) {
            double Number(string name) => double.Parse(circle.Attribute(name)!.Value, CultureInfo.InvariantCulture);
            Assert.True(Number("cy") + Number("r") + Number("stroke-width") / 2 <= height - block.Options.Padding.Bottom + 0.001);
            Assert.True(Number("cx") - Number("r") - Number("stroke-width") / 2 >= block.Options.Padding.Left - 0.001);
            Assert.True(Number("cx") + Number("r") + Number("stroke-width") / 2 <= width - block.Options.Padding.Right + 0.001);
        }
        Assert.NotEmpty(block.ToPng());
    }
}
