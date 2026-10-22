using System;
using System.Collections.Generic;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Validates a source or host-authored occurrence tree and counts its actual native expansion.</summary>
internal static class MermaidZenUmlModel {
    internal static void Validate(MermaidZenUmlDocument document,
        Action<long, MermaidSourceSpan>? elements = null, Action<long, MermaidSourceSpan>? edges = null) {
        var participants = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var participant in document.Participants) {
            MermaidResourceGuard.Checkpoint();
            if (participant == null || participants.ContainsKey(participant.Id)) throw MermaidZenUmlParser.Syntax("ZenUML participant identities must be unique.", document.HeaderSpan);
            participants.Add(participant.Id, participants.Count);
            if (!Enum.IsDefined(typeof(SequenceArtifactParticipantKind), participant.Kind)) throw MermaidZenUmlParser.Syntax("Unknown ZenUML participant notation.", participant.Span);
            elements?.Invoke(1 + participant.Attributes.Count, participant.Span);
        }
        if (document.StarterId != null && !participants.ContainsKey(document.StarterId)) throw MermaidZenUmlParser.Syntax("ZenUML starter is not a declared or inferred participant.", document.HeaderSpan);
        var grouped = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in document.ParticipantGroups) {
            MermaidResourceGuard.Checkpoint(); elements?.Invoke(1 + group.ParticipantIds.Count, group.Span);
            if (group.ParticipantIds.Count == 0) throw MermaidZenUmlParser.Syntax("ZenUML participant group cannot be empty.", group.Span);
            var last = -1;
            foreach (var id in group.ParticipantIds) {
                if (!participants.TryGetValue(id, out var current) || !grouped.Add(id) || last >= 0 && current != last + 1)
                    throw MermaidZenUmlParser.Syntax("ZenUML participant groups require distinct, contiguous existing participants.", group.Span);
                last = current;
            }
        }
        var used = new HashSet<string>(StringComparer.Ordinal); var created = new HashSet<string>(StringComparer.Ordinal);
        var activePath = new HashSet<MermaidZenUmlStatement>(); var external = false;
        Walk(document.Statements, null, null, false, 0);
        if (external) elements?.Invoke(1, document.HeaderSpan);

        void Walk(List<MermaidZenUmlStatement> statements, string? occurrenceCaller, string? occurrenceTarget, bool occurrenceActive, int depth) {
            foreach (var statement in statements) {
                MermaidResourceGuard.Checkpoint();
                if (statement == null || !activePath.Add(statement)) throw MermaidZenUmlParser.Syntax("ZenUML statements must form an acyclic occurrence tree.", document.HeaderSpan);
                MermaidResourceGuard.CheckDepth(depth, statement.Span);
                if (depth > 100) throw MermaidZenUmlParser.Syntax("Native ZenUML fragments support at most 100 nested levels.", statement.Span);
                elements?.Invoke(1 + statement.Comments.Count, statement.Span);
                switch (statement.Kind) {
                    case MermaidZenUmlStatementKind.Call:
                    case MermaidZenUmlStatementKind.Create:
                    case MermaidZenUmlStatementKind.Async:
                    case MermaidZenUmlStatementKind.Return:
                        Endpoint(statement.CallerId, true, statement.Span);
                        Endpoint(statement.TargetId, statement.Kind == MermaidZenUmlStatementKind.Return ||
                            statement.Kind == MermaidZenUmlStatementKind.Call && statement.Keyword == "local-call", statement.Span);
                        if (statement.CallerId == null && statement.TargetId == null &&
                            !(statement.Kind == MermaidZenUmlStatementKind.Call && statement.Keyword == "local-call" ||
                              statement.Kind == MermaidZenUmlStatementKind.Return && statement.Keyword == "return" && occurrenceActive))
                            throw MermaidZenUmlParser.Syntax("ZenUML messages require a concrete participant or an explicit local external context.", statement.Span);
                        if (statement.Kind == MermaidZenUmlStatementKind.Return && statement.Keyword == "return" &&
                            (!occurrenceActive || statement.CallerId != occurrenceTarget || statement.TargetId != occurrenceCaller))
                            throw MermaidZenUmlParser.Syntax("ZenUML return must address its current occurrence's caller.", statement.Span);
                        if (statement.Kind == MermaidZenUmlStatementKind.Create &&
                            (statement.CallerId == statement.TargetId || used.Contains(statement.TargetId!) || !created.Add(statement.TargetId!)))
                            throw MermaidZenUmlParser.Syntax("ZenUML participant creation must precede every use and occur only once.", statement.Span);
                        if (statement.CallerId != null) used.Add(statement.CallerId);
                        if (statement.TargetId != null) used.Add(statement.TargetId);
                        edges?.Invoke(1, statement.Span);
                        if (statement.Kind is MermaidZenUmlStatementKind.Call or MermaidZenUmlStatementKind.Create) {
                            elements?.Invoke(2, statement.Span);
                            if (statement.Assignment != null && !HasReturn(statement.Children)) edges?.Invoke(1, statement.Span);
                            Walk(statement.Children, statement.CallerId, statement.TargetId, true, depth + 1);
                        } else if (statement.Children.Count != 0 || statement.HasBody) throw MermaidZenUmlParser.Syntax("ZenUML replies and asynchronous messages cannot own call occurrences.", statement.Span);
                        if (statement.Branches.Count != 0) throw MermaidZenUmlParser.Syntax("ZenUML messages cannot own alternative branches.", statement.Span);
                        break;
                    case MermaidZenUmlStatementKind.Conditional:
                    case MermaidZenUmlStatementKind.Try:
                    case MermaidZenUmlStatementKind.Loop:
                    case MermaidZenUmlStatementKind.Optional:
                    case MermaidZenUmlStatementKind.Parallel:
                    case MermaidZenUmlStatementKind.Critical:
                    case MermaidZenUmlStatementKind.Section:
                        Walk(statement.Children, occurrenceCaller, occurrenceTarget, occurrenceActive, depth + 1);
                        var terminal = false;
                        foreach (var branch in statement.Branches) {
                            elements?.Invoke(1, branch.Span);
                            var conditional = statement.Kind == MermaidZenUmlStatementKind.Conditional;
                            var exception = statement.Kind == MermaidZenUmlStatementKind.Try;
                            if (terminal || !(conditional && (branch.Kind == "else" || branch.Kind == "else-if") || exception && (branch.Kind == "catch" || branch.Kind == "finally")))
                                throw MermaidZenUmlParser.Syntax("ZenUML branch kind or ordering does not match its enclosing fragment.", branch.Span);
                            terminal = branch.Kind == "else" || branch.Kind == "finally";
                            Walk(branch.Statements, occurrenceCaller, occurrenceTarget, occurrenceActive, depth + 1);
                        }
                        if (statement.Kind == MermaidZenUmlStatementKind.Try && statement.Branches.Count == 0) throw MermaidZenUmlParser.Syntax("ZenUML try requires catch or finally.", statement.Span);
                        break;
                    case MermaidZenUmlStatementKind.Reference:
                    case MermaidZenUmlStatementKind.Divider:
                        if (statement.Children.Count != 0 || statement.Branches.Count != 0) throw MermaidZenUmlParser.Syntax("ZenUML reference and divider statements cannot own nested occurrences.", statement.Span);
                        break;
                    default: throw MermaidZenUmlParser.Syntax("Unknown ZenUML statement kind.", statement.Span);
                }
                activePath.Remove(statement);
            }
        }
        void Endpoint(string? id, bool allowExternal, MermaidSourceSpan span) {
            if (id == null && allowExternal) { external = true; return; }
            if (id == null || !participants.ContainsKey(id)) throw MermaidZenUmlParser.Syntax("ZenUML message endpoint is not a declared or inferred participant.", span);
        }
    }

    internal static bool HasReturn(List<MermaidZenUmlStatement> statements) {
        var pending = new Stack<MermaidZenUmlStatement>(statements); var seen = new HashSet<MermaidZenUmlStatement>();
        while (pending.Count > 0) {
            var statement = pending.Pop();
            MermaidResourceGuard.Checkpoint();
            if (statement == null) continue;
            if (!seen.Add(statement)) continue;
            if (statement.Kind == MermaidZenUmlStatementKind.Return && statement.Keyword == "return") return true;
            if (statement.Kind is MermaidZenUmlStatementKind.Call or MermaidZenUmlStatementKind.Create) continue;
            foreach (var child in statement.Children) pending.Push(child);
            foreach (var branch in statement.Branches) foreach (var child in branch.Statements) pending.Push(child);
        }
        return false;
    }
}
