using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private static void ParseSeriesCommand(MarkupParseResult<MarkupChartDocument> result, ChartState state, List<string> tokens, int lineNumber) {
        try {
            RequireTokenCount(tokens, 4, "series");
            var series = GetOrAddSeries(state, tokens[1]);
            var valueCountBefore = series.Values.Count;
            var readingValues = false;
            for (var i = 2; i < tokens.Count; i++) {
                if (TrySplitAttribute(tokens[i], out var attributeKey, out var attributeValue)) {
                    var normalizedAttributeKey = NormalizeKey(attributeKey);
                    if (normalizedAttributeKey == "type" || normalizedAttributeKey == "kind") {
                        series.Type = ValidateChartType(attributeValue);
                        readingValues = false;
                        continue;
                    }

                    if (normalizedAttributeKey == "color") {
                        series.Color = attributeValue;
                        ValidateColor(series.Color, "Series color");
                        readingValues = false;
                        continue;
                    }
                }

                var key = NormalizeKey(tokens[i].TrimEnd(':'));
                if (key == "type" || key == "kind") {
                    if (i + 1 >= tokens.Count) throw new ArgumentException("Series type requires a value.");
                    series.Type = ValidateChartType(tokens[++i]);
                    readingValues = false;
                    continue;
                }

                if (key == "color") {
                    if (i + 1 >= tokens.Count) throw new ArgumentException("Series color requires a value.");
                    series.Color = tokens[++i];
                    ValidateColor(series.Color, "Series color");
                    readingValues = false;
                    continue;
                }

                if (key == "values") {
                    readingValues = true;
                    continue;
                }

                if (!readingValues && double.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var firstValue)) {
                    series.Values.Add(firstValue);
                    readingValues = true;
                    continue;
                }

                if (readingValues) {
                    series.Values.Add(ParseDouble(tokens[i]));
                    continue;
                }

                Add(result, lineNumber, MarkupDiagnosticSeverity.Warning, "Unknown chart series token '" + tokens[i] + "'.");
            }

            if (series.Values.Count == valueCountBefore) Add(result, lineNumber, MarkupDiagnosticSeverity.Error, "Chart series '" + series.Name + "' must declare at least one numeric value.");
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            Add(result, lineNumber, MarkupDiagnosticSeverity.Error, ex.Message);
        }
    }

    private static void ParseAnnotationCommand(ChartState state, List<string> tokens) {
        RequireTokenCount(tokens, 3, "annotation");
        var annotation = new ChartAnnotationSpec {
            Kind = tokens[1],
            Start = ParseDouble(tokens[2])
        };
        var next = 3;
        var kind = NormalizeKey(annotation.Kind);
        if ((kind == "horizontalband" || kind == "hband" || kind == "verticalband" || kind == "vband") && tokens.Count > 3 && !IsAttribute(tokens[3])) {
            annotation.End = ParseDouble(tokens[3]);
            next = 4;
        }

        if (tokens.Count > next && !IsAttribute(tokens[next])) {
            annotation.Label = tokens[next];
            next++;
        }

        var attributes = Attributes(tokens, next);
        if (attributes.TryGetValue("end", out var end)) annotation.End = ParseDouble(end);
        if (attributes.TryGetValue("label", out var label)) annotation.Label = label;
        if (attributes.TryGetValue("color", out var color)) {
            ValidateColor(color, "Annotation color");
            annotation.Color = color;
        }

        if (attributes.TryGetValue("opacity", out var opacity)) annotation.Opacity = ParseDouble(opacity);
        state.Annotations.Add(annotation);
    }

    private static void ApplyAnnotations(Chart chart, ChartState state) {
        foreach (var annotation in state.Annotations) {
            var color = ParseColor(annotation.Color);
            switch (NormalizeKey(annotation.Kind)) {
                case "horizontalline":
                case "hline":
                    chart.AddHorizontalLine(annotation.Start, annotation.Label, color);
                    break;
                case "verticalline":
                case "vline":
                    chart.AddVerticalLine(annotation.Start, annotation.Label, color);
                    break;
                case "horizontalband":
                case "hband":
                    chart.AddHorizontalBand(annotation.Start, annotation.End ?? annotation.Start, annotation.Label, color, annotation.Opacity ?? 0.14);
                    break;
                case "verticalband":
                case "vband":
                    chart.AddVerticalBand(annotation.Start, annotation.End ?? annotation.Start, annotation.Label, color, annotation.Opacity ?? 0.14);
                    break;
                default:
                    throw new ArgumentException("Unknown chart annotation kind '" + annotation.Kind + "'.");
            }
        }
    }

    private static ChartSeriesSpec GetOrAddSeries(ChartState state, string name) {
        if (!state.SeriesByName.TryGetValue(name, out var series)) {
            series = new ChartSeriesSpec { Name = name };
            state.SeriesByName[name] = series;
            state.Series.Add(series);
        }

        return series;
    }

    private static ChartColor? ParseColor(string? value) =>
        string.IsNullOrWhiteSpace(value) ? (ChartColor?)null : ChartColor.FromHex(value!);

    private static void ValidateColor(string? value, string name) {
        if (string.IsNullOrWhiteSpace(value)) return;
        try {
            ChartColor.FromHex(value!);
        } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
            throw new ArgumentException(name + " must be a valid hex color.", ex);
        }
    }
}
