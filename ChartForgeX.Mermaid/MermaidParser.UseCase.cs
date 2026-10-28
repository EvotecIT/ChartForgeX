using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private static MermaidUseCaseDocument ParseUseCase(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidUseCaseDocument {
            SourceText = source, Kind = MermaidDiagramKind.UseCase, Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length), FrontMatter = frontMatter.Text,
            Direction = MermaidFlowchartDirection.TopToBottom
        };
        var body = (string[])lines.Clone();
        var actors = new HashSet<string>(StringComparer.Ordinal);
        var json = new Dictionary<string, MermaidUseCaseJson.Declaration>(StringComparer.Ordinal);
        var relations = new Dictionary<int, (string Kind, MermaidSourceSpan Span)>();
        var boundaryDepth = 0;
        for (var i = header.Line; i < body.Length; i++) {
            MermaidResourceGuard.Checkpoint();
            var text = body[i].Trim();
            var sourceSpan = new MermaidSourceSpan(i + 1, MermaidParserUtilities.LeadingWhitespace(body[i]) + 1, text.Length);
            if (TryUseCaseNote(document, text, sourceSpan, result)) { body[i] = string.Empty; continue; }
            if (MermaidUseCaseJson.Starts(text)) {
                var first = i; var line = i + 1;
                try {
                    var declaration = MermaidUseCaseJson.Read(lines, ref line);
                    if (json.ContainsKey(declaration.Id)) throw new ArgumentException("UseCase JSON graph identifiers must be declared once.");
                    json.Add(declaration.Id, declaration);
                    body[first] = new string(' ', declaration.IdSpan.Column - 1) + declaration.Id;
                } catch (ArgumentException error) {
                    MermaidParserUtilities.Add(result, sourceSpan, MermaidDiagnosticSeverity.Error, error.Message, MermaidDiagnosticCodes.InvalidStatement);
                    body[first] = string.Empty;
                }
                for (var continuation = first + 1; continuation < line; continuation++) body[continuation] = string.Empty;
                i = Math.Max(first, line - 1); continue;
            }
            if (text == "end") boundaryDepth--;
            if (text.StartsWith("direction ", StringComparison.Ordinal)) {
                document.Direction = ParseFlowchartDirection(text.Substring(10).Trim());
                if (document.Direction == MermaidFlowchartDirection.None) MermaidParserUtilities.Add(result, new MermaidSourceSpan(i + 1, 1, text.Length), MermaidDiagnosticSeverity.Error, "Use case direction must be TB, TD, BT, LR, or RL.");
                body[i] = string.Empty;
            } else if (text.StartsWith("actor ", StringComparison.Ordinal)) {
                actors.Add(Regex.Match(text.Substring(6), @"^\w+", RegexOptions.CultureInvariant).Value);
                body[i] = new string(' ', MermaidParserUtilities.LeadingWhitespace(body[i]) + 6) + text.Substring(6);
            } else if (text.StartsWith("systemBoundary ", StringComparison.Ordinal)) {
                if (++boundaryDepth > 1) MermaidParserUtilities.Add(result, new MermaidSourceSpan(i + 1, 1, text.Length), MermaidDiagnosticSeverity.Error, "Use case system boundaries cannot be nested.");
                var boundary = text.Substring(15).Trim();
                if (boundary.StartsWith("\"", StringComparison.Ordinal)) {
                    var label = boundary.Trim('"');
                    boundary = Regex.Replace(label, @"\W", "_") + "[\"" + label + "\"]";
                }
                body[i] = "subgraph " + boundary;
            } else if (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal)) {
                var label = text.Trim('"');
                body[i] = Regex.Replace(label, @"\W", "_") + "(\"" + label + "\")";
            } else {
                var relation = Regex.Match(text, @"^(?<source>\w+)\s+(?:(?<edge>\w+)\s*@\s*)?(?<arrow>\.\.>)\s*:\s*(?<kind>include|extend)\s+(?<target>\w+)\s*$", RegexOptions.CultureInvariant);
                if (relation.Success) {
                    relations[i + 1] = (relation.Groups["kind"].Value, sourceSpan);
                    // A short internal label leaves room to retain both endpoint columns.
                    // Restore the semantic stereotype after parsing the shared operator.
                    var prefix = text.Substring(0, relation.Groups["arrow"].Index) + "-.->|x|";
                    body[i] = new string(' ', sourceSpan.Column - 1) + prefix
                        + new string(' ', relation.Groups["target"].Index - prefix.Length) + relation.Groups["target"].Value;
                }
            }
        }
        MermaidFlowchartParser.ParseStatements(document, body, header.Line + 1, result);
        foreach (var node in document.Nodes) {
            if (json.TryGetValue(node.Id, out var data)) {
                if (node.Shape != MermaidFlowchartNodeShape.Default || node.Text != null)
                    MermaidParserUtilities.Add(result, data.Span, MermaidDiagnosticSeverity.Error,
                        "A UseCase JSON identifier cannot also declare a use case surface or caption.", MermaidDiagnosticCodes.InvalidStatement);
                if (actors.Contains(node.Id)) MermaidParserUtilities.Add(result, data.Span, MermaidDiagnosticSeverity.Error,
                    "A UseCase JSON vertex cannot share an actor identifier.", MermaidDiagnosticCodes.InvalidStatement);
                node.Shape = MermaidFlowchartNodeShape.Rectangle; node.Text = node.Id;
                node.Classes.AddRange(data.Classes);
                node.Properties[MermaidUseCaseJson.SourceProperty] = data.Source;
                node.PropertySpans[MermaidUseCaseJson.SourceProperty] = data.Span;
            }
            else if (actors.Contains(node.Id)) node.Shape = MermaidFlowchartNodeShape.Actor;
            else if (node.Shape is MermaidFlowchartNodeShape.Rounded or MermaidFlowchartNodeShape.Default) node.Shape = MermaidFlowchartNodeShape.Ellipse;
        }
        foreach (var edge in document.Edges) {
            var semantic = relations.TryGetValue(edge.Span.Line, out var relation);
            if (json.ContainsKey(edge.SourceId) || json.ContainsKey(edge.TargetId)) {
                if (semantic || !Regex.IsMatch(edge.Operator, @"^(?:--+>|<--+|--+)$", RegexOptions.CultureInvariant))
                    MermaidParserUtilities.Add(result, edge.Span, MermaidDiagnosticSeverity.Error,
                        "UseCase JSON relationships permit only point, reversed-point or markerless solid associations.", MermaidDiagnosticCodes.InvalidStatement);
                continue;
            }
            if (!semantic && edge.Operator != "--|>") continue;
            var relationSpan = semantic ? relation.Span : edge.Span;
            var from = document.Nodes.Find(node => node.Id == edge.SourceId);
            var to = document.Nodes.Find(node => node.Id == edge.TargetId);
            if (edge.Operator == "--|>") {
                if (from == null || to == null || (from.Shape == MermaidFlowchartNodeShape.Actor) != (to.Shape == MermaidFlowchartNodeShape.Actor))
                    MermaidParserUtilities.Add(result, relationSpan, MermaidDiagnosticSeverity.Error, "Use case generalization endpoints must both be actors or both be use cases.");
            }
            else {
                edge.Label = "<<" + relation.Kind + ">>";
                if (from == null || to == null || from.Shape == MermaidFlowchartNodeShape.Actor || to.Shape == MermaidFlowchartNodeShape.Actor)
                    MermaidParserUtilities.Add(result, relationSpan, MermaidDiagnosticSeverity.Error, "Use case include and extend relationships require use case endpoints.");
            }
        }
        MermaidFlowchartParser.CompleteUseCase(document, result);
        foreach (var note in document.Notes) {
            if (!document.Nodes.Exists(node => node.Id == note.NodeId)) MermaidParserUtilities.Add(result, note.Span,
                MermaidDiagnosticSeverity.Error, "UseCase notes must attach to an existing actor, use case or JSON vertex.", MermaidDiagnosticCodes.InvalidStatement);
        }
        // The shared parser consumes equivalent graph tokens. Keep actual physical source
        // statements instead of publishing transformed actor/boundary/relation text.
        document.Statements.Clear();
        foreach (var statement in MermaidFlowchartStatements.Read(lines, header.Line + 1, 0))
            if (!MermaidParserUtilities.IsSkippable(statement.Text)) document.Statements.Add(statement);
        return document;
    }

    private static bool TryUseCaseNote(MermaidUseCaseDocument document, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result) {
        if (!MermaidParserUtilities.StartsStatement(text, "note")) return false;
        var match = Regex.Match(text, @"^note[\t ]+for[\t ]+(\w+)[\t ]+(.+)$", RegexOptions.CultureInvariant);
        if (!match.Success) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "UseCase notes require 'note for ID caption'.", MermaidDiagnosticCodes.InvalidStatement); return true;
        }
        var raw = match.Groups[2].Value;
        if ((raw[0] == '"' || raw[0] == '`') && (raw.Length < 2 || raw[raw.Length - 1] != raw[0]
            || MermaidFlowchartText.IsEscaped(raw, raw.Length - 1))) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "UseCase note captions must close their authored quote.", MermaidDiagnosticCodes.InvalidStatement); return true;
        }
        var captionSpan = new MermaidSourceSpan(span.Line, span.Column + match.Groups[2].Index, raw.Length);
        MermaidLabelText.Diagnose(raw, captionSpan, result, "UseCase note");
        MermaidResourceGuard.ReserveElements(1, span);
        document.Notes.Add(new MermaidDiagramNote(MermaidNotationText.Note(raw), match.Groups[1].Value, false, span));
        return true;
    }
}
