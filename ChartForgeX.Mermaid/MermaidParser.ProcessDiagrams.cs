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
        var relations = new Dictionary<int, string>();
        var boundaryDepth = 0;
        for (var i = header.Line; i < body.Length; i++) {
            var text = body[i].Trim();
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
                var relation = Regex.Match(text, @"^(\w+)\s+\.\.>\s*:\s*(include|extend)\s+(\w+)\s*$", RegexOptions.CultureInvariant);
                if (relation.Success) {
                    relations[i + 1] = relation.Groups[2].Value;
                    body[i] = relation.Groups[1].Value + " -.->|<<" + relation.Groups[2].Value + ">>| " + relation.Groups[3].Value;
                } else if (text.Contains("--|>")) {
                    relations[i + 1] = "generalization";
                    body[i] = body[i].Replace("--|>", "-->");
                }
            }
        }
        MermaidFlowchartParser.ParseStatements(document, body, header.Line + 1, result);
        foreach (var node in document.Nodes) {
            if (actors.Contains(node.Id)) node.Shape = MermaidFlowchartNodeShape.Actor;
            else if (node.Shape is MermaidFlowchartNodeShape.Rounded or MermaidFlowchartNodeShape.Default) node.Shape = MermaidFlowchartNodeShape.Ellipse;
        }
        foreach (var edge in document.Edges) if (relations.TryGetValue(edge.Span.Line, out var kind)) {
            if (kind == "generalization") {
                edge.Operator = "--|>";
                if (actors.Contains(edge.SourceId) != actors.Contains(edge.TargetId)) MermaidParserUtilities.Add(result, edge.Span, MermaidDiagnosticSeverity.Error, "Use case generalization endpoints must both be actors or both be use cases.");
            }
            else {
                var from = document.Nodes.Find(node => node.Id == edge.SourceId)!;
                var to = document.Nodes.Find(node => node.Id == edge.TargetId)!;
                if (from.Shape == MermaidFlowchartNodeShape.Actor || to.Shape == MermaidFlowchartNodeShape.Actor)
                    MermaidParserUtilities.Add(result, edge.Span, MermaidDiagnosticSeverity.Error, "Use case include and extend relationships require use case endpoints.");
            }
        }
        return document;
    }

    private static MermaidCynefinDocument ParseCynefin(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidCynefinDocument {
            SourceText = source, Kind = MermaidDiagramKind.Cynefin, Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length), FrontMatter = frontMatter.Text
        };
        var domains = new HashSet<string>(StringComparer.Ordinal) { "complex", "complicated", "chaotic", "clear", "confusion" };
        string? current = null;
        for (var i = header.Line; i < lines.Length; i++) {
            var text = lines[i].Trim();
            if (MermaidParserUtilities.IsSkippable(text)) continue;
            var span = new MermaidSourceSpan(i + 1, MermaidParserUtilities.LeadingWhitespace(lines[i]) + 1, text.Length);
            document.RawStatements.Add(new MermaidRawStatement(text, span));
            if (text.StartsWith("title ", StringComparison.Ordinal)) document.Title = text.Substring(6).Trim().Trim('"');
            else if (domains.Contains(text)) {
                current = text;
                if (!document.Domains.ContainsKey(text)) document.Domains.Add(text, new List<string>());
            } else if (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal) && current != null) {
                document.Domains[current].Add(text.Substring(1, text.Length - 2));
            } else {
                var transition = Regex.Match(text, @"^(\w+)\s*-->\s*(\w+)(?:\s*:\s*""(.*)"")?\s*$", RegexOptions.CultureInvariant);
                if (transition.Success && domains.Contains(transition.Groups[1].Value) && domains.Contains(transition.Groups[2].Value)) {
                    if (transition.Groups[1].Value != transition.Groups[2].Value) document.Transitions.Add(new MermaidFlowchartEdge(transition.Groups[1].Value, transition.Groups[2].Value, "-->", span) { Label = transition.Groups[3].Value });
                } else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Cynefin statements require a domain, quoted item, title, or domain transition.");
            }
        }
        return document;
    }
}
