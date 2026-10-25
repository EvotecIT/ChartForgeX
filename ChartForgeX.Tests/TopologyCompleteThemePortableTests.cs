using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyCompleteThemePortableTests {
    [Theory] [InlineData(VisualThemeMode.Light)] [InlineData(VisualThemeMode.Dark)]
    public void CompletePairedThemeRetainsEveryExistingValueAndNativeUnitPixels(VisualThemeMode mode) {
        var chart=Source();chart.Theme=Complete();chart.ThemeMode=mode;var before=chart.Prepare();
        var svg=before.ToSvg();var png=before.ToPng();var envelope=before.ToInterchangeEnvelope();var json=envelope.ToJson();Capture("theme-complete-"+mode,svg,png,json);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var after=restored.Prepare();Capture("theme-complete-restored-"+mode,after.ToSvg(),after.ToPng(),after.ToInterchangeEnvelope().ToJson());
        Assert.Equal(chart.Theme.ToJson(),restored.Theme.ToJson());Assert.Equal(mode,restored.ThemeMode);Assert.Equal(2,envelope.Version);
        Assert.NotNull(envelope.Presentation!.Theme!.Definition);Assert.Equal(png,after.ToPng());
        foreach(var selected in new[]{VisualThemeMode.Light,VisualThemeMode.Dark}) {
            restored.ThemeMode=selected;var edited=restored.Prepare();var sibling=Source();sibling.Theme=chart.Theme;sibling.ThemeMode=selected;
            Assert.Equal(sibling.Prepare().ToPng(),edited.ToPng());
        }
        envelope.Presentation.Theme.Definition=VisualTheme.Graphite();Assert.Equal(chart.Theme.ToJson(),before.ToInterchangeEnvelope().Presentation!.Theme!.Definition!.ToJson());
        chart.Theme=VisualTheme.Graphite();Assert.Equal(svg,before.ToSvg());Assert.Equal(png,before.ToPng());
    }

    [Fact]
    public void OrdinaryGraphiteFamilyAndRoleFactsKeepDefaultOmissionAndVersionOne() {
        var chart=Source();chart.Theme=VisualTheme.Graphite();chart.Nodes[0].IconShape=null;var before=chart.Prepare();var json=before.ToInterchangeEnvelope().ToJson();Capture("theme-complete-ordinary",before.ToSvg(),before.ToPng(),json);
        var envelope=VisualArtifactInterchangeEnvelope.FromJson(json);var restored=envelope.ToTopologyChart();
        Assert.Null(envelope.Presentation!.Theme!.Definition);Assert.Null(envelope.Presentation.Theme.Mode);Assert.Equal(1,envelope.Version);
        Assert.DoesNotContain("themeJson",json,StringComparison.Ordinal);Assert.DoesNotContain("themeMode",json,StringComparison.Ordinal);
        Assert.Equal(chart.Theme.ToJson(),restored.Theme.ToJson());Assert.Equal(before.ToPng(),restored.ToPng());
    }

    [Fact]
    public void WireThemeLimitsDoNotEagerlyRejectNativeStaticPaintingOrWrapping() {
        var chart=Source();chart.Theme=chart.Theme.WithPalette(Enumerable.Repeat(ChartColor.FromHex("#2563eb"),300).ToArray());
        var before=chart.Prepare();var svg=before.ToSvg();var png=before.ToPng();var artifact=chart.ToVisualArtifact();
        Capture("theme-native-wire-limit",svg,png,"");Capture("theme-native-wire-limit-wrapped",artifact.ToSvg(),artifact.ToPng(),"");
        Assert.Equal(TopologyStaticPaintObservation.Paint(svg),TopologyStaticPaintObservation.Paint(artifact.ToSvg()));Assert.Equal(png,artifact.ToPng());
        Assert.Throws<ArgumentException>(()=>artifact.ToInterchangeJson());Assert.Equal(TopologyStaticPaintObservation.Paint(svg),TopologyStaticPaintObservation.Paint(artifact.ToSvg()));Assert.Equal(png,artifact.ToPng());
    }

    [Fact]
    public void UndefinedModesMalformedDefinitionAndHostCssRemainExplicitPortableErrors() {
        var envelope=Source().Prepare().ToInterchangeEnvelope();envelope.Presentation!.Theme!.Mode=(VisualThemeMode)999;
        Assert.Throws<ArgumentException>(()=>envelope.Validate());envelope.Presentation.Theme.Mode=null;
        var json=envelope.ToJson().Replace("\"theme\":{","\"theme\":{\"themeJson\":\"{}\",",StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(json));
        envelope.Presentation.Theme.Definition=Complete();envelope.Presentation.Theme.Border="var(--host-border)";
        Assert.Throws<NotSupportedException>(()=>envelope.ToTopologyChart());
    }

    internal static TopologyChart Source() {
        var chart=TopologyChart.Create().WithId("complete-theme").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).AddNode("node","Review request",210,150,width:180,height:90);
        var node=chart.Nodes[0];node.DisplayMode=TopologyNodeDisplayMode.Icon;node.Shape=TopologyNodeShape.Rectangle;node.IconShape=TopologyIconShape.Router;
        node.PreserveDisplayModeSize=true;node.ShowStatusBadge=false;node.Color="#2563eb";
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeLegend=false,IncludeTitle=false,IncludeIconLabels=false,FitContentToViewport=false});return chart;
    }
    private static VisualTheme Complete()=>VisualTheme.Graphite().Configure(theme=>{
        theme.Spacing=17;theme.SeriesStrokeWidth=2.25;theme.MarkerRadius=4;theme.AreaOpacity=.27;theme.BarRadius=11;theme.MaximumBarWidth=79;
        theme.GridStrokeWidth=1.25;theme.AxisStrokeWidth=3.5;theme.CardRadius=7;theme.PlotRadius=9;theme.CardShadowOpacity=.12;
        theme.CardShadowColor=ChartColor.FromHex("#12345678");theme.GaugeStrokeWidth=16;theme.GaugeBandWidth=6;
        theme.Typography=new VisualTypography(BrowserProofFonts.Family,23,15,13,14,17,39,25,16,"Cascadia Mono, Consolas, monospace",15,27,14);
        theme.Light.Palette=new[]{ChartColor.FromHex("#ef4444"),ChartColor.FromHex("#22c55e")};theme.Dark.Palette=new[]{ChartColor.FromHex("#a855f7"),ChartColor.FromHex("#06b6d4")};
        theme.Light.Grid=ChartColor.FromHex("#224466");theme.Dark.Axis=ChartColor.FromHex("#778899");
        theme.Light.Status.Pass=new VisualTokenColor(ChartColor.FromHex("#008844"),ChartColor.FromHex("#ffffff"));
        theme.Light.SequentialRamp=new[]{ChartColor.FromHex("#eeeeee"),ChartColor.FromHex("#111111")};
        theme.Dark.DivergingRamp=new VisualDivergingRamp(new[]{ChartColor.FromHex("#112233")},ChartColor.FromHex("#334455"),new[]{ChartColor.FromHex("#556677")});
    });
    internal static void Capture(string name,string svg,byte[] png,string json) {
        if(Environment.GetEnvironmentVariable("CFX_TOPOLOGY_THEME_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var path=Path.Combine(output,name);File.WriteAllText(path+".svg",svg);File.WriteAllBytes(path+".png",png);if(json.Length>0)File.WriteAllText(path+".json",json);
    }
}
