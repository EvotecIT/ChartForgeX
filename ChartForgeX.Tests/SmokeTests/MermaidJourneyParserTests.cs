using System;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesJourneySectionsTasksScoresAndActors() {
        const string source = @"journey
title User Journey
section Start
  Open app: 5: User
  Find dashboard: 4: User, Analyst
section Resolve
  Run remediation: 3: Analyst";

        var result = new MermaidParser().ParseJourney(source);

        Assert(!result.HasErrors, "Mermaid journey parser should parse sections, scores, and actors: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid journey parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Journey, "Mermaid journey parser should produce a journey document.");
        Assert(document.Title == "User Journey", "Mermaid journey parser should preserve title statements.");
        Assert(document.Sections.Count == 2 && document.Sections[0].Name == "Start", "Mermaid journey parser should preserve sections.");
        Assert(document.Tasks.Count == 3 && document.Tasks[1].Score == 4, "Mermaid journey parser should parse scored tasks.");
        Assert(document.Tasks[1].Actors.Count == 2 && document.Tasks[1].Actors[1] == "Analyst", "Mermaid journey parser should parse comma-separated actors.");
        Assert(document.Tasks[2].Section == "Resolve", "Mermaid journey tasks should retain containing section names.");
        Assert(document.Statements.Count == 6, "Mermaid journey parser should retain raw semantic statements.");
    }

    private static void MermaidJourneyConvertsToChartArtifactAndRenders() {
        const string source = @"journey
title User Journey
section Start
  Open app: 5: User
  Find dashboard: 4: User, Analyst
section Resolve
  Run remediation: 3: Analyst";

        var result = new MermaidParser().ParseJourney(source);
        Assert(!result.HasErrors, "Mermaid journey parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid journey parser should produce a document.");
        var chart = document.ToChart(new MermaidJourneyRenderOptions { Id = "user-journey" });
        Assert(chart.Series.Count == 1 && chart.Series[0].Kind == ChartSeriesKind.Bar, "Mermaid journey conversion should produce a ChartForgeX score bar chart.");
        Assert(chart.Title == "User Journey", "Mermaid journey conversion should use Mermaid titles by default.");
        Assert(chart.Options.XAxisLabels.Count == 3, "Mermaid journey conversion should preserve one label per task.");

        var artifact = document.ToVisualArtifact(new MermaidJourneyRenderOptions { Id = "user-journey" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid journey visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is Chart, "Mermaid journey visual artifact should carry a renderable chart model.");
        Assert(artifact.Metadata["mermaid.sections"] == "2" && artifact.Metadata["mermaid.tasks"] == "3", "Mermaid journey artifacts should expose section and task counts.");
        Assert(artifact.Metadata["mermaid.actors"] == "2", "Mermaid journey artifacts should expose distinct actor counts.");
        Assert(artifact.Metadata["render.model"] == nameof(Chart), "Mermaid journey artifacts should expose the chart render model.");

        var svg = document.ToSvg(new MermaidJourneyRenderOptions { Id = "user-journey" });
        var png = document.ToPng(new MermaidJourneyRenderOptions { Id = "user-journey" });
        Assert(svg.Contains("data-cfx-role=\"bar\"", StringComparison.Ordinal), "Mermaid journey SVG rendering should emit ChartForgeX bar marks.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid journey PNG rendering should emit a valid PNG.");
    }

    private static void MermaidJourneyRejectsNonFiniteScores() {
        const string source = @"journey
section Start
  Impossible score: NaN: User";

        var result = new MermaidParser().ParseJourney(source);

        Assert(result.HasErrors, "Mermaid journey parser should reject non-finite scores before chart conversion.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("numeric score", StringComparison.Ordinal)), "Mermaid journey score diagnostics should explain the numeric score contract.");
    }

    private static void MermaidJourneyStripsInlineCommentsBeforeParsingTasks() {
        const string source = @"journey
section Start
  Login: 5: User %% happy path";

        var result = new MermaidParser().ParseJourney(source);

        Assert(!result.HasErrors, "Mermaid journey parser should ignore trailing Mermaid comments after tasks: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid journey parser should produce a document.");
        Assert(document.Tasks.Count == 1 && document.Tasks[0].Text == "Login" && document.Tasks[0].Score == 5, "Mermaid journey parser should parse task scores before trailing comments.");
        Assert(document.Tasks[0].Actors.Count == 1 && document.Tasks[0].Actors[0] == "User", "Mermaid journey parser should not include trailing comments in actor names.");
    }
}
