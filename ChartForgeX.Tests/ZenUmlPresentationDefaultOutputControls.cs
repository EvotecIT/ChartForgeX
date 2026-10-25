using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Run unchanged before and after the Zen presentation patch; compare all emitted bytes.</summary>
public sealed class ZenUmlPresentationDefaultOutputControls {
    public static IEnumerable<object[]> Cases() {
        foreach(var family in new[]{"zenuml-native-occurrences","zenuml-native-local-context","sequence-lifetimes-boxes","native"})
            foreach(var width in new[]{640,1100})foreach(var dark in new[]{false,true})yield return new object[]{family,width,dark};
    }
    [Theory] [MemberData(nameof(Cases))]
    public void PlainSourceAndNativeDefaultsRetainSvgPngAndPortableBytes(string family,int width,bool dark) {
        VisualArtifact artifact;
        if(family=="native") {
            var model=SequenceArtifact.Create("zen-ordinary").WithTitle("Ordinary sequence").WithSize(width,600)
                .AddParticipant("a","Accept").AddParticipant("b","Build").AddMessage("a","b","Read")
                .AddBlock(SequenceArtifactBlockKind.Loop,"repeat",0,0).AddNote(SequenceArtifactNotePlacement.Over,new[]{"a","b"},"Existing note");
            model.ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light;artifact=model.ToVisualArtifact();
        } else {
            var directory=family.StartsWith("zenuml",StringComparison.Ordinal)?Path.Combine("external-plugins","zenuml"):"fixtures";
            var source=File.ReadAllText(Path.Combine(TestRepository.Root,"tests","mermaid-conformance",directory,family+".mmd"));
            var result=MermaidRenderer.Render((dark?"%%{init: {'theme':'dark'}}%%\n":"")+source,
                new MermaidRenderOptions{Sequence=new MermaidSequenceRenderOptions{Width=width,Height=600}});
            Assert.Empty(result.Diagnostics);artifact=result.Artifact!;
        }
        var svg=artifact.ToSvg();var png=artifact.ToPng();var json=artifact.ToInterchangeJson();
        Assert.Equal(svg,artifact.ToSvg());Assert.Equal(png,artifact.ToPng());Assert.Equal(json,artifact.ToInterchangeJson());
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output) {
            Directory.CreateDirectory(output);var path=Path.Combine(output,"zen-default-"+family+"-"+width+(dark?"-dark":"-light"));
            File.WriteAllText(path+".svg",svg);File.WriteAllBytes(path+".png",png);File.WriteAllText(path+".json",json);
        }
    }
}
