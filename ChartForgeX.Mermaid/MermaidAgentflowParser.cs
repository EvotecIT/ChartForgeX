using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidAgentflowParser {
    private sealed class Scope {
        internal Scope(MermaidAgentflowFlow? flow, bool global, MermaidSourceSpan span) { Flow = flow; Global = global; Span = span; }
        internal MermaidAgentflowFlow? Flow { get; }
        internal bool Global { get; }
        internal MermaidSourceSpan Span { get; }
    }
    private sealed class Attachment {
        internal Attachment(string id, MermaidMetadataValue data, MermaidSourceSpan span) { Id = id; Data = data; Span = span; }
        internal string Id { get; }
        internal MermaidMetadataValue Data { get; }
        internal MermaidSourceSpan Span { get; }
    }
    internal static void ParseStatements(MermaidAgentflowDocument document, string[] lines, int firstLine, MermaidParseResult<MermaidDocument> result) {
        var scopes = new Stack<Scope>();
        var nodes = new Dictionary<string, MermaidAgentflowNode>(StringComparer.Ordinal);
        var attachments = new List<Attachment>();
        var linkStyles=new List<MermaidFlowchartLinkStyle>();
        for (var line = firstLine; line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[line - 1];
            var text = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(text)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, text.Length);
            if(TryStyle(document,nodes,linkStyles,text,span,result))continue;
            if (text == "end") {
                if (scopes.Count == 0) Error("Agentflow end has no open flow or global block.");
                else scopes.Pop();
                continue;
            }
            if (text == "global") { MermaidResourceGuard.CheckDepth(scopes.Count + 1, span); scopes.Push(new Scope(null, true, span)); continue; }
            if (MermaidParserUtilities.StartsStatement(text, "global")) { Error("Agentflow global blocks do not accept an identity, label or metadata."); continue; }
            if (MermaidParserUtilities.TryReadDirection(text, span, result, out var direction)) {
                if (scopes.Count > 0 && scopes.Peek().Flow != null) scopes.Peek().Flow!.Direction = direction;
                else document.Direction = direction;
                continue;
            }
            var flow = MermaidParserUtilities.StartsStatement(text, "flow");
            var connector = MermaidParserUtilities.StartsStatement(text, "connector");
            var position = flow ? 4 : connector ? 9 : 0;
            var parent = Parent(scopes);
            var global = IsGlobal(scopes);
            if (!Endpoint(document, nodes, attachments, lines, ref line, text, ref position, span, parent, global, result, out var current)) continue;
            if (flow) {
                if (document.Flows.Exists(item => item.Id == current!.Id)) { Error("Agentflow flow identities must be unique."); continue; }
                var item = new MermaidAgentflowFlow(current!.Id, span) { Label = current.Label, ParentId = global ? null : parent };
                item.Classes.AddRange(current.Classes);MermaidStyleParser.Copy(item.Styles,current.Styles,span);
                item.SurfaceOpacitySpan = current.SurfaceOpacitySpan;
                document.Flows.Add(item);
                MermaidResourceGuard.ReserveElements(1, span);
                MermaidResourceGuard.CheckDepth(scopes.Count + 1, span);
                scopes.Push(new Scope(item, false, span));
            } else if (connector) { current!.IsConnector = true; current.IsGlobal = true; current.ParentFlowId = null; }
            while (!flow && !connector && position < text.Length) {
                White(text, ref position);
                if (position == text.Length || text[position] == ';') break;
                var id = "agentflow-edge-" + document.Edges.Count.ToString(CultureInfo.InvariantCulture);
                if (!Operator(text, ref position, ref id, out var kind, out var label)) { Error("Agentflow relationships require '-->', '-.-' or '--x'."); break; }
                if (!Endpoint(document, nodes, attachments, lines, ref line, text, ref position, span, parent, global, result, out var target)) break;
                if (document.Edges.Exists(item => item.Id == id)) { Error("Agentflow relationship identities must be unique."); break; }
                MermaidResourceGuard.ReserveEdges(1, span);
                document.Edges.Add(new MermaidAgentflowEdge(id, current!.Id, target!.Id, kind, span) { Label = label });
                current = target;
            }
            White(text, ref position);
            if (position < text.Length && text.Substring(position).Trim() != ";") Error("Unexpected text after Agentflow statement.");
            void Error(string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
        }
        foreach (var opening in scopes) MermaidParserUtilities.Add(result, opening.Span, MermaidDiagnosticSeverity.Error,
            "Agentflow flow and global blocks must close with end.", MermaidDiagnosticCodes.InvalidStatement);
        ApplyAttachments(document, attachments, result);
        document.Nodes.RemoveAll(node => document.Flows.Exists(flow => flow.Id == node.Id) || document.Edges.Exists(edge => edge.Id == node.Id));
        ApplyStyles(document,linkStyles,result);
        foreach (var node in document.Nodes) if (node.IsGlobal) node.ParentFlowId = null;
        if (document.Nodes.Count == 0 && document.Flows.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error,
            "Agentflow diagrams require a node or flow.", MermaidDiagnosticCodes.InvalidStatement);
    }
    private static string? Parent(Stack<Scope> scopes) {
        foreach (var scope in scopes) { if (scope.Global) return null; if (scope.Flow != null) return scope.Flow.Id; }
        return null;
    }
    private static bool IsGlobal(Stack<Scope> scopes) { foreach (var scope in scopes) if (scope.Global) return true; return false; }
    private static void White(string text, ref int position) { while (position < text.Length && char.IsWhiteSpace(text[position])) position++; }
}
