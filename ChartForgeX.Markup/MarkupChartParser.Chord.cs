using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private sealed partial class ChartState {
        internal readonly List<(ChartNode Value, int Line)> ChordNodes = new();
        internal readonly List<(ChartFlowLink Value, int Line)> ChordFlows = new();
        internal readonly Dictionary<string, int> ChordOptionLines = new(StringComparer.Ordinal);
        internal string[]? ChordNodeOrder, ChordOutgoingOrder, ChordIncomingOrder;
        internal double? ChordTension;
    }

    private static void ParseChordCommand(ChartState state, List<string> tokens, int line) {
        if (NormalizeKey(tokens[0]) == "node") {
            RequireTokenCount(tokens, 3, "node");
            state.ChordNodes.Add((new ChartNode(tokens[1], JoinTail(tokens, 2)), line));
            return;
        }
        RequireTokenCount(tokens, 5, "flow");
        for (var index = 5; index < tokens.Count; index++) if (!IsAttribute(tokens[index]))
            throw new ArgumentException("Chord flow attributes require key=value syntax.");
        var attributes = Attributes(tokens, 5);
        foreach (var key in attributes.Keys) if (key != "colorvalue")
            throw new ArgumentException("Unknown Chord flow attribute '" + key + "'.");
        double? color = attributes.TryGetValue("colorvalue", out var value) ? ParseDouble(value) : (double?)null;
        state.ChordFlows.Add((new ChartFlowLink(tokens[1], tokens[2], tokens[3], ParseDouble(tokens[4]), color), line));
    }

    private static void ParseChordSectionCommand(MarkupParseResult<MarkupChartDocument> result, ChartState state,
        string section, string line, int number) {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;
        if (NormalizeKey(tokens[0]) == (section == "nodes" ? "node" : "flow")) ParseCommand(result, state, line, number);
        else Add(result, number, MarkupDiagnosticSeverity.Error, "Chart command requires end before leaving the " + section + " section.");
    }

    private static List<string>? ParseChordTableLine(MarkupParseResult<MarkupChartDocument> result, ChartState state,
        string section, List<string>? headers, string line, int number) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0 || IsTableSeparator(cells)) return headers;
        if (headers == null) return cells;
        try {
            var row = Row(headers, cells);
            if (section == "nodes") state.ChordNodes.Add((new ChartNode(Required(row, "id"), Required(row, "label")), number));
            else {
                double? color = row.TryGetValue("colorvalue", out var text) && !string.IsNullOrWhiteSpace(text) ? ParseDouble(text) : (double?)null;
                state.ChordFlows.Add((new ChartFlowLink(Required(row, "id"), Required(row, "source"), Required(row, "target"), ParseDouble(Required(row, "value")), color), number));
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, number, MarkupDiagnosticSeverity.Error, ex.Message);
        }
        return headers;
    }

    private static bool TryParseChordOption(ChartState state, string key, string value, int line) {
        var normalized = NormalizeKey(key);
        if (normalized is "chordnodeorder" or "chordoutgoingfloworder" or "chordincomingfloworder")
            return TryParseChordOrderOption(state, key, Tokenize(value), line);
        if (normalized != "chordribbontension") return false;
        state.ChordTension = ParseUnitIntervalDouble(value, key);
        state.ChordOptionLines[normalized] = line;
        return true;
    }

    // Command values have already been tokenized; joining them would erase quoted identity boundaries.
    // Option-table values enter through the same owner after their one necessary tokenization.
    private static bool TryParseChordOrderOption(ChartState state, string key, IEnumerable<string> ids, int line) {
        var normalized = NormalizeKey(key);
        switch (normalized) {
            case "chordnodeorder": state.ChordNodeOrder = ids.ToArray(); break;
            case "chordoutgoingfloworder": state.ChordOutgoingOrder = ids.ToArray(); break;
            case "chordincomingfloworder": state.ChordIncomingOrder = ids.ToArray(); break;
            default: return false;
        }
        // This locates source diagnostics; Core remains the authoritative relationship/order validator.
        state.ChordOptionLines[normalized] = line;
        _ = new ChartChordOrdering(state.ChordNodeOrder, state.ChordOutgoingOrder, state.ChordIncomingOrder);
        return true;
    }

    private static void ValidateChordSource(MarkupParseResult<MarkupChartDocument> result, ChartState state, int fenceLine) {
        var isChord = NormalizeKey(state.Type) == "chord";
        if (state.Series.Any(series => NormalizeKey(series.Type) == "chord"))
            Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "Numeric series cannot invent relationships; use type chord with typed node/flow declarations.");
        if (!isChord) {
            if (state.ChordNodes.Count + state.ChordFlows.Count + state.ChordOptionLines.Count > 0)
                Add(result, state.ChordNodes.Count > 0 ? state.ChordNodes[0].Line : state.ChordFlows.Count > 0 ? state.ChordFlows[0].Line : state.ChordOptionLines.Values.First(),
                    MarkupDiagnosticSeverity.Error, "Typed nodes, flows and Chord options require type chord.");
            return;
        }
        if (state.Values.Count > 0 || state.Series.Count > 0 || state.Labels.Count > 0)
            Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "Chord markup uses typed node/flow declarations; numeric values, categories and numeric series cannot invent relationships.");
        var nodes = new HashSet<string>(StringComparer.Ordinal); var flows = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in state.ChordNodes) if (!nodes.Add(row.Value.Id)) Add(result, row.Line, MarkupDiagnosticSeverity.Error, "Duplicate Chord node ID '" + row.Value.Id + "'.");
        foreach (var row in state.ChordFlows) {
            if (!flows.Add(row.Value.Id)) Add(result, row.Line, MarkupDiagnosticSeverity.Error, "Duplicate Chord flow ID '" + row.Value.Id + "'.");
            if (!nodes.Contains(row.Value.SourceId) || !nodes.Contains(row.Value.TargetId))
                Add(result, row.Line, MarkupDiagnosticSeverity.Error, "Chord flow '" + row.Value.Id + "' requires declared source and target node IDs.");
        }
        ValidateChordOrderSource(result, state.ChordNodeOrder, nodes, state, "chordnodeorder");
        ValidateChordOrderSource(result, state.ChordOutgoingOrder, flows, state, "chordoutgoingfloworder");
        ValidateChordOrderSource(result, state.ChordIncomingOrder, flows, state, "chordincomingfloworder");
    }

    private static void ValidateChordOrderSource(MarkupParseResult<MarkupChartDocument> result, string[]? order,
        HashSet<string> ids, ChartState state, string key) {
        if (order == null) return;
        if (order.Length != ids.Count || order.Any(id => !ids.Contains(id)))
            Add(result, state.ChordOptionLines[key], MarkupDiagnosticSeverity.Error, "Chart " + key + " must contain every declared ID exactly once.");
    }

    private static void BuildChord(Chart chart, ChartState state) {
        // The Core intake owns finite totals, directed endpoints, parallel/self/zero facts and immutable snapshots.
        chart.AddChord(state.SeriesName, state.ChordNodes.Select(row => row.Value), state.ChordFlows.Select(row => row.Value));
        chart.ConfigureChord(options => {
            if (state.ChordNodeOrder != null || state.ChordOutgoingOrder != null || state.ChordIncomingOrder != null)
                options.Ordering = new ChartChordOrdering(state.ChordNodeOrder, state.ChordOutgoingOrder, state.ChordIncomingOrder);
            if (state.ChordTension.HasValue) options.RibbonTension = state.ChordTension.Value;
        });
    }
}
