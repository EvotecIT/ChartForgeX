using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>Defines how grammar rule identities resolve references.</summary>
public enum RailroadRuleComparison {
    /// <summary>Rule identities are case-sensitive.</summary>
    Ordinal,
    /// <summary>Rule identities ignore character case, as in ABNF.</summary>
    OrdinalIgnoreCase
}

/// <summary>A generic grammar diagram with symbolic references and native recursive railroad geometry.</summary>
/// <remarks>Native layout accepts up to 512 rules, 32768 expression occurrences and 128 nesting levels.
/// References and repetition counts remain symbolic. Measure or call WithNaturalSize after changing
/// rules, padding or typography; a smaller explicit frame is rejected instead of clipping grammar.</remarks>
public sealed class RailroadDiagramBlock : VisualBlock<RailroadDiagramBlock> {
    private readonly List<RailroadRule> _rules = new();
    private readonly IReadOnlyList<RailroadRule> _view;
    private RailroadRuleComparison _comparison;
    /// <summary>Initializes an empty grammar diagram.</summary>
    public RailroadDiagramBlock() { _view = _rules.AsReadOnly(); }
    /// <summary>Gets rules in authored order.</summary>
    public IReadOnlyList<RailroadRule> Rules => _view;
    /// <summary>Gets or sets rule-identity comparison without changing displayed rule IDs.</summary>
    public RailroadRuleComparison RuleComparison { get => _comparison; set { VisualBlockGuards.EnumDefined(value, nameof(value)); _comparison = value; } }
    /// <summary>Gets a concise accessible grammar name.</summary>
    public override string AccessibleName => Options.Accessibility.Name ?? (Title.Length == 0 ? "Railroad grammar" : Title);
    /// <summary>Creates an empty grammar diagram.</summary>
    public static RailroadDiagramBlock Create() => new();
    /// <summary>Adds a rule without expanding references or repeat counts.</summary>
    public RailroadDiagramBlock AddRule(string id, RailroadExpression expression, string? label = null) { _rules.Add(new RailroadRule(id, label ?? id, expression)); return this; }
    /// <summary>Measures the complete natural frame before any raster allocation.</summary>
    public ChartSize MeasureNaturalSize() => VisualBlockRendering.RailroadLayout(this).Size;
    /// <summary>Sets the complete natural frame, retaining optional minimum dimensions.</summary>
    public RailroadDiagramBlock WithNaturalSize(int minimumWidth = 0, int minimumHeight = 0) {
        if (minimumWidth < 0 || minimumHeight < 0) throw new ArgumentOutOfRangeException(nameof(minimumWidth));
        var size = MeasureNaturalSize(); Options.Size = new ChartSize(Math.Max(size.Width, minimumWidth), Math.Max(size.Height, minimumHeight)); return this;
    }
}

/// <summary>A stable rule identity, displayed label and immutable definition.</summary>
public sealed class RailroadRule {
    /// <summary>Initializes a grammar rule.</summary>
    public RailroadRule(string id, string label, RailroadExpression expression) {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("A grammar rule ID is required.", nameof(id)) : id;
        Label = string.IsNullOrWhiteSpace(label) ? throw new ArgumentException("A grammar rule label is required.", nameof(label)) : label;
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }
    /// <summary>Gets the stable identity used by symbolic references.</summary>
    public string Id { get; }
    /// <summary>Gets the displayed rule heading.</summary>
    public string Label { get; }
    /// <summary>Gets the immutable grammar expression.</summary>
    public RailroadExpression Expression { get; }
}
