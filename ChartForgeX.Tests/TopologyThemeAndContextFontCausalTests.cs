using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyThemeAndContextFontCausalTests {
    [Theory] [InlineData("stroke")] [InlineData("radius")] [InlineData("palette")] [InlineData("dark")]
    public void NativeThemeFactsThatReachBodyPaintSurviveEditablePortableRestoration(string variant) {
        var chart=Source(TopologyNodeDisplayMode.Icon);chart.Theme=VisualTheme.Graphite().Configure(theme=>{
            if(variant=="stroke")theme.AxisStrokeWidth=3.5;if(variant=="radius")theme.BarRadius=11;
            if(variant=="palette"){theme.Light.Palette=new[]{ChartColor.FromHex("#ef4444"),ChartColor.FromHex("#22c55e")};theme.Dark.Palette=new[]{ChartColor.FromHex("#a855f7"),ChartColor.FromHex("#06b6d4")};}
        });if(variant=="dark")chart.ThemeMode=VisualThemeMode.Dark;
        var direct=chart.Prepare();var png=direct.ToPng();var json=direct.ToInterchangeEnvelope().ToJson();Capture("theme-"+variant,direct.Visual,json);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var after=restored.Prepare();Capture("theme-"+variant+"-restored",after.Visual,after.ToInterchangeEnvelope().ToJson());
        if(variant=="stroke")Assert.Equal(chart.Theme.AxisStrokeWidth,restored.Theme.AxisStrokeWidth);
        if(variant=="radius")Assert.Equal(chart.Theme.BarRadius,restored.Theme.BarRadius);
        if(variant=="palette")foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})Assert.Equal(chart.Theme.Resolve(mode).Palette,restored.Theme.Resolve(mode).Palette);
        if(variant=="dark")Assert.Equal(chart.ThemeMode,restored.ThemeMode);
        Assert.Equal(png,after.ToPng());Assert.Equal(png,direct.ToPng());
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void AnExplicitContextFontFamilyIsAsPortableAsTheIncumbentThemeFamily(bool throughTheme) {
        var chart=Source(TopologyNodeDisplayMode.CompactCard);if(throughTheme)chart.Theme=chart.Theme.WithFontFamily(BrowserProofFonts.Family);
        var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640,400),24),chart.Theme,chart.ThemeMode,
            new VisualFrame(title:"",subtitle:"",showLegend:false),throughTheme?null:FontSpec.FromFamily(BrowserProofFonts.Family));
        var options=new TopologyRenderOptions{IncludeLegend=false,IncludeTitle=false,FitContentToViewport=false};
        var direct=chart.Prepare(context,options);var png=direct.ToPng();var envelope=direct.ToArtifact().ToInterchangeEnvelope();Capture("font-context-"+throughTheme,direct,envelope.ToJson());
        var restored=VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart();var after=restored.Prepare();Capture("font-context-"+throughTheme+"-restored",after.Visual,after.ToInterchangeEnvelope().ToJson());
        Assert.Equal(png,after.ToPng());Assert.Equal(png,direct.ToPng());Assert.Equal(throughTheme?BrowserProofFonts.Family:VisualTheme.Graphite().Typography.Family,chart.Theme.Typography.Family);
    }

    [Fact]
    public void OrdinaryUnitGraphiteGlyphPaintKeepsItsExistingPortableContract() {
        var chart=Source(TopologyNodeDisplayMode.Icon);var direct=chart.Prepare();var png=direct.ToPng();var json=direct.ToInterchangeEnvelope().ToJson();Capture("theme-ordinary-control",direct.Visual,json);
        var after=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();Capture("theme-ordinary-restored",after.Visual,after.ToInterchangeEnvelope().ToJson());Assert.Equal(png,after.ToPng());Assert.Equal(json,direct.ToInterchangeEnvelope().ToJson());
    }

    private static TopologyChart Source(TopologyNodeDisplayMode mode) {
        var chart=TopologyChart.Create().WithId("theme-context-before").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("node","Review request",210,150,width:180,height:90);var node=chart.Nodes[0];node.DisplayMode=mode;node.Shape=TopologyNodeShape.Rectangle;node.IconShape=TopologyIconShape.Router;node.PreserveDisplayModeSize=true;node.ShowStatusBadge=false;node.Color="#2563eb";
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeLegend=false,IncludeTitle=false,IncludeIconLabels=false,FitContentToViewport=false});return chart;
    }
    private static void Capture(string name,PreparedVisual visual,string json){if(Environment.GetEnvironmentVariable("CFX_TOPOLOGY_THEME_CAPTURE_DIRECTORY") is not {Length:>0} output)return;Directory.CreateDirectory(output);var path=Path.Combine(output,name);File.WriteAllText(path+".svg",visual.ToSvg());File.WriteAllBytes(path+".png",visual.ToPng());File.WriteAllText(path+".json",json);}
}
