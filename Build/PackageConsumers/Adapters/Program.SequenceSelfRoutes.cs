using System;
using System.IO;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
internal static partial class Program {
    private static void SequenceSelfRoutesSourceContracts() {
        using var stream=typeof(Program).Assembly.GetManifestResourceStream("PackageProbe.sequence-self-routes.mmd")??throw new InvalidOperationException("Missing self-route source.");using var reader=new StreamReader(stream);var result=MermaidRenderer.Render(reader.ReadToEnd(),new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject});if(result.HasErrors||result.Diagnostics.Count!=0||result.Artifact==null)throw new InvalidOperationException("Self-route source did not lower.");var restored=VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson()).ToSequenceArtifact();if(restored.Layout?.SelfMessageRouting!=SequenceSelfMessageRouting.Curved||result.Artifact.ToPng().Length==0)throw new InvalidOperationException("Source self route did not survive.");
    }
}
