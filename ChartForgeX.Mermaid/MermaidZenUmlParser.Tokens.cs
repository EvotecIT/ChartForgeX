using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidZenUmlParser {
    private enum TokenKind { Text, Open, Close, Comment }
    private readonly struct Token {
        internal Token(TokenKind kind, string text, MermaidSourceSpan span) { Kind = kind; Text = text; Span = span; }
        internal TokenKind Kind { get; }
        internal string Text { get; }
        internal MermaidSourceSpan Span { get; }
    }

    // Parenthesized expressions and quoted content are inert. Only braces outside
    // them create occurrences; neither string content nor comments enter dispatch.
    private static List<Token> Tokenize(string[] lines, int firstLine) {
        var tokens = new List<Token>(); var text = new StringBuilder();
        var startLine = firstLine; var startColumn = 1; var quote = '\0'; var escaped = false;
        var parentheses = 0; var brackets = 0; var asynchronousText = false;
        void Flush() {
            var value = text.ToString().TrimEnd();
            if (value.Length != 0) tokens.Add(new Token(TokenKind.Text, value, new MermaidSourceSpan(startLine, startColumn, value.Length)));
            text.Clear();
        }
        for (var lineNumber = firstLine; lineNumber <= lines.Length; lineNumber++) {
            var line = lines[lineNumber - 1];
            if (text.Length == 0 && line.TrimStart().StartsWith("%%", StringComparison.Ordinal)) continue;
            for (var column = 0; column < line.Length; column++) {
                MermaidResourceGuard.Checkpoint(); var ch = line[column];
                if (text.Length == 0 && quote == '\0' && char.IsWhiteSpace(ch)) continue;
                if (text.Length == 0) { startLine = lineNumber; startColumn = column + 1; }
                if (asynchronousText) { text.Append(ch); continue; }
                if (quote != '\0') {
                    text.Append(ch);
                    if (escaped) escaped = false;
                    else if (ch == '\\') escaped = true;
                    else if (ch == quote) quote = '\0';
                    continue;
                }
                if (ch is '"' or '\'' or '`') { quote = ch; text.Append(ch); continue; }
                if (ch == '/' && column + 1 < line.Length && line[column + 1] == '/' &&
                    (column == 0 || char.IsWhiteSpace(line[column - 1]))) {
                    Flush(); var comment = line.Substring(column + 2).Trim();
                    tokens.Add(new Token(TokenKind.Comment, comment, new MermaidSourceSpan(lineNumber, column + 1, line.Length - column)));
                    break;
                }
                if (ch == '(') { parentheses++; MermaidResourceGuard.CheckDepth(parentheses + brackets, new MermaidSourceSpan(lineNumber, column + 1, 1)); }
                else if (ch == ')') { if (--parentheses < 0) throw Syntax("Unmatched ZenUML closing parenthesis.", new MermaidSourceSpan(lineNumber, column + 1, 1)); }
                else if (ch == '[') { brackets++; MermaidResourceGuard.CheckDepth(parentheses + brackets, new MermaidSourceSpan(lineNumber, column + 1, 1)); }
                else if (ch == ']') { if (--brackets < 0) throw Syntax("Unmatched ZenUML closing bracket.", new MermaidSourceSpan(lineNumber, column + 1, 1)); }
                if (parentheses == 0 && brackets == 0 && ch == ':') asynchronousText = true;
                if (parentheses == 0 && brackets == 0 && (ch is '{' or '}')) {
                    Flush(); tokens.Add(new Token(ch == '{' ? TokenKind.Open : TokenKind.Close, ch.ToString(), new MermaidSourceSpan(lineNumber, column + 1, 1)));
                } else if (parentheses == 0 && brackets == 0 && ch == ';') Flush();
                else text.Append(ch);
            }
            if (quote != '\0') throw Syntax("ZenUML quoted content must close on its source line.", new MermaidSourceSpan(startLine, startColumn, text.Length));
            if (parentheses == 0 && brackets == 0) { Flush(); asynchronousText = false; } else text.Append('\n');
        }
        if (parentheses != 0 || brackets != 0) throw Syntax("Unclosed ZenUML argument or condition.", new MermaidSourceSpan(startLine, startColumn, text.Length));
        return tokens;
    }

    internal sealed class SyntaxException : ArgumentException {
        internal SyntaxException(string message, MermaidSourceSpan span) : base(message) { Span = span; }
        internal MermaidSourceSpan Span { get; }
    }
    internal static SyntaxException Syntax(string message, MermaidSourceSpan span) => new(message, span);

    private static int FindTopLevel(string text, string value) {
        var quote = '\0'; var escaped = false; var depth = 0;
        for (var index = 0; index <= text.Length - value.Length; index++) {
            MermaidResourceGuard.Checkpoint(); var ch = text[index];
            if (quote != '\0') {
                if (escaped) escaped = false; else if (ch == '\\') escaped = true; else if (ch == quote) quote = '\0';
                continue;
            }
            if (ch is '\'' or '"' or '`') { quote = ch; continue; }
            if (depth == 0 && string.CompareOrdinal(text, index, value, 0, value.Length) == 0) return index;
            if (ch is '(' or '[') depth++; else if (ch is ')' or ']') depth--;
        }
        return -1;
    }

    private static string Condition(string text, string keyword, MermaidSourceSpan span) {
        var condition = text.Substring(keyword.Length).Trim();
        if (condition.Length == 0) return string.Empty;
        if (condition.Length < 2 || condition[0] != '(' || MatchingClose(condition, 0) != condition.Length - 1)
            throw Syntax("ZenUML fragment conditions must be parenthesized.", span);
        return condition.Substring(1, condition.Length - 2).Trim();
    }

    private static bool IsKeyword(string text, string keyword) => text == keyword || text.StartsWith(keyword + " ", StringComparison.Ordinal) || text.StartsWith(keyword + "(", StringComparison.Ordinal);
    private static int MatchingClose(string text, int opening) {
        var depth = 0; var quote = '\0'; var escaped = false;
        for (var index = opening; index < text.Length; index++) {
            MermaidResourceGuard.Checkpoint(); var ch = text[index];
            if (quote != '\0') {
                if (escaped) escaped = false; else if (ch == '\\') escaped = true; else if (ch == quote) quote = '\0';
            } else if (ch is '\'' or '"' or '`') quote = ch;
            else if (ch == '(') depth++;
            else if (ch == ')' && --depth == 0) return index;
        }
        return -1;
    }
    private static bool IsIdentity(string text) {
        if (text.Length == 0) return false;
        foreach (var ch in text) if (char.IsWhiteSpace(ch) || ch is '(' or ')' or '{' or '}' or ':' or '=' or ';' or '"' or '\'' or '`' or '<' or '>') return false;
        return true;
    }
}
