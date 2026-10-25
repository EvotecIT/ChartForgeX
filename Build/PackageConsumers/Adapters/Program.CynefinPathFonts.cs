using System.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void CynefinPathFontContracts() {
        const string source = "%%{init: {'themeVariables':{'cynefin':{'domainFontSize':22,'itemFontSize':15,'textColor':'#123456'}}}}%%\ncynefin-beta\ncomplex\n\"Discover demand\"\nclear\n\"Run checklist\"\n";
        var parsed = new MermaidParser().Parse(source);
        PackageAssertions.Require(!parsed.HasErrors && parsed.Diagnostics.Count == 0, "Packed Cynefin source fonts were not consumed.");
        var chart = ((MermaidCynefinDocument)parsed.Document!).ToTopologyChart();
        var artifact = chart.ToVisualArtifact(); var saved = artifact.ToInterchangeEnvelope();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        PackageAssertions.Require(saved.Version == 2 && restored.PathDecorations.Count == 8, "Packed Cynefin paths were not restored.");
        PackageAssertions.Require(restored.Nodes.All(node => node.TextStyle!.FontSize == 22 && node.SubtitleTextStyle!.FontSize == 14)
            && restored.Nodes.SelectMany(node => node.Details).All(detail => detail.TextStyle!.FontSize == 15), "Packed Cynefin font roles were lost.");
        PackageAssertions.Require(restored.Theme.Typography.TitleSize == 24, "Packed Cynefin title size was lost.");
        PackageAssertions.Require(restored.ToSvg().Contains("Discover demand"), "Packed Cynefin authored items disappeared.");
        PackageAssertions.Png(restored.ToPng());
    }
}
