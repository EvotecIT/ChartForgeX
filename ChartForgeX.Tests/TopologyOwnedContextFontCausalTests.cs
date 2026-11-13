using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyOwnedContextFontCausalTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void APreparedExplicitFileAndTheOrdinaryThemeFamilyKeepTheirOwnedNativePaintAfterRestoration(bool file) {
        var chart=TopologyChart.Create().WithId("owned-font-before").WithTitle("").WithViewport(640,400,24)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).AddNode("node","ChartForgeX",210,150,width:180,height:90);
        chart.Nodes[0].DisplayMode=TopologyNodeDisplayMode.CompactCard;chart.Nodes[0].PreserveDisplayModeSize=true;chart.Nodes[0].ShowStatusBadge=false;
        var path=Path.Combine(Path.GetTempPath(),"cfx-owned-context-font-"+Guid.NewGuid().ToString("N")+".ttf");
        try {
            if(file)File.Copy(Path.Combine(TestRepository.Root,"ChartForgeX.Tests","Fixtures","OpenType","fallback-weight-regular.ttf"),path);
            var font=file?FontSpec.FromFile(path):FontSpec.FromFamily(BrowserProofFonts.Family);font.Family=BrowserProofFonts.Family;
            var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640,400),24),chart.Theme,chart.ThemeMode,new VisualFrame("","",showLegend:false),font);
            var prepared=chart.Prepare(context,new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,FitContentToViewport=false});
            var svg=prepared.ToSvg();var png=prepared.ToPng();var json=prepared.ToArtifact().ToInterchangeJson();Capture("owned-font-before-"+file,svg,png,json);
            if(file)File.Delete(path);
            var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();Capture("owned-font-before-restored-"+file,restored.ToSvg(),restored.ToPng(),restored.ToInterchangeEnvelope().ToJson());
            Assert.Equal(png,restored.ToPng());Assert.Equal(svg,prepared.ToSvg());Assert.Equal(png,prepared.ToPng());Assert.DoesNotContain(path,json,StringComparison.Ordinal);
        } finally {if(File.Exists(path))File.Delete(path);}
    }
    private static void Capture(string name,string svg,byte[] png,string json) {
        if(Environment.GetEnvironmentVariable("CFX_TOPOLOGY_THEME_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var path=Path.Combine(output,name);File.WriteAllText(path+".svg",svg);File.WriteAllBytes(path+".png",png);File.WriteAllText(path+".json",json);
    }
}
