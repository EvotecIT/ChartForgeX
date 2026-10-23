using System;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidZenUmlParser {
    private sealed partial class ParserState {
        private MermaidZenUmlStatement ReadStatement(Token token, Occurrence occurrence, int depth) {
            if (token.Kind == TokenKind.Open) {
                var anonymous = New(MermaidZenUmlStatementKind.Section, string.Empty, token, "frame");
                anonymous.HasBody = true; ReadBody(anonymous.Children, occurrence, depth + 1, true, token.Span); return anonymous;
            }
            if (token.Kind != TokenKind.Text) throw Syntax("ZenUML statement expected.", token.Span);
            var text = token.Text;
            if (IsKeyword(text, "if")) return Fragment(token, occurrence, depth, MermaidZenUmlStatementKind.Conditional, "if");
            foreach (var keyword in new[] { "while", "for", "foreach", "forEach", "loop" })
                if (IsKeyword(text, keyword)) return Fragment(token, occurrence, depth, MermaidZenUmlStatementKind.Loop, keyword);
            if (IsKeyword(text, "opt")) return Fragment(token, occurrence, depth, MermaidZenUmlStatementKind.Optional, "opt");
            if (IsKeyword(text, "par")) return Fragment(token, occurrence, depth, MermaidZenUmlStatementKind.Parallel, "par");
            if (IsKeyword(text, "critical")) return Fragment(token, occurrence, depth, MermaidZenUmlStatementKind.Critical, "critical");
            if (IsKeyword(text, "section")) return Fragment(token, occurrence, depth, MermaidZenUmlStatementKind.Section, "section");
            if (IsKeyword(text, "frame")) return Fragment(token, occurrence, depth, MermaidZenUmlStatementKind.Section, "frame");
            if (text == "try") return Fragment(token, occurrence, depth, MermaidZenUmlStatementKind.Try, "try");
            if (IsKeyword(text, "else") || IsKeyword(text, "catch") || text == "finally") throw Syntax("ZenUML branch has no matching if or try fragment.", token.Span);
            if (IsKeyword(text, "return")) {
                if (!occurrence.Active) throw Syntax("ZenUML return requires an enclosing call occurrence.", token.Span);
                MermaidResourceGuard.ReserveEdges(1, token.Span);
                return new MermaidZenUmlStatement(MermaidZenUmlStatementKind.Return, text.Substring(6).Trim(), token.Span) {
                    CallerId = occurrence.Target, TargetId = occurrence.Caller, Keyword = "return"
                };
            }
            if (IsKeyword(text, "ref")) {
                var names = Condition(text, "ref", token.Span);
                if (names.Length == 0) throw Syntax("ZenUML ref requires a diagram name.", token.Span);
                Unsupported(text, token.Span, "reference-frame presentation");
                return New(MermaidZenUmlStatementKind.Reference, names, token, "ref");
            }
            if (text.StartsWith("==", StringComparison.Ordinal)) {
                if (token.Span.Column != 1) throw Syntax("ZenUML dividers must start at column one.", token.Span);
                var label = text.Substring(2).Trim();
                if (label.EndsWith("==", StringComparison.Ordinal)) label = label.Substring(0, label.Length - 2).TrimEnd();
                Unsupported(text, token.Span, "divider-line presentation");
                return New(MermaidZenUmlStatementKind.Divider, label, token, "divider");
            }
            return Message(token, occurrence, depth);
        }

        private MermaidZenUmlStatement Fragment(Token token, Occurrence occurrence, int depth, MermaidZenUmlStatementKind kind, string keyword) {
            var condition = keyword == "try" ? string.Empty : Condition(token.Text, keyword, token.Span);
            var statement = New(kind, condition, token, keyword); statement.HasBody = true;
            Open(token.Span); ReadBody(statement.Children, occurrence, depth + 1, true, token.Span);
            var terminalBranch = false;
            while (_index < _tokens.Count) {
                if (_tokens[_index].Kind == TokenKind.Comment) { Comment(_tokens[_index++]); continue; }
                var branch = _tokens[_index];
                if (branch.Kind != TokenKind.Text) break;
                string? branchKind = null; string branchText = string.Empty;
                if (kind == MermaidZenUmlStatementKind.Conditional && IsKeyword(branch.Text, "else")) {
                    if (terminalBranch) throw Syntax("ZenUML else must be the final alternative.", branch.Span);
                    var remaining = branch.Text.Substring(4).Trim();
                    if (remaining.Length == 0) { branchKind = "else"; terminalBranch = true; }
                    else if (IsKeyword(remaining, "if")) { branchKind = "else-if"; branchText = Condition(remaining, "if", branch.Span); }
                    else throw Syntax("Invalid ZenUML else branch.", branch.Span);
                } else if (kind == MermaidZenUmlStatementKind.Try && IsKeyword(branch.Text, "catch")) {
                    if (terminalBranch) throw Syntax("ZenUML catch cannot follow finally.", branch.Span);
                    branchKind = "catch"; branchText = Condition(branch.Text, "catch", branch.Span);
                } else if (kind == MermaidZenUmlStatementKind.Try && branch.Text == "finally") {
                    if (terminalBranch) throw Syntax("ZenUML finally may occur only once.", branch.Span);
                    branchKind = "finally"; terminalBranch = true;
                }
                if (branchKind == null) break;
                _index++; Open(branch.Span); MermaidResourceGuard.ReserveElements(1, branch.Span);
                var body = new MermaidZenUmlBranch(branchKind, branchText, branch.Span);
                ReadBody(body.Statements, occurrence, depth + 1, true, branch.Span); statement.Branches.Add(body);
            }
            if (kind == MermaidZenUmlStatementKind.Try && statement.Branches.Count == 0) throw Syntax("ZenUML try requires catch or finally.", token.Span);
            return statement;
        }

        private MermaidZenUmlStatement Message(Token token, Occurrence occurrence, int depth) {
            var text = token.Text; var markedReturn = false;
            foreach (var annotation in new[] { "@Return", "@return", "@Reply", "@reply" }) {
                if (!text.StartsWith(annotation + " ", StringComparison.Ordinal)) continue;
                markedReturn = true; text = text.Substring(annotation.Length + 1).TrimStart(); break;
            }
            var colon = FindTopLevel(text, ":");
            var assignment = FindTopLevel(text, "="); string? capture = null;
            if (assignment >= 0 && (colon < 0 || assignment < colon)) {
                capture = text.Substring(0, assignment).Trim();
                if (!ValidAssignment(capture)) throw Syntax("ZenUML assignment requires a variable, optionally preceded by a type or modifier.", token.Span);
                text = text.Substring(assignment + 1).Trim(); colon = FindTopLevel(text, ":");
            }
            var arrow = FindTopLevel(text, "-->"); var returning = arrow >= 0;
            if (arrow < 0) arrow = FindTopLevel(text, "->");
            if (colon >= 0 && arrow > colon) { arrow = -1; returning = false; }
            string? caller = occurrence.Target ?? _document.StarterId;
            if (arrow >= 0) {
                caller = text.Substring(0, arrow).Trim();
                if (!IsIdentity(caller)) throw Syntax("ZenUML explicit sender requires a participant identity.", token.Span);
                Participant(caller, token.Span, true);
                if (occurrence.Target == null && _document.StarterId == null) _document.StarterId = caller;
                text = text.Substring(arrow + (returning ? 3 : 2)).Trim(); colon = FindTopLevel(text, ":");
            }
            MermaidZenUmlStatement statement;
            if (colon >= 0) {
                if (capture != null) throw Syntax("ZenUML asynchronous messages do not capture a synchronous return.", token.Span);
                var target = text.Substring(0, colon).Trim();
                if (!IsIdentity(target)) throw Syntax("ZenUML asynchronous target requires a participant identity.", token.Span);
                var message = text.Substring(colon + 1).Trim();
                statement = new MermaidZenUmlStatement(returning || markedReturn ? MermaidZenUmlStatementKind.Return : MermaidZenUmlStatementKind.Async, message, token.Span) {
                    CallerId = caller, TargetId = target, Keyword = returning || markedReturn ? "reply" : "async"
                };
                if (HasOpen) throw Syntax("ZenUML asynchronous messages cannot open synchronous occurrence bodies.", token.Span);
            } else {
                if (returning || markedReturn) throw Syntax("ZenUML annotated and dashed replies require asynchronous colon syntax.", token.Span);
                var creating = text.StartsWith("new ", StringComparison.Ordinal);
                if (creating) text = text.Substring(4).TrimStart();
                var parentheses = FindTopLevel(text, "(");
                if (parentheses <= 0 || MatchingClose(text, parentheses) != text.Length - 1) throw Syntax("ZenUML synchronous calls require one complete method argument list.", token.Span);
                var callable = text.Substring(0, parentheses).Trim();
                var dot = FindTopLevel(callable, "."); string? target; string message;
                if (creating) { target = callable; message = "new " + text; }
                else if (dot > 0) { target = callable.Substring(0, dot).Trim(); message = text.Substring(dot + 1).TrimStart(); }
                else { target = occurrence.Target ?? _document.StarterId; message = text; }
                var local = !creating && dot < 0;
                if ((target != null && !IsIdentity(target)) || (!local && target == null) || callable.Length == 0 || callable.IndexOf(' ') >= 0)
                    throw Syntax("ZenUML call requires a target participant, or an enclosing occurrence for a local call.", token.Span);
                statement = new MermaidZenUmlStatement(creating ? MermaidZenUmlStatementKind.Create : MermaidZenUmlStatementKind.Call, message, token.Span) {
                    CallerId = caller, TargetId = target, Assignment = capture, Keyword = creating ? "new" : local ? "local-call" : "call"
                };
                if (target != null) Participant(target, token.Span, true);
                if (HasOpen) {
                    _index++; statement.HasBody = true;
                    ReadBody(statement.Children, new Occurrence(caller, target, true), depth + 1, true, token.Span);
                }
            }
            if (statement.TargetId != null) Participant(statement.TargetId, token.Span, true);
            MermaidResourceGuard.ReserveEdges(1, token.Span);
            return statement;
        }

        private static MermaidZenUmlStatement New(MermaidZenUmlStatementKind kind, string text, Token token, string keyword) => new(kind, text, token.Span) { Keyword = keyword };
        private static bool ValidAssignment(string text) {
            if (text.Length == 0) return false;
            foreach (var ch in text) if (!char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch) && ch is not '_' and not '$' and not '.' and not '<' and not '>' and not '[' and not ']' and not ',' and not '?') return false;
            return true;
        }
    }
}
