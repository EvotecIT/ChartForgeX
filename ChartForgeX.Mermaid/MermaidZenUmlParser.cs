using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Parses a bounded occurrence grammar directly; no sequence-text translation or expression execution occurs.</summary>
internal static partial class MermaidZenUmlParser {
    internal static void ParseStatements(MermaidZenUmlDocument document, string[] lines, int firstLine, MermaidParseResult<MermaidDocument> result) {
        try {
            var parser = new ParserState(document, Tokenize(lines, firstLine), result);
            parser.ReadBody(document.Statements, new Occurrence(null, null), 0, false);
            MermaidZenUmlModel.Validate(document);
        } catch (SyntaxException exception) {
            document.ContainsSyntaxErrors = true;
            MermaidParserUtilities.Add(result, exception.Span, MermaidDiagnosticSeverity.Error, exception.Message, MermaidDiagnosticCodes.InvalidStatement);
        }
    }

    private readonly struct Occurrence {
        internal Occurrence(string? caller, string? target, bool active = false) { Caller = caller; Target = target; Active = active; }
        internal string? Caller { get; }
        internal string? Target { get; }
        internal bool Active { get; }
    }

    private sealed partial class ParserState {
        private readonly MermaidZenUmlDocument _document;
        private readonly List<Token> _tokens;
        private readonly MermaidParseResult<MermaidDocument> _result;
        private readonly List<MermaidRawStatement> _comments = new();
        private int _index;
        private bool _seenHeadToken;
        private bool _seenInteraction;

        internal ParserState(MermaidZenUmlDocument document, List<Token> tokens, MermaidParseResult<MermaidDocument> result) {
            _document = document; _tokens = tokens; _result = result;
        }

        internal void ReadBody(List<MermaidZenUmlStatement> statements, Occurrence occurrence, int depth, bool closeRequired, MermaidSourceSpan? opening = null) {
            MermaidResourceGuard.CheckDepth(depth, _document.HeaderSpan);
            while (_index < _tokens.Count) {
                MermaidResourceGuard.Checkpoint(); var token = _tokens[_index++];
                if (token.Kind == TokenKind.Comment) { Comment(token); continue; }
                if (token.Kind == TokenKind.Close) {
                    if (!closeRequired) throw Syntax("ZenUML closing brace has no open occurrence or fragment.", token.Span);
                    if (_comments.Count > 0) { RetainTrailingComments(); }
                    return;
                }
                if (token.Kind == TokenKind.Text && depth == 0 && TryHead(token)) continue;
                _seenHeadToken = true; _seenInteraction = true;
                var comments = _comments.ToArray(); _comments.Clear();
                var statement = ReadStatement(token, occurrence, depth);
                MermaidResourceGuard.ReserveElements(1, token.Span);
                statement.Comments.AddRange(comments);
                statements.Add(statement);
            }
            if (closeRequired) throw Syntax("ZenUML occurrence or fragment is missing its closing brace.", opening ?? _document.HeaderSpan);
            RetainTrailingComments();
        }

        private bool TryHead(Token token) {
            var text = token.Text;
            if (text.StartsWith("title ", StringComparison.Ordinal)) {
                if (_seenHeadToken) throw Syntax("ZenUML title must be the first non-comment statement.", token.Span);
                _document.Title = text.Substring(6).Trim();
                _seenHeadToken = true; return true;
            }
            if (text.StartsWith("@Starter(", StringComparison.Ordinal) || text.StartsWith("@starter(", StringComparison.Ordinal)) {
                if (_seenInteraction || _document.StarterId != null) throw Syntax("ZenUML starter must be declared once before interactions.", token.Span);
                var close = text.LastIndexOf(')');
                if (close != text.Length - 1) throw Syntax("Invalid ZenUML starter declaration.", token.Span);
                var id = text.Substring(9, text.Length - 10).Trim();
                if (!IsIdentity(id)) throw Syntax("ZenUML starter requires one participant identity.", token.Span);
                _document.StarterId = id; Participant(id, token.Span, true); _seenHeadToken = true; return true;
            }
            if (IsKeyword(text, "group")) {
                if (_seenInteraction) throw Syntax("ZenUML participant groups must precede interactions.", token.Span);
                ReadGroup(token); _seenHeadToken = true; return true;
            }
            if (IsParticipantDeclaration(text)) {
                if (_seenInteraction) throw Syntax("ZenUML participant declarations must precede interactions.", token.Span);
                ReadParticipant(token, null); _seenHeadToken = true; return true;
            }
            return false;
        }

        private void ReadGroup(Token token) {
            var label = MermaidParserUtilities.Unquote(token.Text.Substring(5).Trim());
            if (label.Length == 0) throw Syntax("ZenUML participant group requires a label.", token.Span);
            Open(token.Span);
            var group = new MermaidZenUmlParticipantGroup(label, token.Span);
            MermaidResourceGuard.ReserveElements(1, token.Span);
            while (_index < _tokens.Count) {
                var member = _tokens[_index++]; MermaidResourceGuard.Checkpoint();
                if (member.Kind == TokenKind.Comment) { Comment(member); continue; }
                if (member.Kind == TokenKind.Close) {
                    if (group.ParticipantIds.Count == 0) throw Syntax("ZenUML participant group cannot be empty.", token.Span);
                    _document.ParticipantGroups.Add(group); return;
                }
                if (member.Kind != TokenKind.Text) throw Syntax("ZenUML groups contain participant declarations, not interactions or nested groups.", member.Span);
                ReadParticipant(member, group);
            }
            throw Syntax("ZenUML participant group is missing its closing brace.", token.Span);
        }

        private void Open(MermaidSourceSpan span) {
            if (_index >= _tokens.Count || _tokens[_index].Kind != TokenKind.Open) throw Syntax("ZenUML fragment or occurrence requires an opening brace.", span);
            _index++;
        }
        private bool HasOpen => _index < _tokens.Count && _tokens[_index].Kind == TokenKind.Open;
        private void Comment(Token token) {
            MermaidResourceGuard.ReserveElements(1, token.Span);
            _comments.Add(new MermaidRawStatement(token.Text, token.Span));
        }
        private void RetainTrailingComments() {
            foreach (var comment in _comments) _document.RawStatements.Add(comment);
            _comments.Clear();
        }
        private void Unsupported(string source, MermaidSourceSpan span, string reason) => MermaidParserUtilities.RetainUnsupported(_document, source, span, _result, "ZenUML " + reason);
    }
}
