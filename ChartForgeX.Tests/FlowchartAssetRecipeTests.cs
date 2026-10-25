using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class FlowchartAssetRecipeTests {
    [Theory]
    [InlineData("compact",false)] [InlineData("compact",true)] [InlineData("wide",false)] [InlineData("wide",true)]
    public void EmittedGalleryRecipeUsesCapturedImagesAndFiniteNativeGlyphs(string variant,bool dark) {
        var mode=dark?VisualThemeMode.Dark:VisualThemeMode.Light;
        var artifact=FlowchartAssetRecipe.Create(variant,mode,BrowserProofFonts.Family);
        var svg=artifact.ToSvg();var png=artifact.ToPng();var json=artifact.ToInterchangeJson();
        var html=artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize});
        var program=ExampleRecipeSource.EmitFlowchartAssets(variant,mode);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var after=restored.Prepare();
        Capture("asset-program-"+variant+"-"+dark,svg,png,json,html,program);
        Capture("asset-program-restored-"+variant+"-"+dark,after.ToSvg(),after.ToPng(),after.ToInterchangeEnvelope().ToJson(),"","");
        var chart=Assert.IsType<TopologyChart>(artifact.Model);var prepared=chart.Prepare();
        Assert.Equal(5,chart.Nodes.Count);Assert.Equal(2,chart.Groups.Count);Assert.Equal(4,chart.Edges.Count);
        var image=chart.Nodes.Single(node=>node.Id=="Review");Assert.Equal(TopologyNodeDisplayMode.Artwork,image.DisplayMode);
        Assert.Equal(TopologyCaptionPlacement.Top,image.CaptionPlacement);Assert.Equal(8,image.CaptionGap);Assert.Equal(16,image.CaptionTextStyle!.FontSize);
        Assert.Equal("common:server",chart.Nodes.Single(node=>node.Id=="Policy").Metadata["mermaid.property.icon"]);
        Assert.Equal("cloud:cloud",chart.Nodes.Single(node=>node.Id=="Publish").Metadata["mermaid.property.icon"]);
        Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>());
        var text=string.Join(" ",prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().SelectMany(mark=>mark.Text.Lines).Select(line=>line.Text));
        foreach(var label in new[]{"Review group","Policy service","Publish result"})Assert.Contains(label,text,StringComparison.Ordinal);
        Assert.Equal(png,after.ToPng());Assert.Equal(2,VisualArtifactInterchangeEnvelope.FromJson(json).Version);
        Assert.Contains("var artifact=FlowchartAssetRecipe.Create",program,StringComparison.Ordinal);
        foreach(var required in new[]{"using System.Collections.Generic;","using ChartForgeX.Raster;","ImageBindings","ToInterchangeJson"})Assert.Contains(required,program,StringComparison.Ordinal);
        Assert.DoesNotContain("ChartForgeX.Examples",program,StringComparison.Ordinal);Assert.DoesNotContain("node_modules",program,StringComparison.Ordinal);
        Assert.DoesNotContain("<script",html,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("foreignObject",svg,StringComparison.Ordinal);
    }

    internal static void Capture(string name,string svg,byte[] png,string json,string html,string program) {
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var path=Path.Combine(output,name);File.WriteAllText(path+".svg",svg);File.WriteAllBytes(path+".png",png);
        File.WriteAllText(path+".json",json);if(html.Length>0)File.WriteAllText(path+".html",html);if(program.Length>0)File.WriteAllText(path+".Program.cs",program);
    }
}
