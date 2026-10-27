using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void QuadrantSourceDepthContracts() {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("QuadrantSourceDepth.mmd")
            ?? throw new InvalidOperationException("Packed adapter probe omitted its shared Quadrant source fixture.");
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();
        var direct = RenderFixture(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        var fenced = CreateFixtureParser(new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null
            && !fenced.HasErrors && fenced.Artifacts.Count == 1, "Packed direct/fenced Quadrant source mapping was rejected.");
        var artifact = direct.Artifact ?? throw new InvalidOperationException("Packed Quadrant source omitted its native artifact.");
        var chart = artifact.Model as Chart ?? throw new InvalidOperationException("Packed Quadrant did not use native Chart.");
        var points = chart.Series.Single();
        PackageAssertions.Require(points.Kind == ChartSeriesKind.Scatter && points.Points.Count == 4
            && points.PointLabels[0] == "Opportunity" && points.GetPointMarkers(0)!.Radius == 9
            && points.GetPointMarkers(1)!.Fill?.Equals(ChartColor.FromHex("#0369A1")) == true && points.GetPointMarkers(3) == null,
            "Packed class/direct source paint lost source order or per-point ownership.");
        PackageAssertions.Require(chart.Options.XAxis.Labels.Any(label => label.Value == .25 && label.Text == "Low effort")
            && chart.Options.XAxis.Labels.Any(label => label.Value == .75 && label.Text == "High effort")
            && chart.Options.YAxis.Labels.Any(label => label.Value == .75 && label.Text == "High impact"),
            "Packed source endpoint captions did not reach independently located native axis labels.");
        var svg = artifact.ToSvg();
        PackageAssertions.Require(svg == fenced.Artifacts[0].ToSvg() && !svg.Contains("<script", StringComparison.OrdinalIgnoreCase)
            && !svg.Contains("<foreignObject", StringComparison.OrdinalIgnoreCase), "Packed Quadrant direct/fence static output diverged.");
        PackageAssertions.Png(artifact.ToPng());
        var portable = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(portable.Extensions["mermaid.quadrant.sourceFacts"].Contains("stroke-width", StringComparison.Ordinal),
            "Packed Quadrant portable facts discarded source style declarations.");
        var detached = points.GetPointMarkers(0)!; detached.Radius = 44;
        PackageAssertions.Require(points.GetPointMarkers(0)!.Radius == 9, "Packed per-point marker getter exposed mutable installed state.");
        points.WithObservationId(0, new ChartObservationId("opportunity"))
            .WithObservationId(1, new ChartObservationId("investment"))
            .WithObservationId(2, new ChartObservationId("experiment"))
            .WithObservationId(3, new ChartObservationId("review"));
        points.ApplyPointChanges(new[] {
            ChartObservationChange<ChartPoint>.Remove(new ChartObservationId("opportunity")),
            ChartObservationChange<ChartPoint>.Replace(new ChartObservationId("investment"), new ChartPoint(.8, .8))
        });
        PackageAssertions.Require(points.GetPointMarkers(0)!.Radius == 7 && points.GetPointMarkers(1)!.Radius == 6,
            "Packed keyed edits detached source marker presentation from surviving identities.");
        PackageAssertions.Png(chart.ToPng());
        Console.WriteLine("Packed shared-source Quadrant styles, native endpoint captions, portable facts and keyed marker edits passed.");
    }
}
