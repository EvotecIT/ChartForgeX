using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Existing API controls that can execute before the additive configuration product packet.</summary>
public sealed class MermaidC4ConfigurationDefaultOutputControls {
    [Theory]
    [InlineData("C4Context\nPerson(user,\"User\")\nSystem(app,\"Application\")\nRel(user,app,\"Uses\")","context")]
    [InlineData("C4Container\nBoundary(scope,\"Scope\") {\nContainer(api,\"API\",\"HTTP\")\nContainerDb(store,\"Store\",\"SQL\")\n}\nRel(api,store,\"Writes\")","nested")]
    public void OmittedAndEmptyC4ConfigurationKeepOrdinaryNativeOutputs(string source,string name) {
        var ordinary=MermaidRenderer.Render(source);var empty=MermaidRenderer.Render("%%{init: {'c4':{}}}%%\n"+source);
        Assert.False(ordinary.HasErrors);Assert.False(empty.HasErrors);
        var ordinaryArtifact=Assert.IsType<VisualArtifact>(ordinary.Artifact);var emptyArtifact=Assert.IsType<VisualArtifact>(empty.Artifact);
        var svg=ordinaryArtifact.ToSvg();var png=ordinaryArtifact.ToPng();Assert.Equal(svg,emptyArtifact.ToSvg());Assert.Equal(png,emptyArtifact.ToPng());
        var image=RasterImageDecoder.Decode(png);Assert.True(image.Width>0&&image.Height>0);
        var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;
        if(!Path.IsPathRooted(output))throw new InvalidOperationException("Capture root must be absolute.");Directory.CreateDirectory(output);
        foreach(var item in new[]{("ordinary",ordinaryArtifact),("empty",emptyArtifact)}) {
            var stem=Path.Combine(output,"c4-config-default-"+name+"-"+item.Item1);File.WriteAllText(stem+".svg",item.Item2.ToSvg());File.WriteAllBytes(stem+".png",item.Item2.ToPng());File.WriteAllText(stem+".json",item.Item2.ToInterchangeJson());
        }
    }
}
