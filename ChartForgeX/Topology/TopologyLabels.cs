using System;
using ChartForgeX.Core;

namespace ChartForgeX.Topology;

/// <summary>
/// Holds the words ChartForgeX writes into rendered topology diagrams on its own, so hosts can localize them, as
/// <see cref="ChartLabels"/> does for charts. Defaults are English.
/// </summary>
public sealed class TopologyLabels {
    private const string DefaultUntitled = "ChartForgeX topology";
    private string _untitledTopology = DefaultUntitled;
    private string _untitledReport = "Topology report";

    /// <summary>
    /// Gets or sets the accessible name of a diagram without a title (the SVG <c>title</c> element and the HTML page
    /// title). Default <c>ChartForgeX topology</c>.
    /// </summary>
    public string UntitledTopology {
        get => _untitledTopology;
        set => _untitledTopology = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Label text must not be empty.", nameof(value)) : value;
    }

    /// <summary>
    /// Gets or sets a function that writes the automatic description (the SVG <c>desc</c> element) from its
    /// <see cref="ChartDescriptionFacts"/>, whose <see cref="ChartDescriptionFacts.Kind"/> is
    /// <see cref="ChartDescriptionKind.Topology"/> with the node, group, and edge counts. It takes the same facts as
    /// <see cref="ChartLabels.AccessibleTextFormatter"/>, so a host can use one formatter for charts and diagrams. Null
    /// (the default), or a function returning null or white space, writes <see cref="ChartDescriptionFacts.EnglishText"/>.
    /// A description set through <see cref="TopologyChart.WithAccessibility"/> still wins and the function is not called
    /// for it. Exceptions thrown by the function are not caught; they surface from the render call.
    /// </summary>
    public Func<ChartDescriptionFacts, string?>? AccessibleTextFormatter { get; set; }

    /// <summary>
    /// Gets or sets the title of a topology report (<c>TopologyReport</c>) whose source diagram has no title; its pages
    /// add their number. Default <c>Topology report</c>.
    /// </summary>
    public string UntitledReport {
        get => _untitledReport;
        set => _untitledReport = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Label text must not be empty.", nameof(value)) : value;
    }

    internal TopologyLabels Clone() => new() { _untitledTopology = _untitledTopology, _untitledReport = _untitledReport, AccessibleTextFormatter = AccessibleTextFormatter };

    internal string Describe(TopologyChart chart) => Describe(chart.Title, chart.Groups.Count, chart.Nodes.Count, chart.Edges.Count);

    internal string Describe(string? title, int groupCount, int nodeCount, int edgeCount) {
        var facts = new ChartDescriptionFacts(ChartDescriptionKind.Topology, title, Array.Empty<string>(), nodeCount, groupCount: groupCount, edgeCount: edgeCount);
        var text = AccessibleTextFormatter?.Invoke(facts);
        return string.IsNullOrWhiteSpace(text) ? facts.EnglishText : text!;
    }

    internal string Name(TopologyChart chart) => string.IsNullOrWhiteSpace(chart.Title) ? UntitledTopology : chart.Title!;
}
