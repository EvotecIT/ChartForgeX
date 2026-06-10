using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes Mermaid XY chart orientation.
/// </summary>
public enum MermaidXYChartOrientation {
    /// <summary>Render plots with the default vertical orientation.</summary>
    Vertical,
    /// <summary>Render capable plots with a horizontal orientation.</summary>
    Horizontal
}

/// <summary>
/// Identifies a Mermaid XY chart series family.
/// </summary>
public enum MermaidXYChartSeriesKind {
    /// <summary>A bar series.</summary>
    Bar,
    /// <summary>A line series.</summary>
    Line
}

/// <summary>
/// Describes a parsed Mermaid XY chart document.
/// </summary>
public sealed class MermaidXYChartDocument : MermaidDocument {
    /// <summary>Gets retained raw body statements.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets the requested chart orientation.</summary>
    public MermaidXYChartOrientation Orientation { get; set; }

    /// <summary>Gets or sets the optional chart title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets the optional x-axis declaration.</summary>
    public MermaidXYChartAxis XAxis { get; } = new();

    /// <summary>Gets the optional y-axis declaration.</summary>
    public MermaidXYChartAxis YAxis { get; } = new();

    /// <summary>Gets parsed bar and line series in source order.</summary>
    public List<MermaidXYChartSeries> Series { get; } = new();
}

/// <summary>
/// Describes a Mermaid XY chart axis declaration.
/// </summary>
public sealed class MermaidXYChartAxis {
    /// <summary>Gets or sets the optional axis title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets optional categorical labels.</summary>
    public List<string> Labels { get; } = new();

    /// <summary>Gets or sets the optional numeric minimum.</summary>
    public double? Minimum { get; set; }

    /// <summary>Gets or sets the optional numeric maximum.</summary>
    public double? Maximum { get; set; }

    /// <summary>Gets whether this axis declares a numeric range.</summary>
    public bool HasRange => Minimum.HasValue && Maximum.HasValue;
}

/// <summary>
/// Describes a Mermaid XY chart data series.
/// </summary>
public sealed class MermaidXYChartSeries : MermaidAstNode {
    private string _name;

    /// <summary>Initializes a Mermaid XY chart series.</summary>
    public MermaidXYChartSeries(MermaidXYChartSeriesKind kind, string name, IEnumerable<double> values, MermaidSourceSpan span) : base(span) {
        Kind = kind;
        _name = name ?? throw new ArgumentNullException(nameof(name));
        if (values == null) throw new ArgumentNullException(nameof(values));
        Values.AddRange(values);
    }

    /// <summary>Gets or sets the series kind.</summary>
    public MermaidXYChartSeriesKind Kind { get; set; }

    /// <summary>Gets or sets the renderer-facing series name.</summary>
    public string Name { get => _name; set => _name = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets parsed numeric values.</summary>
    public List<double> Values { get; } = new();
}
