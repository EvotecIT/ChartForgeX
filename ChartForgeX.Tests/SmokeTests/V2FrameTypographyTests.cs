using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2FrameTypographyTests {
    [Theory]
    [InlineData(TextBaseline.Superscript, -7.28)]
    [InlineData(TextBaseline.Subscript, 4.576)]
    public void NativeScriptTypographyMaterializesItsSizeAndBaselineExactlyOnce(TextBaseline baseline, double offset) {
        var builder = new VisualSceneBuilder(new VisualSize(200, 100), FontSpec.FromFamily("Missing script fixture font"));
        var style = new TextStyle { FontSize = 32, Baseline = baseline };
        builder.Text("Script", 20, 50, style, role: "script");
        var text = Assert.Single(builder.Build().Nodes.OfType<VisualSceneText>());
        Assert.Equal(50 + offset, text.Baseline, 8);
        Assert.Equal(32 * .65, text.Text.Size, 8);
        Assert.Equal(TextBaseline.Normal, text.Text.Style.Baseline);
    }

    [Fact]
    public void FrameTypographyIsDetachedAndUsesTheSameMeasuredPlacementAcrossFamilies() {
        var style = new TextStyle { Font = FontSpec.FromFamily("Missing frame test font"), FontSize = 24,
            Color = ChartColor.FromHex("#7923A1"), Alignment = TextAlignment.Right, UnderlineStyle = TextDecorationStyle.Single };
        var frame = new VisualFrame("Shared heading", showLegend: false, titleStyle: style);
        style.FontSize = 60; frame.TitleStyle!.Font.Family = "Changed getter";
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(480, 320)), frame: frame);
        var line = Chart.Create().AddLine("Data", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) }).Prepare(context);
        var donut = Chart.Create().AddDonut("Data", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) }).Prepare(context);
        var lineText = Heading(line); var donutText = Heading(donut);
        Assert.Equal(lineText.ToString(), donutText.ToString());
        Assert.Equal("24", lineText.Attribute("font-size")!.Value);
        Assert.Equal("Missing frame test font", lineText.Attribute("font-family")!.Value);
        Assert.Equal("#7923A1", lineText.Attribute("fill")!.Value, ignoreCase: true);
        Assert.Contains(XDocument.Parse(line.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "text-decoration");
        Assert.True(double.Parse(lineText.Attribute("x")!.Value, System.Globalization.CultureInfo.InvariantCulture) > 100);
        Assert.Equal(480, line.ToRgba().Width);
    }

    [Fact]
    public void ExplicitModelHeadingOverrideWinsOverContextAndSnapshotRetainsIt() {
        var chart = Chart.Create().WithTitle("Override").AddBar("Values", new[] { new ChartPoint(0, 1) });
        chart.Options.TitleStyle.FontSize = 31;
        chart.Options.TitleStyle.Color = ChartColor.FromHex("#945112");
        var contextStyle = new TextStyle { FontSize = 18, Color = ChartColor.Black };
        var prepared = chart.Prepare(new VisualRenderContext(frame: new VisualFrame(titleStyle: contextStyle)));
        var original = prepared.ToSvg();
        chart.Options.TitleStyle.FontSize = 60; contextStyle.FontSize = 70;
        Assert.Equal("31", Heading(prepared).Attribute("font-size")!.Value);
        Assert.Equal("#945112", Heading(prepared).Attribute("fill")!.Value, ignoreCase: true);
        Assert.Equal(original, prepared.ToSvg());
    }

    private static XElement Heading(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Single(element => (string?)element.Attribute("data-cfx-role") == "frame-heading").Elements().Single();
}
