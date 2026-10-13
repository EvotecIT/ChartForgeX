using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Bounds failed inline-label searches to one suffix scan per link family and input.</summary>
internal sealed class MermaidFlowchartInlineLabels {
    private readonly string _text;
    private readonly HashSet<char> _exhausted = new();
    internal MermaidFlowchartInlineLabels(string text) => _text = text;

    internal bool TryRead(int start, out int labelStart, out int labelEnd, out int edgeEnd) {
        labelStart = labelEnd = edgeEnd = start;
        var prefix = MermaidFlowchartOperators.ReadLabelPrefixEnd(_text, start);
        if (prefix == start || _exhausted.Contains(_text[prefix - 1])) return false;
        if (MermaidFlowchartOperators.TryReadInlineLabel(_text, start, out labelStart, out labelEnd, out edgeEnd)) return true;
        // The exhaustive failed search found no closing token in the remaining input.
        // Both statement splitting and chained-edge parsing use this same policy.
        _exhausted.Add(_text[prefix - 1]);
        return false;
    }
}
