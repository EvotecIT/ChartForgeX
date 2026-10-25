using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidWardleyParser {
    private static void ParseEvolutionStages(MermaidWardleyDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        document.Stages.Clear(); document.StageBoundaries.Clear();
        const string name = @"(?:""(?:\\.|[^""])*""|'(?:\\.|[^'])*'|[^@/]+?)";
        var pattern = @"^\s*(?<name>" + name + @")(?:\s*@\s*(?<boundary>[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)))?(?:\s*/\s*(?<second>" + name + @"))?\s*$";
        foreach (var part in text.Split(new[] { "->" }, StringSplitOptions.None)) {
            var match = System.Text.RegularExpressions.Regex.Match(part, pattern, System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            if (!match.Success) { MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Wardley evolution stages must use 'name @ boundary / second label'."); return; }
            var label = MermaidParserUtilities.Unquote(match.Groups["name"].Value.Trim());
            if (match.Groups["second"].Success) label += " / " + MermaidParserUtilities.Unquote(match.Groups["second"].Value.Trim());
            if (label.Trim().Length == 0) { MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Wardley evolution stage labels must not be empty."); return; }
            document.Stages.Add(label);
            if (match.Groups["boundary"].Success) {
                if (!double.TryParse(match.Groups["boundary"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value <= 0 || value > 1 || document.StageBoundaries.Count > 0 && value <= document.StageBoundaries[document.StageBoundaries.Count - 1]) {
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Wardley evolution boundaries must increase within (0,1]."); return;
                }
                document.StageBoundaries.Add(value);
            }
        }
    }

    private static void ParseAnnotationsBox(MermaidWardleyDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!TryParseCoordinatePair(text, true, out var visibility, out var evolution)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Wardley annotation boxes must use 'annotations [visibility,evolution]'."); return;
        }
        if (!TryNormalizeCoordinate(visibility, span, result, out var v) || !TryNormalizeCoordinate(evolution, span, result, out var e)) return;
        if (document.AnnotationsBox != null) { MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Wardley maps may declare only one annotation box."); return; }
        document.AnnotationsBox = new MermaidWardleyAnnotationBox(v, e, span);
    }

    private static bool FiniteWardleyOffset(double x, double y, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!double.IsNaN(x) && !double.IsInfinity(x) && !double.IsNaN(y) && !double.IsInfinity(y)) return true;
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Wardley label offsets must be finite numbers."); return false;
    }

    private static void ResolveWardleyEndpoints(MermaidWardleyDocument document, MermaidParseResult<MermaidDocument> result) {
        var ids = new HashSet<string>(document.Nodes.Select(node => node.Id), StringComparer.Ordinal);
        var labels = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var pipeline in document.Pipelines) foreach (var child in pipeline.Components) {
            var id = pipeline.ParentId + "_" + child.Label;
            if (!ids.Add(id)) MermaidParserUtilities.Add(result, child.Span, MermaidDiagnosticSeverity.Error, "Wardley pipeline endpoint id collides with another declared component: " + id + ".");
            if (!labels.TryGetValue(child.Label, out var matches)) labels.Add(child.Label, matches = new List<string>());
            matches.Add(id);
        }
        string? Resolve(string value, MermaidSourceSpan span) {
            if (ids.Contains(value)) return value;
            if (labels.TryGetValue(value, out var matches) && matches.Count == 1) return matches[0];
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Wardley endpoint must identify one declared component: " + value + "."); return null;
        }
        for (var index = 0; index < document.Links.Count; index++) {
            var link = document.Links[index]; var from = Resolve(link.From, link.Span); var to = Resolve(link.To, link.Span);
            if (from != null && to != null) document.Links[index] = new MermaidWardleyLink(from, to, link.Label, link.Dashed, link.Flow, link.Span);
        }
        for (var index = 0; index < document.Evolutions.Count; index++) {
            var evolution = document.Evolutions[index]; var id = Resolve(evolution.NodeId, evolution.Span);
            if (id != null) document.Evolutions[index] = new MermaidWardleyEvolution(id, evolution.TargetEvolution, evolution.Span);
        }
    }
}
