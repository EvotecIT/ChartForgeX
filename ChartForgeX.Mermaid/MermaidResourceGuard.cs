using System;
using System.Threading;

namespace ChartForgeX.Mermaid;

/// <summary>Keeps one captured resource policy and cancellation token within a parser invocation.</summary>
internal static partial class MermaidResourceGuard {
    private static readonly AsyncLocal<Context?> Current = new();

    internal static IDisposable Begin(MermaidResourceLimits limits, CancellationToken cancellationToken) {
        if (limits == null) throw new ArgumentNullException(nameof(limits));
        var previous = Current.Value;
        Current.Value = new Context(limits.Clone(), cancellationToken);
        return new Scope(previous);
    }

    internal static void Checkpoint() => Current.Value?.Cancellation.ThrowIfCancellationRequested();

    internal static void CheckDepth(int depth, MermaidSourceSpan span) {
        Checkpoint();
        if (Current.Value is { } context) Check(nameof(MermaidResourceLimits.MaximumNestingDepth), context.Limits.MaximumNestingDepth, depth, span);
    }

    internal static void ReserveEdges(long count, MermaidSourceSpan span) {
        Checkpoint();
        if (Current.Value is not { } context) return;
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        Check(nameof(MermaidResourceLimits.MaximumEdges), context.Limits.MaximumEdges, Sum(context.ReservedEdges, count), span);
        context.ReservedEdges += count;
    }

    internal static void ReserveElements(long count, MermaidSourceSpan span) {
        Checkpoint();
        if (Current.Value is not { } context) return;
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        Check(nameof(MermaidResourceLimits.MaximumElements), context.Limits.MaximumElements, Sum(context.ReservedElements, count), span);
        context.ReservedElements += count;
    }

    internal static void ValidateSource(string source) {
        Checkpoint();
        if (Current.Value is not { } context) return;
        var limits = context.Limits;
        if (source.Length > limits.MaximumSourceCharacters)
            throw new MermaidResourceLimitException(nameof(limits.MaximumSourceCharacters), limits.MaximumSourceCharacters, source.Length, AtOffset(source, limits.MaximumSourceCharacters));
        var line = 1; var column = 0;
        for (var index = 0; index < source.Length; index++) {
            if ((index & 4095) == 0) Checkpoint();
            if (source[index] is '\r' or '\n') {
                if (source[index] == '\r' && index + 1 < source.Length && source[index + 1] == '\n') index++;
                line++; column = 0;
                Check(nameof(limits.MaximumSourceLines), limits.MaximumSourceLines, line, new MermaidSourceSpan(line, 1, 0));
            } else {
                column++;
                Check(nameof(limits.MaximumLineCharacters), limits.MaximumLineCharacters, column, new MermaidSourceSpan(line, column, 1));
            }
        }
        Checkpoint();
    }

    private static MermaidSourceSpan AtOffset(string source, int offset) {
        var line = 1; var column = 1;
        for (var index = 0; index < offset; index++) {
            if ((index & 4095) == 0) Checkpoint();
            if (source[index] == '\r') {
                if (index + 1 < offset && source[index + 1] == '\n') index++;
                line++; column = 1;
            } else if (source[index] == '\n') { line++; column = 1; }
            else column++;
        }
        return new MermaidSourceSpan(line, column, offset < source.Length ? 1 : 0);
    }

    private static void Check(string name, long limit, long actual, MermaidSourceSpan span) {
        if (actual > limit) throw new MermaidResourceLimitException(name, limit, actual, span);
    }

    private static long Sum(long current, long added) => added > long.MaxValue - current ? long.MaxValue : current + added;

    private sealed class Context {
        internal Context(MermaidResourceLimits limits, CancellationToken cancellation) { Limits = limits; Cancellation = cancellation; }
        internal MermaidResourceLimits Limits { get; }
        internal CancellationToken Cancellation { get; }
        internal long ReservedEdges { get; set; }
        internal long ReservedElements { get; set; }
        internal bool FlowchartOccurrenceBudget { get; set; }
    }

    private sealed class Scope : IDisposable {
        private readonly Context? _previous;
        private bool _disposed;
        internal Scope(Context? previous) => _previous = previous;
        public void Dispose() {
            if (_disposed) return;
            Current.Value = _previous; _disposed = true;
        }
    }
}
