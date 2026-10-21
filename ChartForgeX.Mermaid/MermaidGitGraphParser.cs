using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGitGraphParser {
    private const int MaximumGitGraphBranches = 128;
    private const int MaximumGitGraphCommits = 10000;

    public static void ParseStatements(MermaidGitGraphDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var branches = new HashSet<string>(StringComparer.Ordinal);
        var branchHeads = new Dictionary<string, string?>(StringComparer.Ordinal);
        var commits = new HashSet<string>(StringComparer.Ordinal);
        var mergeCommits = new HashSet<string>(StringComparer.Ordinal);
        var currentBranch = "main";
        var commitIndex = 0;

        AddBranch(document, branches, branchHeads, currentBranch, 0, null, document.HeaderSpan);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            foreach (var statement in SplitStatements(lines[line - 1], line)) {
                var trimmed = statement.Text;
                var span = statement.Span;
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
                    ParseMergeStatement(document, trimmed.Substring(5).Trim(), span, currentBranch, branchHeads, commits, mergeCommits, ref commitIndex, result);
                } else if (StartsWithKeyword(trimmed, "cherry-pick")) {
                    ParseCherryPickStatement(document, trimmed.Substring(11).Trim(), span, currentBranch, branchHeads, commits, mergeCommits, ref commitIndex, result);
                } else {
                    document.RetainedStatements.Add(new MermaidRawStatement(trimmed, span));
                    Invalid(result, span, "Unknown Mermaid gitGraph statement: " + trimmed + ".");
                }
            }
        }

        if (document.Commits.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph diagrams require at least one commit.");
    }

    private static void ParseBranchStatement(MermaidGitGraphDocument document, string text, MermaidSourceSpan span, HashSet<string> branches, Dictionary<string, string?> branchHeads, ref string currentBranch, MermaidParseResult<MermaidDocument> result) {
        if (!TryReadNameAndAttributes(text, "branch", span, result, out var name, out var attributes)) return;

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
            if (int.TryParse(orderText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedOrder)) order = parsedOrder;
            else { Invalid(result, span, "Mermaid gitGraph branch order must be a non-negative integer."); return; }
        }

        AddBranch(document, branches, branchHeads, name, order, branchHeads[currentBranch], span);
        currentBranch = name;
    }

    private static void ParseCheckoutStatement(string text, MermaidSourceSpan span, HashSet<string> branches, ref string currentBranch, MermaidParseResult<MermaidDocument> result) {
        if (!TryReadNameAndAttributes(text, "checkout", span, result, out var name, out _)) return;
        if (!branches.Contains(name)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph checkout references an unknown branch: " + name + ".");
            return;
        }

        currentBranch = name;
    }

    private static void ParseMergeStatement(MermaidGitGraphDocument document, string text, MermaidSourceSpan span, string currentBranch, Dictionary<string, string?> branchHeads, HashSet<string> commits, HashSet<string> mergeCommits, ref int commitIndex, MermaidParseResult<MermaidDocument> result) {
        if (!TryReadNameAndAttributes(text, "merge", span, result, out var sourceBranch, out var attributes)) return;
        if (!TryReadCommitType(attributes, GitGraphCommitType.Merge, span, result, out var type)) return;

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

        var currentHead = branchHeads[currentBranch];
        if (currentHead == null) {
            Invalid(result, span, "Mermaid gitGraph merge requires the current branch to have at least one commit.");
            return;
        }
        if (string.Equals(currentHead, sourceHead, StringComparison.Ordinal)) {
            Invalid(result, span, "Mermaid gitGraph cannot merge branches with the same head commit.");
            return;
        }

        var parents = new[] { currentHead, sourceHead };
        var id = ResolveCommitId(attributes, "merge-" + (++commitIndex).ToString(CultureInfo.InvariantCulture));
        if (AddCommit(document, commits, branchHeads, currentBranch, id, parents, type, id, Attr(attributes, "tag"), string.Empty, span, result)) mergeCommits.Add(id);
    }

    private static void ParseCherryPickStatement(MermaidGitGraphDocument document, string text, MermaidSourceSpan span, string currentBranch, Dictionary<string, string?> branchHeads, HashSet<string> commits, HashSet<string> mergeCommits, ref int commitIndex, MermaidParseResult<MermaidDocument> result) {
        if (!TryReadAttributes(text, 0, "cherry-pick", span, result, out var attributes)) return;
        if (!attributes.TryGetValue("id", out var sourceId) || sourceId.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph cherry-pick statements must define an id attribute.");
            return;
        }

        if (!commits.Contains(sourceId)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph cherry-pick references an unknown commit id: " + sourceId + ".");
            return;
        }

        var sourceCommit = document.Commits.Find(commit => string.Equals(commit.Id, sourceId, StringComparison.Ordinal));
        var sourceParent = Attr(attributes, "parent");
        if (sourceParent.Length > 0 && sourceCommit != null && !IsImmediateParent(sourceCommit, sourceParent)) {
            Invalid(result, span, "Mermaid gitGraph cherry-pick parent must be an immediate parent of the source commit: " + sourceParent + ".");
            return;
        }
        if (mergeCommits.Contains(sourceId) && sourceParent.Length == 0) {
            Invalid(result, span, "Mermaid gitGraph cherry-picking a merge commit requires an immediate parent attribute.");
            return;
        }
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
        var tag = attributes.TryGetValue("tag", out var authoredTag) ? authoredTag
            : "cherry-pick:" + sourceId + (mergeCommits.Contains(sourceId) ? "|parent:" + sourceParent : string.Empty);
        AddCommit(document, commits, branchHeads, currentBranch, id, parents, GitGraphCommitType.CherryPick, sourceId, tag, sourceId, span, result, sourceParent);
    }

    private static void AddCommitStatement(MermaidGitGraphDocument document, string text, MermaidSourceSpan span, string currentBranch, Dictionary<string, string?> branchHeads, HashSet<string> commits, ref int commitIndex, MermaidParseResult<MermaidDocument> result) {
        if (!TryReadAttributes(text, 0, "commit", span, result, out var attributes)) return;
        if (!TryReadCommitType(attributes, GitGraphCommitType.Normal, span, result, out var type)) return;
        var id = ResolveCommitId(attributes, "commit-" + (++commitIndex).ToString(CultureInfo.InvariantCulture));
        var parents = branchHeads[currentBranch] == null ? Array.Empty<string>() : new[] { branchHeads[currentBranch]! };
        AddCommit(document, commits, branchHeads, currentBranch, id, parents, type, attributes.TryGetValue("msg", out var message) ? message : id, Attr(attributes, "tag"), string.Empty, span, result);
    }

    private static bool AddCommit(MermaidGitGraphDocument document, HashSet<string> commits, Dictionary<string, string?> branchHeads, string branchName, string id, IEnumerable<string> parentIds, GitGraphCommitType type, string label, string tag, string sourceCommitId, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, string sourceParentCommitId = "") {
        if (document.Commits.Count >= MaximumGitGraphCommits) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph diagrams support no more than " + MaximumGitGraphCommits.ToString(CultureInfo.InvariantCulture) + " commits.");
            return false;
        }

        if (!commits.Add(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Mermaid gitGraph commit ids must be unique: " + id + ".");
            return false;
        }

        var commit = new MermaidGitGraphCommit(id, branchName, parentIds, type, label, tag, sourceCommitId, span, sourceParentCommitId);
        document.Commits.Add(commit);
        branchHeads[branchName] = id;
        return true;
    }

    private static void AddBranch(MermaidGitGraphDocument document, HashSet<string> branches, Dictionary<string, string?> branchHeads, string name, int? order, string? head, MermaidSourceSpan span) {
        branches.Add(name);
        branchHeads[name] = head;
        document.Branches.Add(new MermaidGitGraphBranch(name, order, span));
    }

    private static string ResolveCommitId(Dictionary<string, string> attributes, string fallback) =>
        attributes.TryGetValue("id", out var id) && id.Length > 0 ? id : fallback;

    private static string Attr(Dictionary<string, string> attributes, string key) =>
        attributes.TryGetValue(key, out var value) ? value : string.Empty;

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }

}
