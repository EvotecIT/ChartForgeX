using System;
using System.Collections.Generic;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateRailroad(VisualArtifactInterchangeEnvelope envelope) {
        var railroad = envelope.Railroad!;
        Defined(railroad.RuleComparison, "railroad rule comparison", envelope);
        Defined(railroad.ThemeMode, "railroad theme mode", envelope);
        if (railroad.Theme == null) throw new ArgumentException("Railroad presentation requires a native theme.", nameof(envelope));
        if (railroad.Theme.ToJson().Length > 1024 * 1024) throw new ArgumentException("Railroad theme JSON exceeds the native theme input limit.", nameof(envelope));
        foreach (var padding in new[] { railroad.Padding.Left, railroad.Padding.Top, railroad.Padding.Right, railroad.Padding.Bottom }) {
            Finite(padding, "railroad padding");
            if (padding < 0) throw new ArgumentOutOfRangeException(nameof(envelope), "Railroad padding must not be negative.");
        }
        if (railroad.PngOutputScale < 1 || railroad.PngOutputScale > 4) throw new ArgumentOutOfRangeException(nameof(envelope), "Railroad PNG scale must be between one and four.");
        if (envelope.Width.HasValue != envelope.Height.HasValue) throw new ArgumentException("Railroad frames require both dimensions or neither.", nameof(envelope));
        foreach (var dimension in new[] { envelope.Width, envelope.Height })
            if (dimension.HasValue && (dimension.Value > int.MaxValue || Math.Truncate(dimension.Value) != dimension.Value))
                throw new ArgumentOutOfRangeException(nameof(envelope), "Railroad frame dimensions must be positive whole native pixels.");
        if (envelope.Groups.Count != 0 || envelope.Nodes.Count != 0 || envelope.Edges.Count != 0 || envelope.Annotations.Count != 0 || envelope.Scenarios.Count != 0)
            throw new ArgumentException("Railroad semantics belong to the typed rule model, not graph entities.", nameof(envelope));
        if (railroad.Rules.Count == 0) throw new ArgumentException("Railroad diagrams require at least one rule.", nameof(envelope));
        Count(railroad.Rules.Count, RailroadDiagramLimits.MaximumRules, "railroad rules");
        var comparer = railroad.RuleComparison == RailroadRuleComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var ids = new HashSet<string>(comparer); var occurrences = 0;
        foreach (var rule in railroad.Rules) {
            if (rule == null) throw new ArgumentException("Railroad rules must not be null.", nameof(envelope));
            RequiredId(rule.Id, "railroad rule id"); Text(rule.Label, "railroad rule label");
            if (!ids.Add(rule.Id)) throw new ArgumentException("Railroad rule IDs must be unique under their comparison policy.", nameof(envelope));
            ValidateRailroadExpression(rule.Expression, ref occurrences);
        }
    }

    private static void ValidateRailroadExpression(RailroadExpression expression, ref int occurrences) {
        if (++occurrences > RailroadDiagramLimits.MaximumExpressionOccurrences)
            throw new ArgumentException("Railroad expression occurrences exceed the native limit.");
        if (expression.Depth > RailroadDiagramLimits.MaximumExpressionDepth)
            throw new ArgumentException("Railroad expression nesting exceeds the native limit.");
        Text(expression.Value, "railroad expression text");
        if (expression.Children.Count > RailroadDiagramLimits.MaximumExpressionOccurrences - occurrences)
            throw new ArgumentException("Railroad expression occurrences exceed the native limit.");
        foreach (var child in expression.Children) ValidateRailroadExpression(child, ref occurrences);
    }
}
