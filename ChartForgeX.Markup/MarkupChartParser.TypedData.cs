using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private sealed partial class ChartState {
        internal readonly List<ChartCandlestick> Candles = new();
        internal readonly List<(ChartHierarchyItem Value, int Line)> Items = new();
        internal int? CandleLine;
        internal int? GaugeOptionLine;
        internal double GaugeMinimum, GaugeMaximum = 100;
    }

    private static bool UsesTypedData(string type) => NormalizeKey(type) is "chord" or "sankey" or "treemap" or "candlestick" or "ohlc";

    private static bool TryParseFamilyCommand(ChartState state, List<string> tokens, int lineNumber) {
        switch (NormalizeKey(tokens[0].TrimEnd(':'))) {
            case "candle":
                RequireTokenCount(tokens, 6, "candle");
                if (tokens.Count != 6) throw new ArgumentException("Candle commands require x, open, high, low and close values.");
                state.Candles.Add(new ChartCandlestick(ParseDouble(tokens[1]), ParseDouble(tokens[2]), ParseDouble(tokens[3]), ParseDouble(tokens[4]), ParseDouble(tokens[5])));
                state.CandleLine ??= lineNumber;
                return true;
            case "item":
                RequireTokenCount(tokens, 3, "item");
                for (var i = 3; i < tokens.Count; i++) if (!IsAttribute(tokens[i]))
                    throw new ArgumentException("Treemap item attributes require key=value syntax.");
                var attributes = Attributes(tokens, 3);
                foreach (var key in attributes.Keys) if (key is not ("parent" or "value" or "colorvalue"))
                    throw new ArgumentException("Unknown treemap item attribute '" + key + "'.");
                state.Items.Add((CreateHierarchyItem(tokens[1], tokens[2], attributes), lineNumber));
                return true;
            case "theme":
            case "thememode":
            case "gaugeminimum":
            case "gaugemaximum":
                RequireTokenCount(tokens, 2, tokens[0]);
                if (tokens.Count != 2) throw new ArgumentException("Chart command '" + tokens[0] + "' requires one value.");
                return TryParseFamilyOption(state, tokens[0].TrimEnd(':'), tokens[1], lineNumber);
            default:
                return false;
        }
    }

    private static ChartHierarchyItem CreateHierarchyItem(string id, string label, Dictionary<string, string> attributes) =>
        new(id, label, OptionalText(attributes, "parent"), OptionalNumber(attributes, "value"), OptionalNumber(attributes, "colorvalue"));

    private static string? OptionalText(Dictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text) ? text : null;

    private static double? OptionalNumber(Dictionary<string, string> row, string key) =>
        OptionalText(row, key) is string text ? ParseDouble(text) : (double?)null;

    private static void ParseTypedSectionCommand(MarkupParseResult<MarkupChartDocument> result, ChartState state,
        string section, string line, int number) {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;
        if (NormalizeKey(tokens[0]) == (section == "candles" ? "candle" : "item")) ParseCommand(result, state, line, number);
        else Add(result, number, MarkupDiagnosticSeverity.Error, "Chart command requires end before leaving the " + section + " section.");
    }

    private static List<string>? ParseTypedTableLine(MarkupParseResult<MarkupChartDocument> result, ChartState state,
        string section, List<string>? headers, string line, int number) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0 || IsTableSeparator(cells)) return headers;
        if (headers == null) return cells;
        try {
            var row = Row(headers, cells);
            if (section == "candles") {
                state.Candles.Add(new ChartCandlestick(ParseDouble(Required(row, "x")), ParseDouble(Required(row, "open")),
                    ParseDouble(Required(row, "high")), ParseDouble(Required(row, "low")), ParseDouble(Required(row, "close"))));
                state.CandleLine ??= number;
            } else state.Items.Add((CreateHierarchyItem(Required(row, "id"), Required(row, "label"), row), number));
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, number, MarkupDiagnosticSeverity.Error, ex.Message);
        }
        return headers;
    }

    private static void ValidateTypedSource(MarkupParseResult<MarkupChartDocument> result, ChartState state, int fenceLine) {
        var type = NormalizeKey(state.Type);
        if (state.GaugeOptionLine is int optionLine) {
            try { _ = new ChartGaugeValue(0, state.GaugeMinimum, state.GaugeMaximum); }
            catch (ArgumentException ex) { Add(result, optionLine, MarkupDiagnosticSeverity.Error, ex.Message); }
        }
        if (state.Series.Any(series => UsesTypedData(series.Type) && NormalizeKey(series.Type) != "chord"))
            Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "Typed chart families require their chart type and typed data declarations instead of numeric series.");
        if (state.Candles.Count > 0 && type is not ("candlestick" or "ohlc"))
            Add(result, state.CandleLine ?? fenceLine, MarkupDiagnosticSeverity.Error, "Candle declarations require type candlestick or ohlc.");
        if (state.Items.Count > 0 && type != "treemap")
            Add(result, state.Items[0].Line, MarkupDiagnosticSeverity.Error, "Hierarchy items require type treemap.");
        if (type is not ("treemap" or "candlestick" or "ohlc")) return;
        if (state.Values.Count > 0 || state.Series.Count > 0)
            Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "Typed chart families use typed data declarations instead of numeric values or series.");
        if (type == "treemap") {
            if (state.Labels.Count > 0) Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "Treemap labels belong to hierarchy items.");
            if (state.Items.Count == 0) Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "Treemap markup requires at least one hierarchy item.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in state.Items) if (!ids.Add(item.Value.Id))
                Add(result, item.Line, MarkupDiagnosticSeverity.Error, "Duplicate treemap item ID '" + item.Value.Id + "'.");
            foreach (var item in state.Items) if (item.Value.ParentId is string parent && !ids.Contains(parent))
                Add(result, item.Line, MarkupDiagnosticSeverity.Error, "Treemap parent '" + parent + "' requires a declared item ID.");
        } else if (state.Candles.Count == 0) Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "Financial markup requires at least one candle.");
    }

    private static bool BuildTypedChart(Chart chart, ChartState state) {
        switch (NormalizeKey(state.Type)) {
            case "chord": BuildChord(chart, state); return true;
            case "candlestick": chart.AddCandlestick(state.SeriesName, state.Candles); return true;
            case "ohlc": chart.AddOhlc(state.SeriesName, state.Candles); return true;
            case "treemap": chart.AddTreemap(state.SeriesName, state.Items.Select(item => item.Value)); return true;
            case "sankey": chart.AddSankey(state.SeriesName, state.ChordNodes.Select(row => row.Value), state.ChordFlows.Select(row => row.Value)); return true;
            default: return false;
        }
    }
}
