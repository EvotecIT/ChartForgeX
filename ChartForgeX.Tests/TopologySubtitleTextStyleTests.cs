using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologySubtitleTextStyleTests {
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(true, true)]
    public void GenericSubtitleRoleMeasuresAndPaintsItsOwnFontAndColorThenRestoresDetached(bool enclosed, bool chip) {
        var chart = Source(); var node = chart.Nodes[0];
        if (enclosed) { node.Shape = TopologyNodeShape.Rectangle; node.FitLabelToContent = true; }
        var style = new TextStyleOverride { FontFamily = BrowserProofFonts.Family, FontSize = 29, Color = ChartColor.FromHex("#2367AB"), Italic = true };
        node.SubtitleTextStyle = style;
        var options = new TopologyRenderOptions { IncludeLegend = false, CardSubtitleMode = chip ? TopologyCardSubtitleMode.Chip : TopologyCardSubtitleMode.Text };
        var prepared = chart.Prepare(options);
        var row = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == (chip ? "topology-subtitle-chip-text" : "topology-node-subtitle"));
        Assert.Equal("Resilient service", Assert.Single(row.Text.Lines).Text); Assert.Equal(29, row.Text.Size, 6);
        Assert.True(row.Text.Style.Font.Italic); Assert.Equal(ChartColor.FromHex("#2367AB"), row.Text.Style.Color);
        var saved = prepared.ToInterchangeEnvelope(); Assert.Equal(2, saved.Version);
        var imported = VisualArtifactInterchangeEnvelope.FromUtf8Json(saved.ToUtf8Json()).ToTopologyChart();
        Assert.Equal(29, imported.Nodes[0].SubtitleTextStyle!.FontSize); Assert.True(imported.Nodes[0].SubtitleTextStyle!.Italic);
        var frozenSvg = prepared.ToSvg(); var frozenPng = prepared.ToPng();
        style.FontSize = 70; style.Color = ChartColor.FromHex("#AB2367");
        imported.Nodes[0].SubtitleTextStyle!.FontSize = 17;
        Assert.Equal(29, prepared.ToInterchangeEnvelope().Nodes[0].Topology!.SubtitleTextStyle!.FontSize);
        Assert.Equal(frozenSvg, prepared.ToSvg()); Assert.Equal(frozenPng, prepared.ToPng());
        Assert.Contains("#2367ab", frozenSvg.ToLowerInvariant());
    }

    [Fact]
    public void NullAndClearedSubtitleRoleKeepOrdinarySvgPngAndCapturedWireVersion() {
        var chart = Source(); chart.Nodes[0].TextStyle=null; var original = chart.Prepare();
        var svg = original.ToSvg(); var png = original.ToPng(); var json = original.ToInterchangeEnvelope().ToJson();
        chart.Nodes[0].SubtitleTextStyle = new TextStyleOverride { FontSize = 25, Color = ChartColor.FromHex("#123456") };
        var frozen = chart.Prepare();
        chart.Nodes[0].SubtitleTextStyle = null;
        var cleared = chart.Prepare();
        Assert.Equal(svg, cleared.ToSvg()); Assert.Equal(png, cleared.ToPng()); Assert.Equal(json, cleared.ToInterchangeEnvelope().ToJson());
        Assert.Equal(original.ToInterchangeEnvelope().Version, cleared.ToInterchangeEnvelope().Version);
        Assert.Equal(25, frozen.ToInterchangeEnvelope().Nodes[0].Topology!.SubtitleTextStyle!.FontSize);
    }

    private static TopologyChart Source() {
        var chart = TopologyChart.Create().WithTheme(ChartForgeX.Themes.VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithId("subtitle-role").WithViewport(760, 480, 24).WithLegend(null)
            .AddNode("service", "Service", 40, 60, width: 560, height: 300);
        chart.Nodes[0].Subtitle = "Resilient service"; chart.Nodes[0].ShowStatusBadge = false;
        chart.Nodes[0].PreserveDisplayModeSize = true;
        chart.Nodes[0].TextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family };
        return chart;
    }
}
