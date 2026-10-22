using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidEntityRelationshipParser {
    private static bool TryGroup(MermaidEntityRelationshipDocument document, string text, MermaidSourceSpan span,
        Stack<MermaidEntityGroup> parents, MermaidParseResult<MermaidDocument> result) {
        if (!MermaidParserUtilities.StartsStatement(text, "subgraph")) return false;
        var title = text.Substring(8).Trim();
        var id = MermaidParserUtilities.Unquote(title);
        var label = id;
        if (MermaidParserUtilities.TryBracketed(title, out var groupId, out var display, out _)) {
            id = MermaidParserUtilities.Unquote(groupId);
            label = MermaidParserUtilities.Unquote(display);
        }
        if (id.Length == 0 || document.Groups.Exists(group => group.Id == id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "ER groups require distinct nonempty identities.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        MermaidResourceGuard.CheckDepth(parents.Count + 1, span);
        var group = new MermaidEntityGroup(id, label, span) { ParentId = parents.Count == 0 ? null : parents.Peek().Id };
        document.Groups.Add(group);
        parents.Push(group);
        return true;
    }

    private static void GroupMember(MermaidEntityNode node, Stack<MermaidEntityGroup> groups) {
        if (groups.Count == 0) return;
        // Inner groups close before their ancestors in Mermaid's grammar. Their
        // members win over an ancestor declaration; a later sibling cannot steal them.
        if (node.GroupId == null) { node.GroupId = groups.Peek().Id; return; }
        foreach (var ancestor in groups) if (ancestor.Id == node.GroupId) {
            node.GroupId = groups.Peek().Id;
            return;
        }
    }

    // A relationship can refer to a later group declaration. Remove only its empty implicit endpoint,
    // while a real aliased/attribute-bearing entity with that identity is an invalid ambiguity.
    private static void ResolveGroupEndpoints(MermaidEntityRelationshipDocument document, MermaidParseResult<MermaidDocument> result) {
        foreach (var group in document.Groups) {
            var node = document.Entities.Find(entity => entity.Id == group.Id);
            if (node == null) continue;
            if (node.Attributes.Count > 0 || node.Label != null)
                MermaidParserUtilities.Add(result, group.Span, MermaidDiagnosticSeverity.Error,
                    "ER groups and attribute-bearing or aliased entities must have different identities.", MermaidDiagnosticCodes.InvalidStatement);
            else document.Entities.Remove(node);
        }
    }
}
