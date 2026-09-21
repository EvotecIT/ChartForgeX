using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

/// <summary>Configures bounded detail pages for a topology too dense to read as a single image.</summary>
public sealed class TopologyReportOptions {
    /// <summary>Gets or sets the target page width in pixels.</summary>
    public double PageWidth { get; set; } = 1200;
    /// <summary>Gets or sets the target page height in pixels.</summary>
    public double PageHeight { get; set; } = 800;
    /// <summary>Gets or sets the maximum nodes per detail page, in addition to geometric capacity.</summary>
    public int MaximumNodesPerPage { get; set; } = 12;
    /// <summary>Gets or sets the maximum internal relationships per page, except indivisible self relationships.</summary>
    public int MaximumEdgesPerPage { get; set; } = 24;
    /// <summary>Gets or sets the requested space between cards in pixels. The layout's minimum card separation also applies.</summary>
    public double Gap { get; set; } = 28;
    /// <summary>Gets or sets the minimum card width; larger source dimensions are retained.</summary>
    public double MinimumNodeWidth { get; set; } = 240;
    /// <summary>Gets or sets the minimum card height; larger source dimensions are retained.</summary>
    public double MinimumNodeHeight { get; set; } = 72;

    /// <summary>Gets or sets the custom icon catalog to snapshot for every report export.</summary>
    public TopologyIconCatalog? IconCatalog { get; set; }
    /// <summary>Gets or sets whether unresolved icon identifiers must fail report preparation.</summary>
    public bool RequireResolvedIcons { get; set; }

    internal void Validate() {
        foreach (var item in new[] { PageWidth, PageHeight, Gap, MinimumNodeWidth, MinimumNodeHeight }) {
            if (double.IsNaN(item) || double.IsInfinity(item) || item <= 0) throw new ArgumentOutOfRangeException(nameof(TopologyReportOptions), "Page dimensions, spacing, and node dimensions must be positive and finite.");
        }
        if (PageWidth < 240 || PageHeight < 240) throw new ArgumentOutOfRangeException(nameof(TopologyReportOptions), "Report pages must be at least 240 pixels in each dimension.");
        if (MaximumEdgesPerPage < 0) throw new ArgumentOutOfRangeException(nameof(MaximumEdgesPerPage));
        if (MaximumNodesPerPage < 1) throw new ArgumentOutOfRangeException(nameof(MaximumNodesPerPage));
    }
}

/// <summary>A relationship crossing two detail pages. No source edge is silently discarded by pagination.</summary>
public sealed class TopologyReportLink {
    internal TopologyReportLink(string edgeId, string sourceNodeId, string targetNodeId, int sourcePage, int targetPage, VisualLinkDirection direction) {
        EdgeId = edgeId; SourceNodeId = sourceNodeId; TargetNodeId = targetNodeId; SourcePage = sourcePage; TargetPage = targetPage; Direction = direction;
    }
    /// <summary>Gets the source relationship id.</summary>
    public string EdgeId { get; }
    /// <summary>Gets the relationship direction in source-to-target order.</summary>
    public VisualLinkDirection Direction { get; }
    /// <summary>Gets the source node id.</summary>
    public string SourceNodeId { get; }
    /// <summary>Gets the target node id.</summary>
    public string TargetNodeId { get; }
    /// <summary>Gets the one-based source detail page number.</summary>
    public int SourcePage { get; }
    /// <summary>Gets the one-based target detail page number.</summary>
    public int TargetPage { get; }
}

