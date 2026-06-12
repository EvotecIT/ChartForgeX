using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Markup;

/// <summary>
/// Parses ChartForgeX chart markup into renderer-independent chart models.
/// </summary>
public sealed partial class MarkupChartParser {
    /// <summary>
    /// Parses raw chart markup or Markdown containing a chartforgex chart fence.
    /// </summary>
    public MarkupParseResult<MarkupChartDocument> Parse(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var scan = VisualMarkupScanner.Scan(text);
        var result = new MarkupParseResult<MarkupChartDocument>();
        foreach (var diagnostic in scan.Diagnostics) result.Diagnostics.Add(diagnostic);
        foreach (var scannedBlock in scan.Blocks) {
            if (scannedBlock.Kind == VisualMarkupKind.Chart) return ParseBlockCore(scannedBlock, result);
        }

        if (result.Diagnostics.Count > 0) return result;
        var block = CreateRawBlock(text);
        return ParseBlockCore(block, result);
    }

    /// <summary>
    /// Parses a pre-scanned ChartForgeX chart block while preserving fence attributes and source lines.
    /// </summary>
    /// <param name="block">The chart visual block.</param>
    /// <returns>The parse result.</returns>
    public MarkupParseResult<MarkupChartDocument> ParseBlock(VisualMarkupBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        var result = new MarkupParseResult<MarkupChartDocument>();
        if (block.Kind != VisualMarkupKind.Chart) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Expected a ChartForgeX chart visual block.");
            return result;
        }

