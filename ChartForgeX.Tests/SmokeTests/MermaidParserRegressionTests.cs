using System;
using ChartForgeX.Mermaid;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserReportsDanglingFlowchartEdges() {
        const string source = @"flowchart LR
  A -->";

        var result = new MermaidParser().ParseFlowchart(source);

        Assert(result.HasErrors, "Dangling Mermaid flowchart edges should produce diagnostics.");
        Assert(result.Diagnostics[0].Span.Line == 2, "Dangling Mermaid flowchart edge diagnostics should preserve source line.");
        Assert(result.Diagnostics[0].Message.Contains("target", StringComparison.OrdinalIgnoreCase), "Dangling Mermaid flowchart edge diagnostics should identify the missing target.");
    }

    private static void MermaidParserKeepsHyphenatedSequenceParticipantIds() {
        const string source = @"sequenceDiagram
participant api-v1 as API
participant DB as Database
api-v1->>DB: Query";

        var result = new MermaidParser().ParseSequence(source);

        Assert(!result.HasErrors, "Hyphenated sequence participant ids should parse without errors: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid sequence parser should produce a document.");
        Assert(document.Messages.Count == 1, "Hyphenated sequence ids should still produce one message.");
        Assert(document.Messages[0].SourceId == "api-v1" && document.Messages[0].TargetId == "DB", "Hyphenated sequence ids should remain intact across message parsing.");
        Assert(document.Participants.Count == 2, "Hyphenated sequence ids should not create bogus implicit participants.");
    }

    private static void MermaidParserReportsUnrecognizedSequenceStatements() {
        const string source = @"sequenceDiagram
Alice => Bob: hi";

        var result = new MermaidParser().ParseSequence(source);

        Assert(result.HasErrors, "Unrecognized Mermaid sequence statements should produce diagnostics.");
        Assert(result.Diagnostics[0].Span.Line == 2, "Unrecognized Mermaid sequence diagnostics should preserve source line.");
        Assert(result.Diagnostics[0].Message.Contains("Unrecognized sequence", StringComparison.Ordinal), "Unrecognized Mermaid sequence diagnostics should describe the unsupported statement.");
    }
}
