using System;
using System.IO;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void XYSourceCaptionContracts() {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("XYSourceDepth.mmd")
            ?? throw new InvalidOperationException("Packed adapter probe omitted its shared XY source fixture.");
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd().Replace("xychart-beta", "xychart-beta horizontal", StringComparison.Ordinal);
        var request = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject,
            XYChart = new MermaidXYChartRenderOptions { Width = 390, Height = 520 } };
        var rendered = RenderFixture(source, request);
        var fenced = CreateFixtureParser(request).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!rendered.HasErrors && rendered.Artifact != null && rendered.Diagnostics.Count == 0 && !fenced.HasErrors && fenced.Artifacts.Count == 1,
            "Packed XY captions/configuration did not reach one native artifact.");
        var chart = (Chart)rendered.Artifact!.Model!;
        PackageAssertions.Require(chart.Options.ShowLegend && chart.Options.Size.Width == 390 && chart.Options.LineOrientation == ChartOrientation.Horizontal
            && chart.Series[1].PointLabels.SequenceEqual(new[] { "Opened", "", "Reviewed, ready", "Released" }) && chart.Series[0].ShowDataLabels == false,
            "Packed XY source labels or independent bar-value policy were lost.");
        PackageAssertions.Require(rendered.Artifact.ToSvg() == fenced.Artifacts[0].ToSvg() && rendered.Artifact.ToPng().SequenceEqual(fenced.Artifacts[0].ToPng()),
            "Packed XY direct/fence exports disagree.");
        var saved = VisualArtifactInterchangeEnvelope.FromJson(rendered.Artifact.ToInterchangeJson());
        PackageAssertions.Require(saved.ToJson() == rendered.Artifact.ToInterchangeJson(), "Packed XY portable facts changed during serialization.");
        Console.WriteLine("Packed XY named series, line captions, source policies and direct/fence SVG/PNG passed.");
    }
}