        return ParseBlockCore(block, result);
    }

    private static MarkupParseResult<MarkupChartDocument> ParseBlockCore(VisualMarkupBlock block, MarkupParseResult<MarkupChartDocument> result) {
        var state = new ChartState();
        if (block.SchemaVersion != 1) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "ChartForgeX chart markup requires schema version v1.");
        ApplyFenceAttributes(result, state, block);
        var lines = block.Payload.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lineOffset = block.StartLine - 1;
        var section = string.Empty;
        List<string>? headers = null;

        for (var index = 0; index < lines.Length; index++) {
            var lineNumber = lineOffset + index + 1;
            var line = StripComment(lines[index]).Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (IsSection(line)) {
                section = NormalizeKey(line.TrimEnd(':'));
                headers = null;
                continue;
            }

            if (section.Length > 0 && IsSectionEnd(line)) {
                section = string.Empty;
                headers = null;
                continue;
            }

            if (section == "options") {
                if (IsTableLine(line, headers)) headers = ParseOptionTableLine(result, state, headers, line, lineNumber);
                else ParseChartOptionLine(result, state, line, lineNumber);
                continue;
            }

            if (IsTableLine(line, headers)) {
                headers = ParseTableLine(result, state, headers, line, lineNumber);
                continue;
            }

            ParseCommand(result, state, line, lineNumber);
        }

        if (state.Values.Count == 0 && state.Series.Count == 0) Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, "Chart markup must declare at least one numeric value or series.");
        if (!result.HasErrors) result.Document = new MarkupChartDocument { Id = state.Id, Chart = BuildChart(state) };
        return result;
    }

    private static VisualMarkupBlock CreateRawBlock(string text) =>
        new VisualMarkupBlock(VisualMarkupKind.Chart, "chartforgex chart", string.Empty, 1, text, 1, 1, Math.Max(1, text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length), EmptyAttributes.Value);

    private static void ApplyFenceAttributes(MarkupParseResult<MarkupChartDocument> result, ChartState state, VisualMarkupBlock block) {
        if (block.Attributes.Count == 0) return;
        try {
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) state.Id = id;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) state.Title = title;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) state.Subtitle = subtitle;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "type", out var type) && !string.IsNullOrWhiteSpace(type)) state.Type = ValidateChartType(type);
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "series", out var series) && !string.IsNullOrWhiteSpace(series)) state.SeriesName = series;
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "size", out var size) && !string.IsNullOrWhiteSpace(size)) ParseSize(state, size);
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "width", out var width) && !string.IsNullOrWhiteSpace(width)) state.Width = ParsePositiveInt32(width, "width");
            if (VisualMarkupFenceOptions.TryGetAttribute(block, "height", out var height) && !string.IsNullOrWhiteSpace(height)) state.Height = ParsePositiveInt32(height, "height");
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, block.FenceLine, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static Chart BuildChart(ChartState state) {
        var chart = Chart.Create()
            .WithTitle(state.Title)
            .WithSubtitle(state.Subtitle)
            .WithSize(state.Width, state.Height);
        if (state.Labels.Count > 0) chart.WithXLabels(state.Labels.ToArray());

        if (state.Series.Count == 0) {
            var points = new ChartPoint[state.Values.Count];
            for (var i = 0; i < points.Length; i++) points[i] = new ChartPoint(i + 1, state.Values[i]);
            AddSeries(chart, state.SeriesName, state.Type, points, null);
        } else {
            foreach (var series in state.Series) {
                var points = new ChartPoint[series.Values.Count];
                for (var i = 0; i < points.Length; i++) points[i] = new ChartPoint(i + 1, series.Values[i]);
                AddSeries(chart, series.Name, string.IsNullOrWhiteSpace(series.Type) ? state.Type : series.Type, points, series.Color);
            }
        }

        ApplyChartOptions(chart, state);
        ApplyAnnotations(chart, state);
        return chart;
    }

    private static void AddSeries(Chart chart, string name, string type, IEnumerable<ChartPoint> points, string? color) {
        var before = chart.Series.Count;
        switch (NormalizeKey(type)) {
            case "line":
                chart.AddLine(name, points, ParseColor(color));
                break;
            case "smoothline":
                chart.AddSmoothLine(name, points, ParseColor(color));
                break;
            case "stepline":
                chart.AddStepLine(name, points, ParseColor(color));
                break;
            case "area":
                chart.AddArea(name, points, ParseColor(color));
                break;
            case "smootharea":
                chart.AddSmoothArea(name, points, ParseColor(color));
                break;
            case "steparea":
                chart.AddStepArea(name, points, ParseColor(color));
                break;
            case "stackedarea":
                chart.AddStackedArea(name, points, ParseColor(color));
                break;
            case "smoothstackedarea":
                chart.AddSmoothStackedArea(name, points, ParseColor(color));
                break;
            case "scatter":
                chart.AddScatter(name, points, ParseColor(color));
                break;
            case "lollipop":
                chart.AddLollipop(name, points, ParseColor(color));
                break;
            case "radar":
                chart.AddRadar(name, points, ParseColor(color));
                break;
            case "funnel":
                chart.AddFunnel(name, points, ParseColor(color));
                break;
            case "polararea":
            case "polar":
                chart.AddPolarArea(name, points);
                break;
            case "donut":
                chart.WithPointLegend().WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).AddDonut(name, points);
                break;
            case "pie":
                chart.WithPointLegend().WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).AddPie(name, points);
                break;
            case "horizontalbar":
            case "hbar":
                chart.AddHorizontalBar(name, points, ParseColor(color));
                break;
            case "waterfall":
                chart.AddWaterfall(name, points, ParseColor(color));
                break;
            case "bar":
            case "column":
                chart.AddBar(name, points, ParseColor(color));
                break;
            default:
                throw new ArgumentException("Unknown chart type '" + type + "'.");
        }

        if (!string.IsNullOrWhiteSpace(color) && chart.Series.Count > before && chart.Series[chart.Series.Count - 1].Color == null) {
            chart.Series[chart.Series.Count - 1].Color = ParseColor(color);
        }
    }

    private static void ParseCommand(MarkupParseResult<MarkupChartDocument> result, ChartState state, string line, int lineNumber) {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;
        var command = NormalizeKey(tokens[0].TrimEnd(':'));
        try {
            switch (command) {
                case "id":
                    RequireTokenCount(tokens, 2, "id");
                    state.Id = tokens[1];
                    break;
                case "title":
                    state.Title = JoinTail(tokens, 1);
                    break;
                case "subtitle":
                    state.Subtitle = JoinTail(tokens, 1);
                    break;
                case "type":
                case "kind":
                    RequireTokenCount(tokens, 2, tokens[0]);
                    state.Type = ValidateChartType(tokens[1]);
                    break;
                case "series":
                    if (tokens.Count == 2) state.SeriesName = tokens[1];
                    else ParseSeriesCommand(result, state, tokens, lineNumber);
                    break;
                case "labels":
                case "categories":
                    state.Labels.Clear();
                    state.Labels.AddRange(tokens.Skip(1));
                    break;
                case "values":
                    state.Values.Clear();
                    for (var i = 1; i < tokens.Count; i++) state.Values.Add(ParseDouble(tokens[i]));
                    break;
                case "value":
                    RequireTokenCount(tokens, 3, "value");
                    state.Labels.Add(tokens[1]);
                    state.Values.Add(ParseDouble(tokens[2]));
                    break;
                case "size":
                    RequireTokenCount(tokens, 2, "size");
                    ParseSize(state, tokens[1]);
                    break;
                case "datalabels":
                case "labelsvisible":
                    state.DataLabels = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "legend":
                    state.ShowLegend = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "pointlegend":
                    state.PointLegend = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "header":
                case "showheader":
                    state.ShowHeader = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "card":
                case "showcard":
                    state.ShowCard = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "plotbackground":
                case "showplotbackground":
                    state.ShowPlotBackground = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "transparent":
                case "transparentbackground":
                    state.TransparentBackground = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "axes":
                    state.ShowAxes = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "xaxisvisible":
                case "showxaxis":
                    state.ShowXAxis = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "yaxisvisible":
                case "showyaxis":
                    state.ShowYAxis = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "axislines":
                case "showaxislines":
                    state.ShowAxisLines = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "grid":
                case "showgrid":
                    state.ShowGrid = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "legendposition":
                    RequireTokenCount(tokens, 2, tokens[0]);
                    state.LegendPosition = VisualMarkupFenceOptions.ParseEnum<ChartLegendPosition>(tokens[1], tokens[0]);
                    break;
                case "tickcount":
                    RequireTokenCount(tokens, 2, tokens[0]);
                    state.TickCount = ParseTickCount(tokens[1], tokens[0]);
                    break;
                case "xaxis":
                case "xtitle":
                    state.XAxisTitle = JoinTail(tokens, 1);
                    break;
                case "yaxis":
                case "ytitle":
                    state.YAxisTitle = JoinTail(tokens, 1);
                    break;
                case "xaxisbounds":
                    RequireTokenCount(tokens, 3, tokens[0]);
                    ParseAxisBounds(tokens[1], tokens[2], "X-axis", out var xMinimum, out var xMaximum);
                    state.XAxisMinimum = xMinimum;
                    state.XAxisMaximum = xMaximum;
                    break;
                case "yaxisbounds":
                    RequireTokenCount(tokens, 3, tokens[0]);
                    ParseAxisBounds(tokens[1], tokens[2], "Y-axis", out var yMinimum, out var yMaximum);
                    state.YAxisMinimum = yMinimum;
                    state.YAxisMaximum = yMaximum;
                    break;
                case "padding":
                    RequireTokenCount(tokens, 2, tokens[0]);
                    state.Padding = ParseNonNegativeFiniteDouble(tokens[1], "padding");
                    break;
                case "sparkline":
                    state.Sparkline = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    break;
                case "overlay":
                    state.Overlay = tokens.Count == 1 || VisualMarkupFenceOptions.ParseBoolean(tokens[1], tokens[0]);
                    if (tokens.Count > 2) state.OverlayShowHeader = VisualMarkupFenceOptions.ParseBoolean(tokens[2], "overlay header");
                    break;
                case "option":
                    RequireTokenCount(tokens, 3, "option");
                    ParseChartOption(result, state, tokens[1], JoinTail(tokens, 2), lineNumber);
                    break;
                case "annotation":
                    ParseAnnotationCommand(state, tokens);
                    break;
                default:
                    Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown chart command '" + tokens[0] + "'.");
                    break;
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static List<string>? ParseTableLine(MarkupParseResult<MarkupChartDocument> result, ChartState state, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) return new List<string>(cells);

        try {
            var row = Row(headers, cells);
            if (row.ContainsKey("value")) {
                var label = Value(row, "label", Value(row, "category", Value(row, "name", string.Empty)));
                var value = Required(row, "value");
                state.Labels.Add(label);
                state.Values.Add(ParseDouble(value));
            } else {
                var labelIndex = FindHeader(headers, "label", "category", "name");
                if (labelIndex < 0) labelIndex = FindFirstNonNumericCell(cells);
                if (labelIndex < 0) throw new ArgumentException("Multi-series chart tables require at least one non-numeric label column.");
                var label = labelIndex < cells.Count ? cells[labelIndex] : string.Empty;
                state.Labels.Add(label);
                for (var i = 0; i < headers.Count && i < cells.Count; i++) {
                    if (i == labelIndex || string.IsNullOrWhiteSpace(cells[i])) continue;
                    var series = GetOrAddSeries(state, headers[i]);
                    series.Values.Add(ParseDouble(cells[i]));
                }
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static List<string>? ParseOptionTableLine(MarkupParseResult<MarkupChartDocument> result, ChartState state, List<string>? headers, string line, int lineNumber) {
        var cells = SplitTableCells(line);
        if (cells.Count == 0) return headers;
        if (IsTableSeparator(cells)) return headers;
        if (headers == null) return new List<string>(cells);

        var row = Row(headers, cells);
        try {
            var key = Value(row, "option", Value(row, "key", Value(row, "name", string.Empty)));
            var value = Value(row, "value", string.Empty);
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Chart options table requires an 'option' or 'key' column.");
            ParseChartOption(result, state, key, value, lineNumber);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }

        return headers;
    }

    private static void ParseChartOptionLine(MarkupParseResult<MarkupChartDocument> result, ChartState state, string line, int lineNumber) {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;
        try {
            ParseChartOption(result, state, tokens[0].TrimEnd(':'), JoinTail(tokens, 1), lineNumber);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static void ParseChartOption(MarkupParseResult<MarkupChartDocument> result, ChartState state, string key, string value, int lineNumber) {
        try {
            switch (NormalizeKey(key)) {
                case "legend":
                case "showlegend":
                    state.ShowLegend = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "pointlegend":
                case "showpointlegend":
                    state.PointLegend = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "datalabels":
                case "showdatalabels":
                    state.DataLabels = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "header":
                case "showheader":
                    state.ShowHeader = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "card":
                case "showcard":
                    state.ShowCard = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "plotbackground":
                case "showplotbackground":
                    state.ShowPlotBackground = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "transparent":
                case "transparentbackground":
                    state.TransparentBackground = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "axes":
                case "showaxes":
                    state.ShowAxes = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "xaxis":
                case "showxaxis":
                    state.ShowXAxis = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "yaxis":
                case "showyaxis":
                    state.ShowYAxis = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "axislines":
                case "showaxislines":
                    state.ShowAxisLines = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "grid":
                case "showgrid":
                    state.ShowGrid = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "legendposition":
                    state.LegendPosition = VisualMarkupFenceOptions.ParseEnum<ChartLegendPosition>(value, key);
                    break;
                case "tickcount":
                    state.TickCount = ParseTickCount(value, key);
                    break;
                case "xaxistitle":
                case "xtitle":
                    state.XAxisTitle = value;
                    break;
                case "yaxistitle":
                case "ytitle":
                    state.YAxisTitle = value;
                    break;
                case "xaxisminimum":
                case "xmin":
                    state.XAxisMinimum = VisualMarkupFenceOptions.ParseDouble(value, key);
                    break;
                case "xaxismaximum":
                case "xmax":
                    state.XAxisMaximum = VisualMarkupFenceOptions.ParseDouble(value, key);
                    break;
                case "yaxisminimum":
                case "ymin":
                    state.YAxisMinimum = VisualMarkupFenceOptions.ParseDouble(value, key);
                    break;
                case "yaxismaximum":
                case "ymax":
                    state.YAxisMaximum = VisualMarkupFenceOptions.ParseDouble(value, key);
                    break;
                case "padding":
                    state.Padding = VisualMarkupFenceOptions.ParseDouble(value, key);
                    break;
                case "sparkline":
                    state.Sparkline = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "overlay":
                    state.Overlay = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                case "overlayshowheader":
                    state.OverlayShowHeader = VisualMarkupFenceOptions.ParseBoolean(value, key);
                    break;
                default:
                    Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown chart option '" + key + "'.");
                    break;
            }
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static Dictionary<string, string> Attributes(List<string> tokens, int start) {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = start; i < tokens.Count; i++) {
            var split = AttributeSplitIndex(tokens[i]);
            if (split <= 0) continue;
            attributes[NormalizeKey(tokens[i].Substring(0, split))] = tokens[i].Substring(split + 1);
        }

        return attributes;
    }

    private static bool IsAttribute(string token) => AttributeSplitIndex(token) > 0;

    private static bool TrySplitAttribute(string token, out string key, out string value) {
        var split = AttributeSplitIndex(token);
        if (split <= 0) {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        key = token.Substring(0, split);
        value = token.Substring(split + 1);
        return true;
    }

    private static int AttributeSplitIndex(string token) {
        var colon = token.IndexOf(':');
        var equals = token.IndexOf('=');
        if (colon <= 0) return equals;
        if (equals <= 0) return colon;
        return Math.Min(colon, equals);
    }

    private static int FindHeader(List<string> headers, params string[] names) {
        for (var index = 0; index < headers.Count; index++) {
            for (var nameIndex = 0; nameIndex < names.Length; nameIndex++) {
                if (NormalizeKey(headers[index]) == NormalizeKey(names[nameIndex])) return index;
            }
        }

        return -1;
    }

    private static int FindFirstNonNumericCell(List<string> cells) {
        for (var index = 0; index < cells.Count; index++) {
            if (!double.TryParse(cells[index], NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return index;
        }

        return -1;
    }

    private static Dictionary<string, string> Row(List<string> headers, List<string> cells) {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count && i < cells.Count; i++) row[NormalizeKey(headers[i])] = cells[i];
        return row;
    }

    private static string Required(Dictionary<string, string> values, string key) {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        throw new ArgumentException("Chart row requires '" + key + "'.");
    }

    private static string Value(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static void ParseSize(ChartState state, string value) {
        var parts = value.Split(new[] { 'x', 'X', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) throw new ArgumentException("Chart size must use WIDTHxHEIGHT syntax.");
        state.Width = ParsePositiveInt32(parts[0], "width");
        state.Height = ParsePositiveInt32(parts[1], "height");
    }

    private static double ParseDouble(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static void ApplyChartOptions(Chart chart, ChartState state) {
        if (!string.IsNullOrWhiteSpace(state.XAxisTitle)) chart.WithXAxis(state.XAxisTitle!);
        if (!string.IsNullOrWhiteSpace(state.YAxisTitle)) chart.WithYAxis(state.YAxisTitle!);
        if (state.ShowAxes.HasValue) chart.WithAxes(state.ShowAxes.Value);
        if (state.ShowXAxis.HasValue) chart.WithXAxisVisible(state.ShowXAxis.Value);
        if (state.ShowYAxis.HasValue) chart.WithYAxisVisible(state.ShowYAxis.Value);
        if (state.ShowAxisLines.HasValue) chart.WithAxisLines(state.ShowAxisLines.Value);
        if (state.ShowGrid.HasValue) chart.WithGrid(state.ShowGrid.Value);
        if (state.ShowLegend.HasValue) chart.WithLegend(state.ShowLegend.Value);
        if (state.PointLegend.HasValue) chart.WithPointLegend(state.PointLegend.Value);
        if (state.LegendPosition.HasValue) chart.WithLegendPosition(state.LegendPosition.Value);
        if (state.ShowHeader.HasValue) chart.WithHeader(state.ShowHeader.Value);
        if (state.ShowCard.HasValue) chart.WithCard(state.ShowCard.Value);
        if (state.ShowPlotBackground.HasValue) chart.WithPlotBackground(state.ShowPlotBackground.Value);
        if (state.TransparentBackground.HasValue) chart.WithTransparentBackground(state.TransparentBackground.Value);
        if (state.DataLabels.HasValue) chart.WithDataLabels(state.DataLabels.Value);
        if (state.TickCount.HasValue) chart.WithTickCount(state.TickCount.Value);
        if (state.XAxisMinimum.HasValue && state.XAxisMaximum.HasValue) chart.WithXAxisBounds(state.XAxisMinimum.Value, state.XAxisMaximum.Value);
        if (state.YAxisMinimum.HasValue && state.YAxisMaximum.HasValue) chart.WithYAxisBounds(state.YAxisMinimum.Value, state.YAxisMaximum.Value);
        if (state.Padding.HasValue) chart.WithPadding(state.Padding.Value, state.Padding.Value, state.Padding.Value, state.Padding.Value);
        if (state.Sparkline.HasValue) chart.WithSparkline(state.Sparkline.Value);
        if (state.Overlay.HasValue && state.Overlay.Value) chart.WithOverlay(state.OverlayShowHeader);
    }

    private static void RequireTokenCount(List<string> tokens, int count, string command) {
        if (tokens.Count < count) throw new ArgumentException("Chart command '" + command + "' requires a value.");
    }

    private static string JoinTail(List<string> tokens, int start) =>
        start >= tokens.Count ? string.Empty : string.Join(" ", tokens.Skip(start));

    private static string StripComment(string line) {
        var inQuote = false;
        for (var i = 0; i < line.Length - 1; i++) {
            if (line[i] == '"') inQuote = !inQuote;
            if (!inQuote && line[i] == '/' && line[i + 1] == '/' && (i == 0 || char.IsWhiteSpace(line[i - 1]))) return line.Substring(0, i);
        }

        return line;
    }

    private static List<string> Tokenize(string line) {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuote = false;
        for (var i = 0; i < line.Length; i++) {
            var ch = line[i];
            if (ch == '"') {
                inQuote = !inQuote;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuote) {
                if (current.Length > 0) {
                    tokens.Add(current.ToString());
                    current.Length = 0;
                }

                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0) tokens.Add(current.ToString());
        return tokens;
    }

    private static bool IsTableLine(string line, List<string>? headers) {
        if (line.IndexOf("|", StringComparison.Ordinal) < 0) return false;
        if (line.TrimStart().StartsWith("|", StringComparison.Ordinal)) return true;
        return headers != null;
    }

    private static List<string> SplitTableCells(string line) {
        var text = line.Trim();
        if (text.StartsWith("|", StringComparison.Ordinal)) text = text.Substring(1);
        if (text.EndsWith("|", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 1);
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var escaped = false;
        foreach (var ch in text) {
            if (escaped) {
                current.Append(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\') {
                escaped = true;
                continue;
            }

            if (ch == '|') {
                cells.Add(current.ToString().Trim());
                current.Length = 0;
                continue;
            }

            current.Append(ch);
        }

        cells.Add(current.ToString().Trim());
        return cells;
    }

    private static bool IsTableSeparator(List<string> cells) {
        if (cells.Count == 0) return false;
        foreach (var cell in cells) {
            var value = cell.Trim().Trim(':');
            if (value.Length == 0 || value.Any(ch => ch != '-')) return false;
        }

        return true;
    }

    private static bool IsSection(string line) {
        var normalized = NormalizeKey(line.TrimEnd(':'));
        return normalized == "options" || normalized == "data";
    }

    private static bool IsSectionEnd(string line) => string.Equals(line, "end", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeKey(string value) {
        var chars = new List<char>(value.Length);
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch)) chars.Add(char.ToLowerInvariant(ch));
        }

        return new string(chars.ToArray());
    }

    private static void Add(MarkupParseResult<MarkupChartDocument> result, int line, MarkupDiagnosticSeverity severity, string message) {
        result.Diagnostics.Add(new MarkupDiagnostic {
            Line = line,
            Severity = severity,
            Message = message
        });
    }

    private sealed class ChartState {
        public string Id { get; set; } = "chart";
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Type { get; set; } = "bar";
        public string SeriesName { get; set; } = "Series";
        public int Width { get; set; } = 820;
        public int Height { get; set; } = 460;
        public bool? DataLabels { get; set; }
        public bool? ShowLegend { get; set; }
        public bool? PointLegend { get; set; }
        public bool? ShowHeader { get; set; }
        public bool? ShowCard { get; set; }
        public bool? ShowPlotBackground { get; set; }
        public bool? TransparentBackground { get; set; }
        public bool? ShowAxes { get; set; }
        public bool? ShowXAxis { get; set; }
        public bool? ShowYAxis { get; set; }
        public bool? ShowAxisLines { get; set; }
        public bool? ShowGrid { get; set; }
        public bool? Sparkline { get; set; }
        public bool? Overlay { get; set; }
        public bool OverlayShowHeader { get; set; }
        public ChartLegendPosition? LegendPosition { get; set; }
        public int? TickCount { get; set; }
        public double? XAxisMinimum { get; set; }
        public double? XAxisMaximum { get; set; }
        public double? YAxisMinimum { get; set; }
        public double? YAxisMaximum { get; set; }
        public double? Padding { get; set; }
        public string? XAxisTitle { get; set; }
        public string? YAxisTitle { get; set; }
        public List<string> Labels { get; } = new();
        public List<double> Values { get; } = new();
        public List<ChartSeriesSpec> Series { get; } = new();
        public Dictionary<string, ChartSeriesSpec> SeriesByName { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<ChartAnnotationSpec> Annotations { get; } = new();
    }

    private sealed class ChartSeriesSpec {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Color { get; set; }
        public List<double> Values { get; } = new();
    }

    private sealed class ChartAnnotationSpec {
        public string Kind { get; set; } = string.Empty;
        public double Start { get; set; }
        public double? End { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Color { get; set; }
        public double? Opacity { get; set; }
    }

    private static class EmptyAttributes {
        public static readonly IReadOnlyDictionary<string, string> Value = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
