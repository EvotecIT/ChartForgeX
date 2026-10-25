using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class SequencePresetOmittedBeforeTests {
    [Theory] [InlineData(VisualThemeMode.Light,"#FDF4FF")] [InlineData(VisualThemeMode.Dark,"#111113")]
    public void OmittedSourcePaletteHasPinnedLightOrSelectedPairedDarkSurface(VisualThemeMode mode,string expected) {
        const string source="sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\nA->>B: Save";var result=MermaidRenderer.Render(source,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=mode}});var root=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(!string.IsNullOrWhiteSpace(root)){Directory.CreateDirectory(root);var stem=Path.Combine(root,"sequence-preset-true-omitted-before-"+mode);File.WriteAllText(stem+".mmd",source);File.WriteAllText(stem+".diagnostics.txt",string.Join("\n",result.Diagnostics.Select(d=>d.Message)));if(result.Artifact is {} artifact){File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".png",artifact.ToPng());File.WriteAllText(stem+".json",artifact.ToInterchangeJson());}}Assert.False(result.HasErrors);var model=Assert.IsType<SequenceArtifact>(result.Artifact!.Model);Assert.Empty(result.Document!.Configuration.Settings);Assert.Equal(expected,(model.Participants[0].Fill??model.Paint.ParticipantFill??model.Theme.Resolve(model.ThemeMode).Surface).ToCss());
    }
}
