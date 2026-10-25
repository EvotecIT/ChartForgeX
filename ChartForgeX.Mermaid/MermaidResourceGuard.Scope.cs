using System;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidResourceGuard {
    // Direct document conversions use their captured limits; nested render bridges retain
    // their existing scope and cancellation rather than starting a weaker operation.
    internal static IDisposable BeginIfAbsent(MermaidResourceLimits? limits) => Current.Value == null
        ? Begin(limits ?? new MermaidResourceLimits(), default) : new Scope(Current.Value);
    // Unique-ID input retains its established final semantic counting order. Before a
    // duplicate opening can allocate, account for its already retained nodes once; all
    // further actual node creation is charged without refunding merged declarations.
    internal static void ReserveFlowchartSubgraph(bool repeatedId, int retainedNodes, MermaidSourceSpan span) {
        if (repeatedId && Current.Value is { FlowchartOccurrenceBudget: false } context) {
            ReserveElements(retainedNodes, span);
            context.FlowchartOccurrenceBudget = true;
        }
        ReserveElements(1, span);
    }

    internal static void ReserveFlowchartNode(MermaidSourceSpan span) {
        if (Current.Value is { FlowchartOccurrenceBudget: true }) ReserveElements(1, span);
    }

    /// <summary>Checks required source-derived text facts before composing or materializing portable metadata.</summary>
    internal static void CheckInterchangeFactLength(long length, MermaidSourceSpan span, string limitName = "Interchange.MaximumTextCharacters") {
        Checkpoint();
        const int maximum = ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumTextCharacters;
        if (length > maximum) throw new MermaidResourceLimitException(limitName, maximum, length, span);
    }

    internal static MermaidResourceLimits CapturedLimits() => (Current.Value?.Limits ?? new MermaidResourceLimits()).Clone();
}
