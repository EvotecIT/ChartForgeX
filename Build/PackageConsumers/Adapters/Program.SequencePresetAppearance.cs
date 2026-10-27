using System;
using System.IO;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
internal static partial class Program {
    private static void SequencePresetAppearanceSourceContracts() {
        using var stream=typeof(Program).Assembly.GetManifestResourceStream("PackageProbe.sequence-preset-appearance.mmd")??throw new InvalidOperationException("Missing palette fixture.");using var reader=new StreamReader(stream);var source=reader.ReadToEnd();foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark}){var result=RenderFixture(source,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=mode}});if(result.HasErrors||result.Artifact?.Model is not SequenceArtifact model||model.Layout?.ParticipantBands==null)throw new InvalidOperationException("Omitted Sequence preset/band projection failed.");var expected=mode==VisualThemeMode.Dark?null:"#FDF4FF";if(model.Participants[0].Fill?.ToCss()!=expected||model.Participants[0].Border?.ToCss()!="#E879F9"||result.Artifact.ToPng().Length==0)throw new InvalidOperationException("Source preset palette did not follow effective host mode.");var restored=VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson()).ToSequenceArtifact();restored.Messages[0].Text="Edited";if(!restored.ToSvg().Contains("Edited"))throw new InvalidOperationException("Source preset facts did not restore to editable native owners.");}
    }
}