/// <summary>A topology overview, bounded detail pages, and a complete node and cross-page relationship index.</summary>
public sealed class TopologyReport {
    internal TopologyReport(PreparedTopology source, PreparedTopology overview, List<PreparedTopology> pages, Dictionary<string, int> nodePages, Dictionary<string, string> nodeLabels, List<string> orderedNodeIds, List<TopologyReportLink> links) {
        Source = source; Overview = overview; Pages = pages.AsReadOnly(); NodePages = new ReadOnlyDictionary<string, int>(nodePages); CrossPageLinks = links.AsReadOnly();
        NodeLabels = new ReadOnlyDictionary<string, string>(nodeLabels);
        OrderedNodeIds = orderedNodeIds.AsReadOnly();
    }
    /// <summary>Gets the original topology for exploration and source readability diagnostics.</summary>
    public PreparedTopology Source { get; }
    /// <summary>Gets one summary node per detail page and counted relationships between pages.</summary>
    public PreparedTopology Overview { get; }
    /// <summary>Gets detail pages. Each source node appears on exactly one detail page.</summary>
    public IReadOnlyList<PreparedTopology> Pages { get; }
    /// <summary>Gets the one-based detail page for each stable source node id.</summary>
    public IReadOnlyDictionary<string, int> NodePages { get; }
    /// <summary>Gets labels keyed by original source node ids, in the same namespace as NodePages and CrossPageLinks.</summary>
    public IReadOnlyDictionary<string, string> NodeLabels { get; }
    internal IReadOnlyList<string> OrderedNodeIds { get; }
    /// <summary>Gets relationships omitted from individual drawings because their endpoints are on different pages.</summary>
    public IReadOnlyList<TopologyReportLink> CrossPageLinks { get; }
}

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Creates readable detail pages using measured card capacity and stable group/input order.
    /// Internal relationships remain on their page; cross-page relationships are returned explicitly.
    /// This report reflows cards; use <see cref="Prepare"/> when original geometry must be retained.
    /// </summary>
    public static TopologyReport PrepareReport(this TopologyChart chart, TopologyReportOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyReportOptions();
        options.Validate();
        var gap = Math.Max(options.Gap, TopologyLayoutNormalizer.NodeGap);
        var renderOptions = new TopologyRenderOptions { IconCatalog = options.IconCatalog?.Clone(), RequireResolvedIcons = options.RequireResolvedIcons };
        var preparedSource = chart.Prepare(renderOptions);
        var pageOptions = renderOptions.Clone();
        pageOptions.IncludeLegend = false;
        var source = TopologyLayoutEngine.Clone(chart);
        source.Title = string.IsNullOrWhiteSpace(source.Title) ? "Topology report" : source.Title;
        var pages = new List<TopologyChart>();
        var nodePages = new Dictionary<string, int>(StringComparer.Ordinal);
        var orderedNodeIds = new List<string>();
        var incidentEdges = source.Edges.SelectMany(edge => edge.SourceNodeId == edge.TargetNodeId
            ? new[] { (NodeId: edge.SourceNodeId, Edge: edge) }
            : new[] { (NodeId: edge.SourceNodeId, Edge: edge), (NodeId: edge.TargetNodeId, Edge: edge) }).ToLookup(item => item.NodeId, item => item.Edge, StringComparer.Ordinal);
        var pageNodeIds = new HashSet<string>(StringComparer.Ordinal);
        int pageEdgeCount = 0;
        const double margin = 40;
        const double header = margin + TopologyRenderPrimitives.HeaderReservedHeight;
        TopologyChart? page = null;
        double x = margin, y = header, rowHeight = 0;
        var groupOrder = source.Groups.Select((group, index) => (group.Id, Rank: index + 1))
            .ToDictionary(group => group.Id, group => group.Rank, StringComparer.Ordinal);
        foreach (var node in source.Nodes.OrderBy(node => node.GroupId != null && groupOrder.TryGetValue(node.GroupId, out var rank) ? rank : 0)) {
            // Report cards always expose their labels; small dot/icon geometry is an overview concern.
            node.DisplayMode = TopologyNodeDisplayMode.Card;
            node.PreserveDisplayModeSize = true;
            node.Width = Math.Max(options.MinimumNodeWidth, node.Width);
            node.Height = Math.Max(options.MinimumNodeHeight, Math.Max(node.Height, TopologyRenderPrimitives.NodeDetailStartOffset(node, pageOptions) + node.Details.Count * 18 + 1));
            if (page != null && x + node.Width > options.PageWidth - margin && x > margin) {
                x = margin; y += rowHeight + gap; rowHeight = 0;
            }
            int addedEdges = incidentEdges[node.Id].Count(edge =>
                (edge.SourceNodeId == node.Id || pageNodeIds.Contains(edge.SourceNodeId)) &&
                (edge.TargetNodeId == node.Id || pageNodeIds.Contains(edge.TargetNodeId)));
            if (page == null || page.Nodes.Count >= options.MaximumNodesPerPage ||
                (page.Nodes.Count > 0 && pageEdgeCount + addedEdges > options.MaximumEdgesPerPage) || (y + node.Height > options.PageHeight - margin && page.Nodes.Count > 0)) {
                string pageSuffix = " — " + (pages.Count + 1).ToString(CultureInfo.InvariantCulture);
                page = TopologyChart.Create().WithId((chart.Id ?? "topology") + "-page-" + (pages.Count + 1).ToString(CultureInfo.InvariantCulture))
                    .WithTitle(VisualArtifactInterchangeMapping.BoundedGeneratedText(source.Title!, pageSuffix))
                    .WithViewport(options.PageWidth, options.PageHeight, margin);
                page.Theme = source.Theme;
                page.Accessibility.Name = source.Accessibility.Name;
                page.Accessibility.Description = source.Accessibility.Description;
                page.Accessibility.Language = source.Accessibility.Language;
                page.Accessibility.IsDecorative = source.Accessibility.IsDecorative;
                pages.Add(page); x = margin; y = header; rowHeight = 0;
                pageNodeIds.Clear(); pageEdgeCount = 0;
                addedEdges = incidentEdges[node.Id].Count(edge => edge.SourceNodeId == node.Id && edge.TargetNodeId == node.Id);
            }
            if (!string.IsNullOrWhiteSpace(node.GroupId)) {
                VisualArtifactInterchangeMapping.TrySetBoundedExtension(node.Metadata, "report.sourceGroupId", node.GroupId!);
            }
            node.GroupId = null;
            node.X = x; node.Y = y;
            page.Nodes.Add(node);
            pageNodeIds.Add(node.Id);
            pageEdgeCount += addedEdges;
            nodePages.Add(node.Id, pages.Count);
            orderedNodeIds.Add(node.Id);
            x += node.Width + gap;
            rowHeight = Math.Max(rowHeight, node.Height);
        }
        var links = new List<TopologyReportLink>();
        foreach (var edge in source.Edges) {
            int from = nodePages[edge.SourceNodeId], to = nodePages[edge.TargetNodeId];
            if (from != to) {
                links.Add(new TopologyReportLink(edge.Id, edge.SourceNodeId, edge.TargetNodeId, from, to, edge.Direction));
                continue;
            }
            edge.Waypoints.Clear();
            edge.Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
            edge.HasLabelAnchorOverride = false;
            edge.LabelAnchorNodeId = null;
            edge.LabelOffsetX = edge.LabelOffsetY = 0;
            pages[from - 1].Edges.Add(edge);
        }
        var preparedPages = pages.Select(item => item.Prepare(pageOptions)).ToList();
        var overview = BuildReportOverview(source, pages, links, options).Prepare(renderOptions);
        return new TopologyReport(preparedSource, overview, preparedPages, nodePages, source.Nodes.ToDictionary(node => node.Id, node => node.Label, StringComparer.Ordinal), orderedNodeIds, links);
    }

    private static TopologyChart BuildReportOverview(TopologyChart source, List<TopologyChart> pages,
        List<TopologyReportLink> links, TopologyReportOptions options) {
        var overview = TopologyChart.Create().WithId((source.Id ?? "topology") + "-overview")
            .WithTitle(source.Title ?? "Topology report")
            .WithSubtitle(source.Nodes.Count.ToString(CultureInfo.InvariantCulture) + " objects across " + pages.Count.ToString(CultureInfo.InvariantCulture) + " detail pages")
            .WithViewport(options.PageWidth, options.PageHeight)
            .WithLayout(TopologyLayoutMode.Matrix);
        overview.Theme = source.Theme;
        overview.Accessibility.Name = source.Accessibility.Name;
        overview.Accessibility.Description = source.Accessibility.Description;
        overview.Accessibility.Language = source.Accessibility.Language;
        overview.Accessibility.IsDecorative = source.Accessibility.IsDecorative;
        for (int i = 0; i < pages.Count; i++) {
            overview.AddAutoNode(ReportPageNodeId(i + 1, pages.Count), "Page " + (i + 1).ToString(CultureInfo.InvariantCulture),
                subtitle: pages[i].Nodes.Count.ToString(CultureInfo.InvariantCulture) + " objects", width: 160, height: 72);
        }
        foreach (var group in links.GroupBy(link => (SourcePage: Math.Min(link.SourcePage, link.TargetPage), TargetPage: Math.Max(link.SourcePage, link.TargetPage)))) {
            string from = ReportPageNodeId(group.Key.SourcePage, pages.Count);
            string to = ReportPageNodeId(group.Key.TargetPage, pages.Count);
            overview.AddEdge(from + "-" + to, from, to, group.Count().ToString(CultureInfo.InvariantCulture) + " relationships");
        }
        return overview;
    }

    private static string ReportPageNodeId(int number, int count) => "page-" + number.ToString("D" + count.ToString(CultureInfo.InvariantCulture).Length, CultureInfo.InvariantCulture);

}
