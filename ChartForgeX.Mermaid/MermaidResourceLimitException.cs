using System;

namespace ChartForgeX.Mermaid;

/// <summary>Reports a located resource bound exceeded during parsing or deferred artifact export.</summary>
public sealed class MermaidResourceLimitException : InvalidOperationException {
    /// <summary>Initializes a resource failure with the configured bound and observed requirement.</summary>
    public MermaidResourceLimitException(string limitName, long limit, long actual, MermaidSourceSpan span)
        : base("Mermaid resource limit '" + limitName + "' is " + limit + "; observed " + actual + ".") {
        LimitName = limitName ?? throw new ArgumentNullException(nameof(limitName));
        Limit = limit; Actual = actual; Span = span;
    }
    internal MermaidResourceLimitException(string limitName, long limit, long actual, MermaidSourceSpan span, Exception inner)
        : base("Mermaid resource limit '" + limitName + "' is " + limit + "; observed " + actual + ".", inner) {
        LimitName = limitName; Limit = limit; Actual = actual; Span = span;
    }

    /// <summary>Gets the resource setting whose bound was exceeded.</summary>
    public string LimitName { get; }
    /// <summary>Gets the captured configured bound.</summary>
    public long Limit { get; }
    /// <summary>Gets the observed count or required allocation.</summary>
    public long Actual { get; }
    /// <summary>Gets the source statement or first character that exceeds the bound.</summary>
    public MermaidSourceSpan Span { get; }
}
