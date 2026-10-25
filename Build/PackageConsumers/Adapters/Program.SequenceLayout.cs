using System;
using System.IO;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void SequenceLayoutSourceContracts() {
        string source;
        using(var stream=typeof(Program).Assembly.GetManifestResourceStream("SequenceLayoutTypography.mmd")??throw new InvalidOperationException("Missing sequence source fixture."))
        using(var reader=new StreamReader(stream))source=reader.ReadToEnd();
        var direct=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject});
        PackageAssertions.Require(!direct.HasErrors&&direct.Artifact!=null,"Sequence source policy failed.");
        var model=(SequenceArtifact)direct.Artifact!.Model;PackageAssertions.Require(model.Typography?.Message?.FontSize==20&&model.Layout?.ActivationWidth==14,"Sequence source policy was lost.");
        var fenced=new MermaidVisualMarkupParser().Parse("```mermaid\n"+source+"\n```");
        PackageAssertions.Require(!fenced.HasErrors&&fenced.Artifacts.Count==1,"Sequence fence policy failed.");
        PackageAssertions.Png(direct.Artifact.ToPng());var restored=VisualArtifactInterchangeEnvelope.FromJson(direct.Artifact.ToInterchangeJson()).ToSequenceArtifact();
        restored.Messages[0].Text="Saved request";PackageAssertions.Require(restored.ToSvg().Contains("Saved request"),"Sequence portable edit failed.");
    }
}
