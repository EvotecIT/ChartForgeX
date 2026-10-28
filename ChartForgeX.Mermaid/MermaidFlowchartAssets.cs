using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Resolves finite caller-owned Flowchart assets without turning source values into asset requests.</summary>
internal static partial class MermaidFlowchartAssets {
    internal static bool IsMetadataKey(string key) => key is "icon" or "img" or "form" or "pos" or "w" or "h" or "constraint";
    internal static bool HasAsset(MermaidFlowchartNode node) => Value(node, "img") != null || Value(node, "icon") != null;
    private static string? Value(MermaidFlowchartNode node, string key) => node.Properties.TryGetValue(key, out var value) && value.Length > 0 ? value : null;
    private static MermaidSourceSpan Span(MermaidFlowchartNode node, string key) => node.PropertyValueSpans.TryGetValue(key, out var value) ? value
        : node.PropertySpans.TryGetValue(key, out value) ? value : node.Span;
    private static bool HasCaption(MermaidFlowchartNode node) => node.Properties.TryGetValue("label", out var declared) && !string.IsNullOrWhiteSpace(declared)
        || !string.IsNullOrWhiteSpace(node.Text);

    internal static IEnumerable<MermaidDiagnostic> Diagnostics(MermaidFlowchartDocument document, MermaidFlowchartRenderOptions options) {
        var catalog = options.IconCatalog ?? TopologyIconCatalog.Default();
        foreach (var node in document.Nodes) {
            if (!HasAsset(node)) {
                foreach (var key in node.Properties.Keys) if (IsMetadataKey(key))
                    yield return Warning(Span(node, key), "Flowchart property '" + key + "' is retained without an active finite icon/image binding.");
                continue;
            }
            foreach (var key in new[] { "w", "h" }) {
                if (!Dimension(node, key, out _, out var error)) yield return new MermaidDiagnostic {
                    Code = MermaidDiagnosticCodes.InvalidStatement, Severity = MermaidDiagnosticSeverity.Error, Span = Span(node, key), Message = error!
                };
            }
            var image = Value(node, "img");
            if (image != null) {
                if (options.ImageBindings == null || !options.ImageBindings.ContainsKey(image))
                    yield return Warning(Span(node, "img"), "Flowchart image identity '" + image + "' has no finite caller-owned binding; its source facts remain inert.");
                continue;
            }
            var identity = Value(node, "icon")!; var icon = catalog.Resolve(identity);
            if (icon == null || !string.Equals(icon.QualifiedId, identity, StringComparison.Ordinal))
                yield return Warning(Span(node, "icon"), "Flowchart icon '" + identity + "' is not an exact qualified native catalog identity; its source facts remain inert.");
            else if (icon.Artwork != null && (!string.IsNullOrWhiteSpace(icon.Artwork.SvgPath) || !string.IsNullOrWhiteSpace(icon.Artwork.PreviewPath)
                || icon.Artwork.ImageHref is string href && !href.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)))
                yield return Warning(Span(node, "icon"), "Flowchart catalog artwork references stay inert; only captured inline artwork and the finite native glyph are used.");
        }
        foreach (var group in document.Subgraphs) foreach (var key in group.Properties.Keys) if (IsMetadataKey(key))
            yield return Warning(group.PropertyValueSpans.TryGetValue(key, out var span) ? span : group.Span,
                "Flowchart subgraph asset property '" + key + "' is retained without a native container asset mapping.");
    }

    private static MermaidDiagnostic Warning(MermaidSourceSpan span, string message) => new() {
        Code = MermaidDiagnosticCodes.UnsupportedStatement, Severity = MermaidDiagnosticSeverity.Warning, Span = span, Message = message
    };

    private static bool Dimension(MermaidFlowchartNode node, string key, out double? dimension, out string? error) {
        dimension = null; error = null;
        if (!node.Properties.TryGetValue(key, out var value)) return true;
        var token = node.PropertyTokens.TryGetValue(key, out var retained) ? retained : value;
        double number;
        if (token is "false" or "null") number = 0;
        else if (token == "true") number = 1;
        else if (string.IsNullOrWhiteSpace(value)) number = 0;
        else if (RadixNumber(value, out var radix)) number = radix;
        else if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return true; // Pinned Number(value) NaN is a falsy/default value.
        if (number == 0 || double.IsNaN(number)) return true;
        if (number < 0 || double.IsInfinity(number) || number > TopologyPathGeometry.MaximumCoordinate) {
            error = "Flowchart asset '" + key + "' must resolve to a finite positive dimension within the native coordinate limit, or zero for its pinned default.";
            return false;
        }
        dimension = number; return true;
    }

    private static bool RadixNumber(string value, out double result) {
        result = 0; var text = value.Trim();
        if (text.Length < 3 || text[0] != '0') return false;
        var radix = text[1] is 'x' or 'X' ? 16 : text[1] is 'b' or 'B' ? 2 : text[1] is 'o' or 'O' ? 8 : 0;
        if (radix == 0) return false;
        for (var index = 2; index < text.Length; index++) {
            var ch = text[index]; var digit = ch is >= '0' and <= '9' ? ch - '0' : ch is >= 'a' and <= 'f' ? ch - 'a' + 10 : ch is >= 'A' and <= 'F' ? ch - 'A' + 10 : -1;
            if (digit < 0 || digit >= radix) return false;
            result = result * radix + digit;
        }
        return true;
    }
}
