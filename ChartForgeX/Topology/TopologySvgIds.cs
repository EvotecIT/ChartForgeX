using System;
using System.Text.RegularExpressions;

namespace ChartForgeX.Topology;

/// <summary>
/// Builds the SVG ids of a rendered topology: the root id (title, description, filters, markers, and the CSS scope)
/// and element ids for groups, nodes, edges, callouts, and motion paths. <see cref="TopologyRenderOptions.IdScope"/>
/// goes in front of all of them, so several renders of one chart can share a document.
/// </summary>
internal static class TopologySvgIds {
    // An id attribute (not data-*-id), an href or xlink:href fragment link, or a url(#…) reference to a cfxi- id.
    private static readonly Regex IconArtworkIdReference = new(
        @"(?<lead>(?<![\w:-])id\s*=\s*[""']|(?<![\w-])(?:xlink:)?href\s*=\s*[""']#|url\(\s*[""']?#)cfxi-",
        RegexOptions.CultureInvariant);

    /// <summary>Returns the root id: the scope and the chart id, or <c>topology</c> when the chart has none.</summary>
    public static string Root(TopologyChart chart, TopologyRenderOptions options) =>
        TopologyRenderPrimitives.SanitizeId(Scope(options) + (string.IsNullOrWhiteSpace(chart.Id) ? "topology" : chart.Id!));

    /// <summary>Returns the id of one rendered element of the given kind.</summary>
    public static string Element(TopologyChart chart, TopologyRenderOptions options, string kind, string id) =>
        TopologyRenderPrimitives.SafeElementId(Scope(options) + (chart.Id ?? "topology"), kind, id);

    /// <summary>
    /// Puts the scope in front of the ids of one piece of imported icon artwork (the <c>cfxi-</c> ids the SVG pack
    /// importer writes) and of the references to them inside it, so icon gradients and clip paths stay unique across
    /// scoped renders. Only <c>id</c> attributes, <c>href</c>/<c>xlink:href</c> fragment links, and <c>url(#…)</c>
    /// references change; ids in hand-written artwork without the <c>cfxi-</c> prefix are left as they are.
    /// </summary>
    public static string ScopeIconArtwork(string body, TopologyRenderOptions options) {
        if (string.IsNullOrWhiteSpace(options.IdScope) || body.IndexOf("cfxi-", StringComparison.Ordinal) < 0) return body;
        var scope = TopologyRenderPrimitives.SanitizeId(options.IdScope!.Trim()) + "-";
        return IconArtworkIdReference.Replace(body, match => match.Groups["lead"].Value + scope + "cfxi-");
    }

    private static string Scope(TopologyRenderOptions options) =>
        string.IsNullOrWhiteSpace(options.IdScope) ? string.Empty : options.IdScope!.Trim() + "-";
}
