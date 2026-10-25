using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Maps finite source curve choices into the canonical native interpolation policy.</summary>
/// <remarks>Source statement order belongs here; route construction, numeric geometry and painting remain native.</remarks>
internal static class MermaidFlowchartCurves {
    private static readonly Dictionary<string,TopologyEdgeInterpolation> Kinds = new(StringComparer.Ordinal) {
        ["linear"] = TopologyEdgeInterpolation.Linear, ["basis"] = TopologyEdgeInterpolation.Basis,
        ["bumpX"] = TopologyEdgeInterpolation.BumpX, ["bumpY"] = TopologyEdgeInterpolation.BumpY,
        ["cardinal"] = TopologyEdgeInterpolation.Cardinal, ["catmullRom"] = TopologyEdgeInterpolation.CatmullRom,
        ["monotoneX"] = TopologyEdgeInterpolation.MonotoneX, ["monotoneY"] = TopologyEdgeInterpolation.MonotoneY,
        ["natural"] = TopologyEdgeInterpolation.Natural, ["step"] = TopologyEdgeInterpolation.Step,
        ["stepAfter"] = TopologyEdgeInterpolation.StepAfter, ["stepBefore"] = TopologyEdgeInterpolation.StepBefore,
        ["rounded"] = TopologyEdgeInterpolation.Rounded
    };
    internal static bool Applies(MermaidDiagramKind kind) => kind is MermaidDiagramKind.Flowchart or MermaidDiagramKind.Swimlane;
    internal static bool TryKind(string? name, out TopologyEdgeInterpolation kind) {
        if (name != null && Kinds.TryGetValue(name, out kind)) return true;
        kind = TopologyEdgeInterpolation.Basis; return false;
    }

    internal static void Apply(MermaidFlowchartDocument document, MermaidFlowchartEdge edge, int index, TopologyEdge target) {
        if (!Applies(document.Kind)) return;
        Choice? raw = null; Choice? defaults = null;
        foreach (var declaration in document.LinkStyles) {
            if (declaration.Interpolation == null) continue;
            var choice = new Choice(declaration.Interpolation, declaration.Interpolation.Length > 0,
                declaration.Span, declaration.InterpolationSpan ?? declaration.Span,
                declaration.Selector == "default" ? "default" : "indexed");
            if (declaration.Selector == "default") {
                defaults = choice;
                // Existing edges keep the default copied when they were created; later defaults are fallback only.
                if (Before(declaration.Span, edge.Span)) raw = choice.WithOrigin("creation-default");
            } else if (Selects(declaration.Selector, index) && (raw == null || !Before(declaration.Span, raw.GetValueOrDefault().OrderSpan))) raw = choice;
        }
        if (edge.Properties.TryGetValue("curve", out var authored)) {
            var span = edge.PropertySpans.TryGetValue("curve", out var located) ? located : edge.Span;
            var quoted = !edge.PropertyTokens.TryGetValue("curve", out var token) || token.StartsWith("'", StringComparison.Ordinal) || token.StartsWith("\"", StringComparison.Ordinal);
            var metadata = new Choice(authored, Truthy(authored, quoted), span, span, "edge");
            if (raw == null || !Before(span, raw.GetValueOrDefault().OrderSpan)) raw = metadata;
        }
        var configuration = document.Configuration.Last("flowchart.curve");
        var source = configuration == null ? (Choice?)null : new Choice(configuration.Value,
            Truthy(configuration.Value, configuration.IsString), configuration.Span, configuration.Span, "configuration");
        var effective = raw?.Truthy == true ? raw.GetValueOrDefault() : defaults?.Truthy == true ? defaults.GetValueOrDefault()
            : source?.Truthy == true ? source.GetValueOrDefault() : new Choice("basis", true, document.HeaderSpan, document.HeaderSpan, "implicit");
        var known = TryKind(effective.Name, out var interpolation);
        // The pinned lane layout rewrites only a missing/basis request, before the renderer's unknown-token fallback.
        if (document.Kind == MermaidDiagramKind.Swimlane && known && interpolation == TopologyEdgeInterpolation.Basis)
            interpolation = TopologyEdgeInterpolation.Rounded;
        target.Interpolation = interpolation;
        target.InterpolationCornerBlendLength = interpolation == TopologyEdgeInterpolation.Rounded ? null : 5;
        MermaidResourceGuard.CheckInterchangeFactLength(effective.Name?.Length ?? 0, effective.Span);
        target.Metadata["mermaid.curve.requested"] = effective.Name ?? string.Empty;
        target.Metadata["mermaid.curve.effective"] = interpolation.ToString(); target.Metadata["mermaid.curve.origin"] = effective.Origin;
        target.Metadata["mermaid.curve.line"] = effective.Span.Line.ToString(CultureInfo.InvariantCulture);
        target.Metadata["mermaid.curve.column"] = effective.Span.Column.ToString(CultureInfo.InvariantCulture);
        foreach (var property in edge.Properties) {
            var span = edge.PropertySpans.TryGetValue(property.Key, out var propertySpan) ? propertySpan : edge.Span;
            MermaidResourceGuard.CheckInterchangeFactLength(property.Key.Length + "mermaid.propertyLength.".Length, span);
            MermaidResourceGuard.CheckInterchangeFactLength(property.Value.Length, span);
            target.Metadata["mermaid.property." + property.Key] = property.Value;
            if (edge.PropertyTokens.TryGetValue(property.Key, out var token)) { MermaidResourceGuard.CheckInterchangeFactLength(token.Length, span); target.Metadata["mermaid.propertyToken." + property.Key] = token; }
            target.Metadata["mermaid.propertyLine." + property.Key] = span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.propertyColumn." + property.Key] = span.Column.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.propertyLength." + property.Key] = span.Length.ToString(CultureInfo.InvariantCulture);
        }
    }
    private static bool Selects(string selector, int index) {
        foreach (var value in selector.Split(',')) if (int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var selected) && selected == index) return true;
        return false;
    }
    private static bool Before(MermaidSourceSpan left, MermaidSourceSpan right) => left.Line < right.Line || left.Line == right.Line && left.Column < right.Column;
    // The bounded scalar owner supplies literal strings versus typed source values; no expression is evaluated.
    private static bool Truthy(string? value, bool isString) {
        if (isString) return value != null && value.Length > 0;
        if (value == null || value is "null" or "Null" or "NULL" or "~" or "false" or "False" or "FALSE") return false;
        return !MermaidConfigurationNumber.TryParse(value, out var number) || number != 0;
    }
    private readonly struct Choice {
        internal Choice(string? name, bool truthy, MermaidSourceSpan order, MermaidSourceSpan span, string origin) {
            Name = name; Truthy = truthy; OrderSpan = order; Span = span; Origin = origin;
        }
        internal string? Name { get; }
        internal bool Truthy { get; }
        internal MermaidSourceSpan OrderSpan { get; }
        internal MermaidSourceSpan Span { get; }
        internal string Origin { get; }
        internal Choice WithOrigin(string value) => new(Name, Truthy, OrderSpan, Span, value);
    }
}
