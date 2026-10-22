using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidStateParser {
    private sealed class StateScope {
        internal StateScope(string id, MermaidSourceSpan span, MermaidStateRegion region) {
            Id = id;
            Span = span;
            Region = region;
        }
        internal string Id { get; }
        internal MermaidSourceSpan Span { get; }
        internal MermaidStateRegion Region { get; set; }
    }

    private static MermaidStateRegion OpenRegion(MermaidStateDocument document, string compositeId, MermaidSourceSpan span) =>
        document.Regions.FindLast(region => region.CompositeStateId == compositeId)
        ?? CreateRegion(document, compositeId, 0, span);

    /// <summary>Retains empty leading, adjacent and trailing regions accepted by the state grammar.</summary>
    private static MermaidStateRegion CreateRegion(MermaidStateDocument document, string compositeId, int index, MermaidSourceSpan span) {
        MermaidResourceGuard.ReserveElements(1, span);
        var region = new MermaidStateRegion("mermaid-region-" + compositeId + "-" + index.ToString(CultureInfo.InvariantCulture), compositeId, index, span);
        document.Regions.Add(region);
        return region;
    }
}
