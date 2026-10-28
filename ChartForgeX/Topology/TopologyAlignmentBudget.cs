using System;

namespace ChartForgeX.Topology;

/// <summary>Bounds cumulative alignment work, including rejected search branches, before allocation.</summary>
internal sealed class TopologyAlignmentBudget {
    internal const long MaximumWork = 8000000;
    internal const long MaximumDecisions = 4096;
    private long work;
    private long decisions;
    private readonly string alignmentId;
    internal TopologyAlignmentBudget(string alignmentId) { this.alignmentId = alignmentId; }
    internal void Work(long amount = 1) {
        if (amount < 0 || amount > MaximumWork - work)
            throw new TopologyAlignmentLimitException("Topology.AlignmentWork", MaximumWork, amount < 0 ? long.MaxValue : amount > long.MaxValue - work ? long.MaxValue : work + amount, alignmentId);
        work += amount;
    }
    internal void Decision() {
        if (++decisions > MaximumDecisions)
            throw new TopologyAlignmentLimitException("Topology.AlignmentDecisions", MaximumDecisions, decisions, alignmentId);
        Work();
    }
}

/// <summary>Retains located resource facts for the source adapter without parsing exception text.</summary>
internal sealed class TopologyAlignmentLimitException : InvalidOperationException {
    internal string LimitName { get; }
    internal long Limit { get; }
    internal long Actual { get; }
    internal string AlignmentId { get; }
    internal TopologyAlignmentLimitException(string name, long limit, long actual, string alignmentId)
        : base("alignment-resource-limit: " + name + " exceeded (limit " + limit + ", actual " + actual + ").", NativeResult(name, limit, actual, alignmentId)) {
        LimitName = name; Limit = limit; Actual = actual; AlignmentId = alignmentId;
    }
    private static TopologyValidationException NativeResult(string name, long limit, long actual, string alignmentId) {
        var result = new TopologyValidationResult();
        result.Errors.Add(new TopologyValidationIssue { Code = "alignment-resource-limit", ItemId = alignmentId,
            Message = name + " exceeded (limit " + limit + ", actual " + actual + ")." });
        return new TopologyValidationException(result);
    }
}
