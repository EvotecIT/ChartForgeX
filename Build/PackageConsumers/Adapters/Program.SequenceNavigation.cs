using System;
using System.IO;
using ChartForgeX.Mermaid;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.VisualArtifacts;
internal static partial class Program {
    private static void SequenceNavigationSourceContracts() {
        using var stream=typeof(Program).Assembly.GetManifestResourceStream("PackageProbe.sequence-navigation.mmd")??throw new InvalidOperationException("Missing navigation source.");using var reader=new StreamReader(stream);var result=MermaidRenderer.Render(reader.ReadToEnd(),new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject});if(result.HasErrors||result.Artifact==null||result.Diagnostics.Count!=0)throw new InvalidOperationException("Navigation source did not lower.");var document=(MermaidSequenceDocument)result.Document!;var sequence=(SequenceArtifact)result.Artifact.Model!;var html=sequence.ToNavigationHtmlPage(new HtmlSequenceNavigationOptions{InitiallyExpanded=document.Configuration.SequenceForceMenus==true});var restored=VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson()).ToSequenceArtifact();if(restored.Participants[0].NavigationLinks.Count!=2||!html.Contains("<details")||!html.Contains(" open")||html.Contains("<script"))throw new InvalidOperationException("Native navigation host ownership was lost.");
    }
}
