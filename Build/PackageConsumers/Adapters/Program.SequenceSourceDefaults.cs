using System;
using System.IO;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
internal static partial class Program {
    private static void SequenceSourceDefaultsSourceContracts() {using var stream=typeof(Program).Assembly.GetManifestResourceStream("PackageProbe.sequence-source-defaults.mmd")??throw new InvalidOperationException("Missing default-source fixture.");using var reader=new StreamReader(stream);var result=RenderFixture(reader.ReadToEnd(),new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject});if(result.HasErrors||result.Artifact==null||result.Diagnostics.Count!=0)throw new InvalidOperationException("Sequence defaults did not lower.");var model=(SequenceArtifact)result.Artifact.Model!;var document=(MermaidSequenceDocument)result.Document!;var layout=model.Layout;if(document.Configuration.Settings.Count!=0||layout==null||layout.MirrorParticipants!=true||layout.ParticipantMinimumWidth!=150||model.Paint.LifelineStroke?.Width!=.5||result.Artifact.ToPng().Length==0)throw new InvalidOperationException("Sequence omitted policies were lost.");}
}
