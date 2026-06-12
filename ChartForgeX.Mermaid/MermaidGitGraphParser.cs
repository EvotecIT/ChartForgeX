using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal static class MermaidGitGraphParser {
    private const int MaximumGitGraphBranches = 128;
    private const int MaximumGitGraphCommits = 10000;

    public static void ParseStatements(MermaidGitGraphDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var branches = new HashSet<string>(StringComparer.Ordinal);
        var branchHeads = new Dictionary<string, string?>(StringComparer.Ordinal);
        var commits = new HashSet<string>(StringComparer.Ordinal);
        var currentBranch = "main";
        var commitIndex = 0;

        AddBranch(document, branches, branchHeads, currentBranch, 0, null, document.HeaderSpan);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "commit")) {
                AddCommitStatement(document, trimmed.Substring(6).Trim(), span, currentBranch, branchHeads, commits, ref commitIndex, result);
            } else if (StartsWithKeyword(trimmed, "branch")) {
                ParseBranchStatement(document, trimmed.Substring(6).Trim(), span, branches, branchHeads, ref currentBranch, result);
            } else if (StartsWithKeyword(trimmed, "checkout")) {
                ParseCheckoutStatement(trimmed.Substring(8).Trim(), span, branches, ref currentBranch, result);
            } else if (StartsWithKeyword(trimmed, "switch")) {
                ParseCheckoutStatement(trimmed.Substring(6).Trim(), span, branches, ref currentBranch, result);
            } else if (StartsWithKeyword(trimmed, "merge")) {
                ParseMergeStatement(document, trimmed.Substring(5).Trim(), span, currentBranch, branchHeads, commits, ref commitIndex, result);
            } else if (StartsWithKeyword(trimmed, "cherry-pick")) {
                ParseCherryPickStatement(document, trimmed.Substring(11).Trim(), span, currentBranch, branchHeads, commits, ref commitIndex, result);
            } else {
                document.RetainedStatements.Add(new MermaidRawStatement(trimmed, span));
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid gitGraph statement is retained but not rendered by ChartForgeX yet: " + FirstToken(trimmed) + ".");
            }
        }

        if (document.Commits.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph diagrams require at least one commit.");
    }

    private static void ParseBranchStatement(MermaidGitGraphDocument document, string text, MermaidSourceSpan span, HashSet<string> branches, Dictionary<string, string?> branchHeads, ref string currentBranch, MermaidParseResult<MermaidDocument> result) {
        if (!TryReadNameAndAttributes(text, out var name, out var attributes) || name.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph branch statements must define a branch name.");
            return;
        }

        if (branches.Contains(name)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph branch names must be unique: " + name + ".");
            return;
        }

        if (document.Branches.Count >= MaximumGitGraphBranches) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph diagrams support no more than " + MaximumGitGraphBranches.ToString(CultureInfo.InvariantCulture) + " branches.");
            return;
        }

        int? order = null;
        if (attributes.TryGetValue("order", out var orderText)) {
            if (int.TryParse(orderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedOrder) && parsedOrder >= 0) order = parsedOrder;
            else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Mermaid gitGraph branch order must be a non-negative integer.");
        }

        AddBranch(document, branches, branchHeads, name, order, branchHeads[currentBranch], span);
        currentBranch = name;
    }

    private static void ParseCheckoutStatement(string text, MermaidSourceSpan span, HashSet<string> branches, ref string currentBranch, MermaidParseResult<MermaidDocument> result) {
        var name = MermaidParserUtilities.Unquote(text.Trim());
        if (!branches.Contains(name)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph checkout references an unknown branch: " + name + ".");
            return;
        }

        currentBranch = name;
    }

    private static void ParseMergeStatement(MermaidGitGraphDocument document, string text, MermaidSourceSpan span, string currentBranch, Dictionary<string, string?> branchHeads, HashSet<string> commits, ref int commitIndex, MermaidParseResult<MermaidDocument> result) {
        if (!TryReadNameAndAttributes(text, out var sourceBranch, out var attributes) || sourceBranch.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph merge statements must define a source branch.");
            return;
        }

        if (!branchHeads.ContainsKey(sourceBranch)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph merge references an unknown branch: " + sourceBranch + ".");
            return;
        }

        if (string.Equals(sourceBranch, currentBranch, StringComparison.Ordinal)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph cannot merge a branch into itself.");
            return;
        }

        var sourceHead = branchHeads[sourceBranch];
        if (sourceHead == null) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph merge source branch has no commits: " + sourceBranch + ".");
            return;
        }

        var parents = new List<string>();
        if (branchHeads[currentBranch] != null) parents.Add(branchHeads[currentBranch]!);
        parents.Add(sourceHead);
        var id = ResolveCommitId(attributes, "merge-" + (++commitIndex).ToString(CultureInfo.InvariantCulture));
        var type = ParseCommitType(Attr(attributes, "type"), GitGraphCommitType.Merge);
        AddCommit(document, commits, branchHeads, currentBranch, id, parents, type, id, Attr(attributes, "tag"), string.Empty, span, result);
    }

    private static void ParseCherryPickStatement(MermaidGitGraphDocument document, string text, MermaidSourceSpan span, string currentBranch, Dictionary<string, string?> branchHeads, HashSet<string> commits, ref int commitIndex, MermaidParseResult<MermaidDocument> result) {
        var attributes = ParseAttributes(text);
        if (!attributes.TryGetValue("id", out var sourceId) || sourceId.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph cherry-pick statements must define an id attribute.");
            return;
        }

        if (!commits.Contains(sourceId)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph cherry-pick references an unknown commit id: " + sourceId + ".");
            return;
        }

        var sourceCommit = document.Commits.Find(commit => string.Equals(commit.Id, sourceId, StringComparison.Ordinal));
        if (sourceCommit != null && string.Equals(sourceCommit.BranchName, currentBranch, StringComparison.Ordinal)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph cherry-pick source commits must come from a different branch.");
            return;
        }

        if (branchHeads[currentBranch] == null) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph cherry-pick requires the current branch to have at least one commit.");
            return;
        }

        var parents = new[] { branchHeads[currentBranch]!, sourceId };
        var id = "cherry-pick-" + (++commitIndex).ToString(CultureInfo.InvariantCulture);
        AddCommit(document, commits, branchHeads, currentBranch, id, parents, GitGraphCommitType.CherryPick, sourceId, Attr(attributes, "tag"), sourceId, span, result);
    }

    private static void AddCommitStatement(MermaidGitGraphDocument document, string text, MermaidSourceSpan span, string currentBranch, Dictionary<string, string?> branchHeads, HashSet<string> commits, ref int commitIndex, MermaidParseResult<MermaidDocument> result) {
        var attributes = ParseAttributes(text);
        var id = ResolveCommitId(attributes, "commit-" + (++commitIndex).ToString(CultureInfo.InvariantCulture));
        var type = ParseCommitType(Attr(attributes, "type"), GitGraphCommitType.Normal);
        var parents = branchHeads[currentBranch] == null ? Array.Empty<string>() : new[] { branchHeads[currentBranch]! };
        AddCommit(document, commits, branchHeads, currentBranch, id, parents, type, id, Attr(attributes, "tag"), string.Empty, span, result);
    }

    private static void AddCommit(MermaidGitGraphDocument document, HashSet<string> commits, Dictionary<string, string?> branchHeads, string branchName, string id, IEnumerable<string> parentIds, GitGraphCommitType type, string label, string tag, string sourceCommitId, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (document.Commits.Count >= MaximumGitGraphCommits) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph diagrams support no more than " + MaximumGitGraphCommits.ToString(CultureInfo.InvariantCulture) + " commits.");
            return;
        }

        if (!commits.Add(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph commit ids must be unique: " + id + ".");
            return;
        }

        var commit = new MermaidGitGraphCommit(id, branchName, parentIds, type, label, tag, sourceCommitId, span);
        document.Commits.Add(commit);
        branchHeads[branchName] = id;
    }

    private static void AddBranch(MermaidGitGraphDocument document, HashSet<string> branches, Dictionary<string, string?> branchHeads, string name, int? order, string? head, MermaidSourceSpan span) {
        branches.Add(name);
        branchHeads[name] = head;
        document.Branches.Add(new MermaidGitGraphBranch(name, order, span));
    }

    private static GitGraphCommitType ParseCommitType(string value, GitGraphCommitType fallback) {
        switch (value.Trim().ToUpperInvariant()) {
            case "HIGHLIGHT":
                return GitGraphCommitType.Highlight;
            case "REVERSE":
                return GitGraphCommitType.Reverse;
            default:
                return fallback;
        }
    }

    private static string ResolveCommitId(Dictionary<string, string> attributes, string fallback) =>
        attributes.TryGetValue("id", out var id) && id.Length > 0 ? id : fallback;

    private static string Attr(Dictionary<string, string> attributes, string key) =>
        attributes.TryGetValue(key, out var value) ? value : string.Empty;

    private static bool TryReadNameAndAttributes(string text, out string name, out Dictionary<string, string> attributes) {
        var tokens = SplitTokens(text);
        name = string.Empty;
        attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (tokens.Count == 0) return false;
        name = MermaidParserUtilities.Unquote(tokens[0]);
        if (tokens.Count > 1) attributes = ParseAttributes(string.Join(" ", tokens.GetRange(1, tokens.Count - 1).ToArray()));
        return true;
    }

    private static Dictionary<string, string> ParseAttributes(string text) {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tokens = SplitTokens(text);
        for (var index = 0; index < tokens.Count; index++) {
            var token = tokens[index];
            var colon = token.IndexOf(':');
            if (colon < 0) continue;
            var key = token.Substring(0, colon).Trim();
            var value = token.Substring(colon + 1).Trim();
            if (value.Length == 0 && index + 1 < tokens.Count) value = tokens[++index].Trim();
            if (key.Length == 0) continue;
            attributes[key] = MermaidParserUtilities.Unquote(value);
        }

        return attributes;
    }

    private static List<string> SplitTokens(string text) {
        var tokens = new List<string>();
        var start = -1;
        var inQuote = false;
        for (var index = 0; index < text.Length; index++) {
            var c = text[index];
            if (c == '"') inQuote = !inQuote;
            if (char.IsWhiteSpace(c) && !inQuote) {
                if (start >= 0) tokens.Add(text.Substring(start, index - start));
                start = -1;
            } else if (start < 0) {
                start = index;
            }
        }

        if (start >= 0) tokens.Add(text.Substring(start));
        return tokens;
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }

    private static string FirstToken(string text) {
        var tokens = SplitTokens(text);
        return tokens.Count == 0 ? text : tokens[0];
    }
}
