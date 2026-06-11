using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public const int MaximumGitGraphBranches = 128;
    public const int MaximumGitGraphCommits = 10000;

    public static void ValidateGitGraph(GitGraphBlock graph) {
        if (graph.Branches.Count == 0) throw new InvalidOperationException("Git graph blocks must contain at least one branch.");
        if (graph.Branches.Count > MaximumGitGraphBranches) throw new InvalidOperationException("Git graph blocks must contain no more than " + MaximumGitGraphBranches.ToString(CultureInfo.InvariantCulture) + " branches.");
        if (graph.Commits.Count == 0) throw new InvalidOperationException("Git graph blocks must contain at least one commit.");
        if (graph.Commits.Count > MaximumGitGraphCommits) throw new InvalidOperationException("Git graph blocks must contain no more than " + MaximumGitGraphCommits.ToString(CultureInfo.InvariantCulture) + " commits.");

        var branches = new HashSet<string>(StringComparer.Ordinal);
        foreach (var branch in graph.Branches) {
            if (branch.Name.Length == 0) throw new InvalidOperationException("Git graph branches must define names.");
            if (!branches.Add(branch.Name)) throw new InvalidOperationException("Git graph branch names must be unique: " + branch.Name + ".");
        }

        var commits = new HashSet<string>(StringComparer.Ordinal);
        foreach (var commit in graph.Commits) {
            if (commit.Id.Length == 0) throw new InvalidOperationException("Git graph commits must define ids.");
            if (commit.BranchName.Length == 0 || !branches.Contains(commit.BranchName)) throw new InvalidOperationException("Git graph commit branch was not found: " + commit.BranchName + ".");
            if (!commits.Add(commit.Id)) throw new InvalidOperationException("Git graph commit ids must be unique: " + commit.Id + ".");
        }

        foreach (var commit in graph.Commits) {
            foreach (var parentId in commit.ParentIds) {
                if (!commits.Contains(parentId)) throw new InvalidOperationException("Git graph commit parent id was not found: " + parentId + ".");
            }

            if (commit.SourceCommitId.Length > 0 && !commits.Contains(commit.SourceCommitId)) throw new InvalidOperationException("Git graph cherry-pick source id was not found: " + commit.SourceCommitId + ".");
        }
    }

    public static GitGraphLayout BuildGitGraphLayout(GitGraphBlock graph, ChartRect content, double y, double bottomPadding, double totalHeight) {
        var orderedBranches = OrderedGitGraphBranches(graph);
        var laneYs = new Dictionary<string, double>(StringComparer.Ordinal);
        var labelWidth = graph.ShowBranchLabels ? Math.Min(130, Math.Max(72, content.Width * 0.18)) : 18;
        var plotX = content.X + labelWidth;
        var plotWidth = Math.Max(40, content.Width - labelWidth);
        var availableHeight = Math.Max(32, totalHeight - bottomPadding - y);
        var laneGap = orderedBranches.Count <= 1 ? 0 : Math.Min(68, Math.Max(34, availableHeight / Math.Max(1, orderedBranches.Count - 1)));
        var startY = y + Math.Max(18, (availableHeight - laneGap * Math.Max(0, orderedBranches.Count - 1)) / 2);
        for (var index = 0; index < orderedBranches.Count; index++) laneYs[orderedBranches[index].Name] = startY + index * laneGap;

        var placements = new List<GitGraphCommitPlacement>(graph.Commits.Count);
        var byId = new Dictionary<string, GitGraphCommitPlacement>(StringComparer.Ordinal);
        var step = graph.Commits.Count <= 1 ? 0 : plotWidth / Math.Max(1, graph.Commits.Count - 1);
        for (var index = 0; index < graph.Commits.Count; index++) {
            var commit = graph.Commits[index];
            var x = plotX + index * step;
            var placement = new GitGraphCommitPlacement(commit, index, x, laneYs[commit.BranchName]);
            placements.Add(placement);
            byId[commit.Id] = placement;
        }

        return new GitGraphLayout(orderedBranches, laneYs, placements, byId, plotX, plotX + plotWidth);
    }

    private static IReadOnlyList<GitGraphBranch> OrderedGitGraphBranches(GitGraphBlock graph) {
        var branches = new List<OrderedGitGraphBranch>(graph.Branches.Count);
        for (var index = 0; index < graph.Branches.Count; index++) branches.Add(new OrderedGitGraphBranch(graph.Branches[index], index));
        branches.Sort((left, right) => {
            var leftOrder = left.Branch.Order ?? int.MaxValue;
            var rightOrder = right.Branch.Order ?? int.MaxValue;
            var order = leftOrder.CompareTo(rightOrder);
            return order != 0 ? order : left.Index.CompareTo(right.Index);
        });

        var ordered = new List<GitGraphBranch>(branches.Count);
        for (var index = 0; index < branches.Count; index++) ordered.Add(branches[index].Branch);
        return ordered;
    }

    private readonly struct OrderedGitGraphBranch {
        public OrderedGitGraphBranch(GitGraphBranch branch, int index) {
            Branch = branch;
            Index = index;
        }

        public GitGraphBranch Branch { get; }
        public int Index { get; }
    }

    public sealed class GitGraphLayout {
        public GitGraphLayout(IReadOnlyList<GitGraphBranch> branches, IReadOnlyDictionary<string, double> laneYs, IReadOnlyList<GitGraphCommitPlacement> placements, IReadOnlyDictionary<string, GitGraphCommitPlacement> placementsById, double plotX, double plotRight) {
            Branches = branches;
            LaneYs = laneYs;
            Placements = placements;
            PlacementsById = placementsById;
            PlotX = plotX;
            PlotRight = plotRight;
        }

        public IReadOnlyList<GitGraphBranch> Branches { get; }
        public IReadOnlyDictionary<string, double> LaneYs { get; }
        public IReadOnlyList<GitGraphCommitPlacement> Placements { get; }
        public IReadOnlyDictionary<string, GitGraphCommitPlacement> PlacementsById { get; }
        public double PlotX { get; }
        public double PlotRight { get; }
    }

    public readonly struct GitGraphCommitPlacement {
        public GitGraphCommitPlacement(GitGraphCommit commit, int index, double x, double y) {
            Commit = commit;
            Index = index;
            X = x;
            Y = y;
        }

        public GitGraphCommit Commit { get; }
        public int Index { get; }
        public double X { get; }
        public double Y { get; }
    }
}
