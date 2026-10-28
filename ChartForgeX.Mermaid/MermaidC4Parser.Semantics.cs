using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidC4Parser {
    private static bool IsDeployment(string name) { var kind = NormalizeName(name); return kind == "deploymentnode" || kind == "node" || kind == "nodel" || kind == "noder"; }
    private static string? BoundaryType(CallStatement call) => IsDeployment(call.Name) || NormalizeName(call.Name) == "boundary" ? OptionalArg(call.Arguments, 2, 0, "type") : null;

    private static void ParseIndexedRelationship(MermaidC4Document document, Dictionary<string, MermaidC4Element> elements, Dictionary<string, MermaidC4Boundary> boundaries, CallStatement call, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (call.Arguments.Count < 4) { MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "RelIndex requires index, source, target, and label."); return; }
        var source = Clean(call.Arguments[1]); var target = Clean(call.Arguments[2]);
        if ((!elements.ContainsKey(source) && !boundaries.ContainsKey(source)) || (!elements.ContainsKey(target) && !boundaries.ContainsKey(target))) { MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "RelIndex endpoints must refer to previously declared elements or boundaries."); return; }
        document.Relationships.Add(new MermaidC4Relationship(source, target, "relindex", span) {
            AuthoredIndex = Clean(call.Arguments[0]), Label = Arg(call.Arguments, 3), Tags = OptionalArg(call.Arguments, 4, 0, "tags"), Link = OptionalArg(call.Arguments, 4, 1, "link")
        });
    }

    private static bool TryParseNativeUpdate(MermaidC4Document document, Dictionary<string, MermaidC4Element> elements, Dictionary<string, MermaidC4Boundary> boundaries, CallStatement call, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var name = NormalizeName(call.Name);
        if (name == "updatelayoutconfig") { ParseLayoutConfig(document, call, span, result); return true; }
        if (name == "updateelementstyle") { ParseElementPresentation(document, elements, boundaries, call, span, result); return true; }
        if (name != "updaterelstyle") return false;
        if (call.Arguments.Count < 2) { MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "UpdateRelStyle requires source and target aliases."); return true; }
        var from = Clean(call.Arguments[0]); var to = Clean(call.Arguments[1]);
        if (!document.Relationships.Exists(edge => edge.SourceAlias == from && edge.TargetAlias == to)) { MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Unknown C4 relationship style target '" + from + "' to '" + to + "'."); return true; }
        var update = new MermaidC4RelationshipStyle(from, to, span) {
            TextColor = ReadColor(OptionalArg(call.Arguments, 2, 0, "textColor")), LineColor = ReadColor(OptionalArg(call.Arguments, 2, 1, "lineColor")),
            OffsetX = Offset(OptionalArg(call.Arguments, 2, 2, "offsetX")), OffsetY = Offset(OptionalArg(call.Arguments, 2, 3, "offsetY"))
        };
        document.RelationshipStyles.Add(update);
        if (call.Arguments.Count > 6 || HasNamedUnsupported(call.Arguments, "textcolor", "linecolor", "offsetx", "offsety")) Retain(document, result, call.Name, span, "Unsupported C4 relationship style arguments remain retained: ");
        return true;
        string? ReadColor(string? value) { if (value == null) return null; if (MermaidStyleValues.IsColor(value)) return value; MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Invalid C4 relationship color: " + value + "."); return null; }
        double? Offset(string? value) { if (value == null) return null; if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var offset) && !double.IsNaN(offset) && !double.IsInfinity(offset)) return offset; MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 label offsets must be finite numbers."); return null; }
    }

    private static bool HasNamedUnsupported(IReadOnlyList<string> args, params string[] supported) => HasNamedUnsupported(args, 1, supported);
    private static bool HasNamedUnsupported(IReadOnlyList<string> args, int firstOptional, params string[] supported) {
        for (var index = firstOptional; index < args.Count; index++) {
            if (!TrySplitNamedArgument(args[index], out var name, out _)) continue;
            var normalized = NormalizeName(name); var known = false;
            foreach (var candidate in supported) if (normalized == candidate) { known = true; break; }
            if (!known) return true;
        }
        return false;
    }
}
