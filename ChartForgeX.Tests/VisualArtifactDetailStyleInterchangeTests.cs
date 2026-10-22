using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualArtifactDetailStyleInterchangeTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeDetailTypographyRoundTripsInheritanceAndExplicitFalseWithoutAliasing(bool reset) {
        var style = new TextStyleOverride();
        if (reset) { style.Italic = false; style.Underline = false; style.Strikethrough = false; }
        var chart = Diagram(style);
        var artifact = chart.Prepare();
        var envelope = artifact.ToInterchangeEnvelope();
        var captured = Assert.Single(Assert.Single(envelope.Nodes).Details).TextStyle!;
        Assert.NotSame(style, captured);
        string json = envelope.ToJson();
        Assert.Equal(reset, json.Contains("\"italic\":false", StringComparison.Ordinal));
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json);
        Assert.Equal(json, restored.ToJson());
        var fallback = new TextStyle(); fallback.Font.Italic = true; fallback.Underline = true; fallback.Strikethrough = true;
        var resolved = Assert.Single(Assert.Single(restored.Nodes).Details).TextStyle!.Resolve(fallback);
        Assert.Equal(!reset, resolved.Font.Italic);
        Assert.Equal(!reset, resolved.Underline);
        Assert.Equal(!reset, resolved.Strikethrough);
        style.Italic = true; style.FontSize = 48;
        Assert.Null(captured.FontSize);
        captured.Underline = true;
        Assert.Equal(json, artifact.ToInterchangeEnvelope().ToJson());
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)128)]
    [InlineData((byte)255)]
    public void NativeDetailTypographyRetainsVariationPaletteColorAndDecorationFacts(byte alpha) {
        var style = new TextStyleOverride().WithItalic().WithUnderline(TextDecorationStyle.Double)
            .WithWeight("600").WithFontSize(15).WithColor(ChartColor.FromRgba(20, 40, 60, alpha))
            .WithVariation("wght", 630).WithColorPalette(0).WithSuperscript().WithTextCase(TextCaseTransform.Uppercase);
        style.OpenTypeLanguageTag = "normal";
        var envelope = Diagram(style).ToVisualArtifact().ToInterchangeEnvelope();
        var restored = VisualArtifactInterchangeEnvelope.FromUtf8Json(envelope.ToUtf8Json());
        var actual = Assert.Single(Assert.Single(restored.Nodes).Details).TextStyle!;
        Assert.Equal(envelope.ToJson(), restored.ToJson());
        Assert.Equal(style.Color, actual.Color); Assert.Equal(630, actual.Variations!["wght"]);
        Assert.Equal(0, actual.ColorPaletteIndex); Assert.Equal(TextDecorationStyle.Double, actual.UnderlineStyle);
        Assert.Equal(TextBaseline.Superscript, actual.Baseline); Assert.Equal(style.TextCase, actual.TextCase);
        Assert.Equal("normal", actual.OpenTypeLanguageTag);
    }

    [Theory]
    [InlineData("\"fontSize\":15", "\"fontSize\":0")]
    [InlineData("\"italic\":true", "\"italic\":1")]
    [InlineData("\"colorPaletteIndex\":0", "\"colorPaletteIndex\":0.5")]
    [InlineData("\"wght\":630", "\"bad\":630")]
    public void InterchangeRejectsInvalidNativeTypographyAtItsExistingOwners(string before, string after) {
        var style = new TextStyleOverride().WithFontSize(15).WithItalic().WithVariation("wght", 630).WithColorPalette(0);
        var json = Diagram(style).ToVisualArtifact().ToInterchangeJson();
        Assert.Contains(before, json, StringComparison.Ordinal);
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace(before, after, StringComparison.Ordinal)));
    }

    private static TopologyChart Diagram(TextStyleOverride style) {
        var chart = TopologyChart.Create().WithViewport(500, 260).WithLegend(null).AddNode("item", "Contract", 40, 60, width: 240, height: 130);
        chart.Nodes[0].Details.Add(new TopologyNodeDetail { Text = "Native text", TextStyle = style });
        return chart;
    }
}
