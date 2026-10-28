using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidFlowchartRendering {
    private static void ApplyUseCaseActor(TopologyNode target, MermaidFlowchartNode source, TopologyIconCatalog catalog) {
        foreach (var key in new[] { "type", "business", "icon" }) {
            if (!source.Properties.ContainsKey(key)) continue;
            var prefix = "mermaid.property." + key;
            if (source.PropertyTokens.TryGetValue(key, out var token)) target.Metadata[prefix + ".raw"] = token;
            if (source.PropertySpans.TryGetValue(key, out var span)) {
                target.Metadata[prefix + ".line"] = span.Line.ToString(CultureInfo.InvariantCulture);
                target.Metadata[prefix + ".column"] = span.Column.ToString(CultureInfo.InvariantCulture);
                target.Metadata[prefix + ".length"] = span.Length.ToString(CultureInfo.InvariantCulture);
            }
        }
        var business = source.Properties.TryGetValue("business", out var authoredBusiness) && authoredBusiness == "true";
        if (source.Shape != MermaidFlowchartNodeShape.Actor) {
            if (business && source.Shape == MermaidFlowchartNodeShape.Ellipse) target.Shape = TopologyNodeShape.BusinessEllipse;
            return;
        }
        var type = source.Properties.TryGetValue("type", out var authoredType) ? authoredType : "normal";
        if (source.Properties.TryGetValue("icon", out var iconId) && iconId.Length > 0) {
            target.Shape = TopologyNodeShape.ActorIcon;
            target.IconId = iconId;
            // Resolve once into ordinary retained native facts. The cloned chart catalog supplies detached artwork.
            var icon = catalog.Resolve(iconId);
            if (icon != null) {
                target.IconShape = icon.Shape;
                target.Kind = icon.NodeKind;
                target.Symbol = icon.Symbol;
                target.Artwork = icon.Artwork;
                if (string.IsNullOrWhiteSpace(target.Color)) target.Color = icon.Color;
            }
        } else target.Shape = type == "awesome" ? TopologyNodeShape.ActorSilhouette
            : type == "hollow" ? business ? TopologyNodeShape.ActorBusinessHollow : TopologyNodeShape.ActorHollow
            : business ? TopologyNodeShape.ActorBusiness : TopologyNodeShape.Actor;
    }

    internal static IEnumerable<MermaidDiagnostic> UseCaseIconDiagnostics(MermaidUseCaseDocument document, MermaidFlowchartRenderOptions options) {
        var catalog = options.IconCatalog ?? TopologyIconCatalog.Default();
        foreach (var node in document.Nodes) {
            if (node.Shape != MermaidFlowchartNodeShape.Actor || !node.Properties.TryGetValue("icon", out var iconId)
                || iconId.Length == 0 || catalog.Resolve(iconId) != null) continue;
            yield return new MermaidDiagnostic { Code = MermaidDiagnosticCodes.UnsupportedStatement, Severity = MermaidDiagnosticSeverity.Warning,
                Span = node.PropertySpans.TryGetValue("icon", out var span) ? span : node.Span,
                Message = "UseCase icon '" + iconId + "' does not resolve in the finite native icon catalog; its frame, caption and source facts are retained." };
        }
    }
}
