using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Terminal;

namespace ChartForgeX.Topology;

/// <summary>
/// Options for projecting a prepared topology into terminal cells.
/// </summary>
public sealed class TopologyCellLayoutOptions {
    private int _maximumColumns = 120;
    private int _maximumLabelWidth = 24;
    private int _reservedValueWidth;

    /// <summary>Gets or sets the preferred width in cells. Labels that need more room can make the layout wider.</summary>
    public int MaximumColumns {
        get => _maximumColumns;
        set => _maximumColumns = value >= 20 && value <= 1000 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Cell layouts support 20 to 1000 columns.");
    }

    /// <summary>Gets or sets the widest node label in cells before it is shortened with an ellipsis.</summary>
    public int MaximumLabelWidth {
        get => _maximumLabelWidth;
        set => _maximumLabelWidth = value >= 4 && value <= 120 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Node labels support 4 to 120 cells.");
    }

    /// <summary>Gets or sets whether nodes keep a second line for their subtitle.</summary>
    public bool IncludeSubtitles { get; set; } = true;

    /// <summary>Gets or sets whether edge labels get room on their routes.</summary>
    public bool IncludeEdgeLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets whether a layout wider than <see cref="MaximumColumns"/> is retried more compactly: first without
    /// subtitles, then with labels shortened to 12 cells, then without edge labels. The narrowest attempt is kept.
    /// </summary>
    public bool CompactToFit { get; set; }

    /// <summary>Gets or sets the room in cells kept on every node's second line for live values such as <c>98 %</c>.</summary>
    public int ReservedValueWidth {
        get => _reservedValueWidth;
        set => _reservedValueWidth = value >= 0 && value <= 64 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Reserved value widths support 0 to 64 cells.");
    }
}

/// <summary>A cell position in a terminal projection.</summary>
public readonly struct TopologyCellPoint : IEquatable<TopologyCellPoint> {
    /// <summary>Creates a cell position.</summary>
    public TopologyCellPoint(int column, int row) { Column = column; Row = row; }
    /// <summary>Gets the zero-based column.</summary>
    public int Column { get; }
    /// <summary>Gets the zero-based row.</summary>
    public int Row { get; }
    /// <inheritdoc />
    public bool Equals(TopologyCellPoint other) => Column == other.Column && Row == other.Row;
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TopologyCellPoint other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => unchecked(Column * 397 ^ Row);
    /// <inheritdoc />
    public override string ToString() => Column + "," + Row;
}

/// <summary>A rectangular topology element placed in terminal cells.</summary>
public abstract class TopologyCellBox {
    private protected TopologyCellBox(string id, string label, TopologyHealthStatus status, int column, int row, int width, int height) {
        Id = id; Label = label; Status = status; Column = column; Row = row; Width = width; Height = height;
    }
    /// <summary>Gets the element id.</summary>
    public string Id { get; }
    /// <summary>Gets the display label, already fitted to the box.</summary>
    public string Label { get; }
    /// <summary>Gets the health status.</summary>
    public TopologyHealthStatus Status { get; }
    /// <summary>Gets the left column.</summary>
    public int Column { get; }
    /// <summary>Gets the top row.</summary>
    public int Row { get; }
    /// <summary>Gets the width in cells, borders included.</summary>
    public int Width { get; }
    /// <summary>Gets the height in cells, borders included.</summary>
    public int Height { get; }
    /// <summary>Returns whether a cell lies inside the box, borders included.</summary>
    public bool Contains(int column, int row) => column >= Column && column < Column + Width && row >= Row && row < Row + Height;
}

/// <summary>A node placed in terminal cells.</summary>
public sealed class TopologyCellNode : TopologyCellBox {
    internal TopologyCellNode(TopologyNode node, string label, string? subtitle, int column, int row, int width, int height)
        : base(node.Id, label, node.Status, column, row, width, height) {
        Subtitle = subtitle; Kind = node.Kind; GroupId = node.GroupId;
    }
    /// <summary>Gets the fitted subtitle, or <see langword="null"/>.</summary>
    public string? Subtitle { get; }
    /// <summary>Gets the node kind.</summary>
    public TopologyNodeKind Kind { get; }
    /// <summary>Gets the containing group id, or <see langword="null"/>.</summary>
    public string? GroupId { get; }
}

/// <summary>A group boundary placed in terminal cells.</summary>
public sealed class TopologyCellGroup : TopologyCellBox {
    internal TopologyCellGroup(TopologyGroup group, int column, int row, int width, int height, int depth)
        : base(group.Id, group.Label, group.Status, column, row, width, height) {
        ParentGroupId = group.ParentGroupId; Depth = depth;
    }
    /// <summary>Gets the parent group id, or <see langword="null"/>.</summary>
    public string? ParentGroupId { get; }
    /// <summary>Gets the nesting depth; top-level groups are zero.</summary>
    public int Depth { get; }
}

/// <summary>An edge route placed in terminal cells. Every segment is horizontal or vertical.</summary>
public sealed class TopologyCellEdge {
    internal TopologyCellEdge(TopologyEdge edge, bool dashed, IReadOnlyList<TopologyCellPoint> points, TopologyCellPoint? labelAt, bool labelOnLine) {
        Id = edge.Id; SourceNodeId = edge.SourceNodeId; TargetNodeId = edge.TargetNodeId; Label = edge.Label;
        Status = edge.Status; Direction = edge.Direction; Dashed = dashed; Points = points; LabelPosition = labelAt; LabelOnLine = labelOnLine;
    }
    /// <summary>Gets the edge id.</summary>
    public string Id { get; }
    /// <summary>Gets the source node id.</summary>
    public string SourceNodeId { get; }
    /// <summary>Gets the target node id.</summary>
    public string TargetNodeId { get; }
    /// <summary>Gets the edge label, or <see langword="null"/>.</summary>
    public string? Label { get; }
    /// <summary>Gets the edge health status.</summary>
    public TopologyHealthStatus Status { get; }
    /// <summary>Gets the arrow direction.</summary>
    public VisualLinkDirection Direction { get; }
    /// <summary>Gets whether the edge is drawn dashed.</summary>
    public bool Dashed { get; }
    /// <summary>Gets the route vertices, from the source node border to the target node border.</summary>
    public IReadOnlyList<TopologyCellPoint> Points { get; }
    /// <summary>Gets where the label starts, or <see langword="null"/> when the route has no room for it.</summary>
    public TopologyCellPoint? LabelPosition { get; }
    /// <summary>Gets whether the label is written on a horizontal run of the line rather than beside a vertical run.</summary>
    public bool LabelOnLine { get; }
}

/// <summary>
/// Projects a prepared topology into terminal cells. The topology layout engine places nodes and routes edges once;
/// this projection only maps that geometry to a character grid, keeping routes orthogonal and nodes wide enough for
/// their labels. Paint it with <see cref="Paint"/> for text or for a terminal widget.
/// </summary>
public sealed partial class TopologyCellLayout {
    private TopologyCellLayout(string? title, int columns, int rows, List<TopologyCellNode> nodes, List<TopologyCellGroup> groups, List<TopologyCellEdge> edges) {
        Title = title; Columns = columns; Rows = rows; Nodes = nodes; Groups = groups; Edges = edges;
    }

    /// <summary>Gets the topology title, or <see langword="null"/>.</summary>
    public string? Title { get; }
    /// <summary>Gets the width in cells.</summary>
    public int Columns { get; }
    /// <summary>Gets the height in cells, without the legend.</summary>
    public int Rows { get; }
    /// <summary>Gets the placed nodes.</summary>
    public IReadOnlyList<TopologyCellNode> Nodes { get; }
    /// <summary>Gets the placed groups, parents before children.</summary>
    public IReadOnlyList<TopologyCellGroup> Groups { get; }
    /// <summary>Gets the placed edges.</summary>
    public IReadOnlyList<TopologyCellEdge> Edges { get; }

    /// <summary>Finds a placed node by id.</summary>
    public TopologyCellNode? FindNode(string id) => Nodes.FirstOrDefault(node => string.Equals(node.Id, id, StringComparison.Ordinal));

    /// <summary>Returns the node under a cell, or <see langword="null"/>.</summary>
    public TopologyCellNode? NodeAt(int column, int row) => Nodes.FirstOrDefault(node => node.Contains(column, row));

    /// <summary>Projects a prepared topology into terminal cells.</summary>
    /// <param name="prepared">The prepared topology. Its layout is reused as is.</param>
    /// <param name="options">Optional projection options.</param>
    /// <returns>The cell layout.</returns>
    public static TopologyCellLayout Create(PreparedTopology prepared, TopologyCellLayoutOptions? options = null) {
        if (prepared == null) throw new ArgumentNullException(nameof(prepared));
        options ??= new TopologyCellLayoutOptions();
        var layout = CreateCore(prepared, options);
        if (!options.CompactToFit || layout.Columns <= options.MaximumColumns) return layout;
        var attempts = new[] {
            Compact(options, subtitles: false, label: options.MaximumLabelWidth, edgeLabels: options.IncludeEdgeLabels),
            Compact(options, subtitles: false, label: Math.Min(12, options.MaximumLabelWidth), edgeLabels: options.IncludeEdgeLabels),
            Compact(options, subtitles: false, label: Math.Min(12, options.MaximumLabelWidth), edgeLabels: false)
        };
        foreach (var attempt in attempts) {
            var candidate = CreateCore(prepared, attempt);
            if (candidate.Columns < layout.Columns) layout = candidate;
            if (layout.Columns <= options.MaximumColumns) break;
        }

        return layout;
    }

    private static TopologyCellLayoutOptions Compact(TopologyCellLayoutOptions options, bool subtitles, int label, bool edgeLabels) => new() {
        MaximumColumns = options.MaximumColumns,
        MaximumLabelWidth = label,
        IncludeSubtitles = subtitles && options.IncludeSubtitles,
        IncludeEdgeLabels = edgeLabels,
        ReservedValueWidth = options.ReservedValueWidth
    };

    private static TopologyCellLayout CreateCore(PreparedTopology prepared, TopologyCellLayoutOptions options) {
        var chart = prepared.LayoutChart;
        var report = prepared.Analyze();
        var nodeModels = chart.Nodes.GroupBy(node => node.Id, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var x = new CellAxis();
        var y = new CellAxis();
        var texts = new Dictionary<string, (string Label, string? Subtitle)>(StringComparer.Ordinal);
        var nodeBounds = new List<(TopologyNode Node, ChartRect Bounds)>();
        foreach (var diagnostic in report.Nodes) {
            if (!nodeModels.TryGetValue(diagnostic.Id, out var node)) continue;
            var label = TerminalTextWidth.Fit(string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label.Trim(), options.MaximumLabelWidth);
            var subtitle = options.IncludeSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle) ? TerminalTextWidth.Fit(node.Subtitle!.Trim(), options.MaximumLabelWidth) : null;
            texts[node.Id] = (label, subtitle);
            var b = diagnostic.Bounds;
            nodeBounds.Add((node, b));
            var width = Math.Max(TerminalTextWidth.Measure(label) + 5, Math.Max(subtitle == null ? 0 : TerminalTextWidth.Measure(subtitle) + 4, options.ReservedValueWidth + 4));
            x.Span(b.Left, b.Right, Math.Max(7, width));
            y.Span(b.Top, b.Bottom, subtitle != null || options.ReservedValueWidth > 0 ? 4 : 3);
        }

        SeparateNeighbours(nodeBounds, x, y);
        var groupBounds = report.Groups.ToDictionary(group => group.Id, group => group.Bounds, StringComparer.Ordinal);
        foreach (var group in chart.Groups) {
            if (!groupBounds.TryGetValue(group.Id, out var b)) continue;
            x.Span(b.Left, b.Right, TerminalTextWidth.Measure(group.Label ?? group.Id) + 6);
            y.Span(b.Top, b.Bottom, 4);
            foreach (var (node, nb) in nodeBounds.Where(item => string.Equals(item.Node.GroupId, group.Id, StringComparison.Ordinal))) Pad(x, y, b, nb);
            foreach (var child in chart.Groups.Where(item => string.Equals(item.ParentGroupId, group.Id, StringComparison.Ordinal))) {
                if (groupBounds.TryGetValue(child.Id, out var cb)) Pad(x, y, b, cb);
            }
        }

        var edgeModels = chart.Edges.GroupBy(edge => edge.Id, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var labelSegments = new Dictionary<string, int>(StringComparer.Ordinal);
        var labelRanges = new Dictionary<string, (double From, double To)>(StringComparer.Ordinal);
        var boundsById = nodeBounds.ToDictionary(item => item.Node.Id, item => item.Bounds, StringComparer.Ordinal);
        var routes = new Dictionary<string, List<ChartPoint>>(StringComparer.Ordinal);
        foreach (var diagnostic in report.Edges) {
            var points = Simplify(Elbows(AttachEnds(Simplify(diagnostic.Points), boundsById, diagnostic.SourceNodeId, diagnostic.TargetNodeId)));
            routes[diagnostic.Id] = points;
            foreach (var point in points) { x.Add(point.X); y.Add(point.Y); }
            if (!options.IncludeEdgeLabels || !edgeModels.TryGetValue(diagnostic.Id, out var edge) || string.IsNullOrWhiteSpace(edge.Label)) continue;
            var segment = LongestHorizontalRun(points);
            if (segment < 0) continue;
            labelSegments[diagnostic.Id] = segment;
            var range = LabelGap(points[segment], points[segment + 1], groupBounds.Values);
            labelRanges[diagnostic.Id] = range;
            x.Span(range.From, range.To, TerminalTextWidth.Measure(edge.Label!.Trim()) + 6);
        }

        SeparateFromGroups(chart, nodeBounds, groupBounds, routes.Values, x, y);
        var spanX = Math.Max(1, x.Extent);
        var scaleX = (options.MaximumColumns - 1) / spanX;
        // Rows keep the preferred resolution even when long labels force the columns to compress.
        var scaleY = scaleX / 2.0;
        CellMap mapX = x.Solve(scaleX);
        for (var attempt = 0; attempt < 4 && mapX.Size > options.MaximumColumns; attempt++) {
            scaleX *= options.MaximumColumns / (double)mapX.Size * 0.95;
            mapX = x.Solve(scaleX);
        }

        var mapY = y.Solve(scaleY);
        var nodes = new List<TopologyCellNode>();
        foreach (var (node, b) in nodeBounds) {
            var (label, subtitle) = texts[node.Id];
            var left = mapX.Map(b.Left);
            var top = mapY.Map(b.Top);
            nodes.Add(new TopologyCellNode(node, label, subtitle, left, top, mapX.Map(b.Right) - left + 1, mapY.Map(b.Bottom) - top + 1));
        }

        var groups = new List<TopologyCellGroup>();
        foreach (var group in chart.Groups.OrderBy(group => Depth(chart, group))) {
            if (!groupBounds.TryGetValue(group.Id, out var b)) continue;
            var left = mapX.Map(b.Left);
            var top = mapY.Map(b.Top);
            groups.Add(new TopologyCellGroup(group, left, top, mapX.Map(b.Right) - left + 1, mapY.Map(b.Bottom) - top + 1, Depth(chart, group)));
        }

        var edges = new List<TopologyCellEdge>();
        foreach (var diagnostic in report.Edges) {
            if (!edgeModels.TryGetValue(diagnostic.Id, out var edge) || routes[diagnostic.Id].Count < 2) continue;
            var mapped = routes[diagnostic.Id].Select(point => new TopologyCellPoint(mapX.Map(point.X), mapY.Map(point.Y))).ToList();
            KeepEndsInside(mapped, nodes, edge.SourceNodeId, edge.TargetNodeId);
            var route = Orthogonalize(mapped);
            if (route.Count < 2) continue;
            TopologyCellPoint? labelAt = null;
            var onLine = false;
            if (options.IncludeEdgeLabels && !string.IsNullOrWhiteSpace(edge.Label)) {
                if (labelSegments.TryGetValue(diagnostic.Id, out var segment)) {
                    var a = mapped[segment];
                    var range = labelRanges[diagnostic.Id];
                    var width = TerminalTextWidth.Measure(edge.Label!.Trim()) + 2;
                    labelAt = new TopologyCellPoint((mapX.Map(range.From) + mapX.Map(range.To)) / 2 - width / 2, a.Row);
                    onLine = true;
                } else {
                    labelAt = VerticalLabelPosition(route);
                }
            }

            edges.Add(new TopologyCellEdge(edge, IsDashed(edge), route, labelAt, onLine));
        }

        return new TopologyCellLayout(prepared.Title, mapX.Size, mapY.Size, nodes, groups, edges);
    }

    private static void Pad(CellAxis x, CellAxis y, ChartRect outer, ChartRect inner) {
        x.Span(outer.Left, inner.Left, 3);
        x.Span(inner.Right, outer.Right, 3);
        y.Span(outer.Top, inner.Top, 2);
        y.Span(inner.Bottom, outer.Bottom, 2);
    }

    private static void SeparateNeighbours(List<(TopologyNode Node, ChartRect Bounds)> nodes, CellAxis x, CellAxis y) {
        for (var i = 0; i < nodes.Count; i++) {
            for (var j = 0; j < nodes.Count; j++) {
                if (i == j) continue;
                var a = nodes[i].Bounds;
                var b = nodes[j].Bounds;
                var overlapY = a.Top < b.Bottom && b.Top < a.Bottom;
                var overlapX = a.Left < b.Right && b.Left < a.Right;
                if (overlapY && a.Right <= b.Left) x.Span(a.Right, b.Left, 6);
                if (overlapX && a.Bottom <= b.Top) y.Span(a.Bottom, b.Top, 4);
            }
        }
    }

    /// <summary>Keeps outside nodes and vertical edge runs off group borders, so a border never hides a box side or an edge.</summary>
    private static void SeparateFromGroups(TopologyChart chart, List<(TopologyNode Node, ChartRect Bounds)> nodes, Dictionary<string, ChartRect> groups,
        IEnumerable<List<ChartPoint>> routes, CellAxis x, CellAxis y) {
        var verticalRuns = new List<double>();
        foreach (var route in routes) {
            for (var i = 0; i + 1 < route.Count; i++) {
                if (Math.Abs(route[i].X - route[i + 1].X) < 0.25) verticalRuns.Add(route[i].X);
            }
        }

        foreach (var group in chart.Groups) {
            if (!groups.TryGetValue(group.Id, out var g)) continue;
            foreach (var (node, b) in nodes) {
                if (InGroup(chart, node.GroupId, group.Id)) continue;
                var overlapY = b.Top < g.Bottom && g.Top < b.Bottom;
                var overlapX = b.Left < g.Right && g.Left < b.Right;
                if (overlapY && b.Right <= g.Left) x.Span(b.Right, g.Left, 3);
                if (overlapY && b.Left >= g.Right) x.Span(g.Right, b.Left, 3);
                if (overlapX && b.Bottom <= g.Top) y.Span(b.Bottom, g.Top, 2);
                if (overlapX && b.Top >= g.Bottom) y.Span(g.Bottom, b.Top, 2);
            }

            foreach (var run in verticalRuns) {
                foreach (var border in new[] { g.Left, g.Right }) {
                    if (Math.Abs(run - border) > 0.25) x.Span(Math.Min(run, border), Math.Max(run, border), 2);
                }
            }
        }
    }

    private static bool InGroup(TopologyChart chart, string? groupId, string ancestorId) {
        for (var depth = 0; !string.IsNullOrWhiteSpace(groupId) && depth < 32; depth++) {
            if (string.Equals(groupId, ancestorId, StringComparison.Ordinal)) return true;
            groupId = chart.Groups.FirstOrDefault(candidate => string.Equals(candidate.Id, groupId, StringComparison.Ordinal))?.ParentGroupId;
        }

        return false;
    }

    private static int Depth(TopologyChart chart, TopologyGroup group) {
        var depth = 0;
        var parent = group.ParentGroupId;
        while (!string.IsNullOrWhiteSpace(parent) && depth < 32) {
            depth++;
            parent = chart.Groups.FirstOrDefault(candidate => string.Equals(candidate.Id, parent, StringComparison.Ordinal))?.ParentGroupId;
        }

        return depth;
    }

    private static bool IsDashed(TopologyEdge edge) =>
        edge.DashPattern.Count > 0
        || edge.LineStyle == TopologyEdgeLineStyle.Dashed
        || edge.LineStyle == TopologyEdgeLineStyle.Dotted
        || edge.LineStyle == TopologyEdgeLineStyle.Auto && TopologyRenderPrimitives.EffectiveEdgeDash(edge) != "none";

    /// <summary>Drops repeated points and merges collinear runs, so each remaining segment is one straight run.</summary>
    private static List<ChartPoint> Simplify(IReadOnlyList<ChartPoint> points) {
        var result = new List<ChartPoint>();
        foreach (var point in points) {
            if (result.Count > 0 && Math.Abs(result[result.Count - 1].X - point.X) < 0.25 && Math.Abs(result[result.Count - 1].Y - point.Y) < 0.25) continue;
            if (result.Count >= 2) {
                var a = result[result.Count - 2];
                var b = result[result.Count - 1];
                var horizontal = Math.Abs(a.Y - b.Y) < 0.25 && Math.Abs(b.Y - point.Y) < 0.25;
                var vertical = Math.Abs(a.X - b.X) < 0.25 && Math.Abs(b.X - point.X) < 0.25;
                if (horizontal || vertical) result.RemoveAt(result.Count - 1);
            }

            result.Add(point);
        }

        return result;
    }

    /// <summary>Replaces diagonal runs with a horizontal-vertical-horizontal elbow (or vertical-horizontal-vertical), so cells can draw them.</summary>
    private static List<ChartPoint> Elbows(List<ChartPoint> points) {
        var result = new List<ChartPoint>();
        for (var i = 0; i < points.Count; i++) {
            if (i > 0) {
                var a = points[i - 1];
                var b = points[i];
                if (Math.Abs(a.X - b.X) > 0.25 && Math.Abs(a.Y - b.Y) > 0.25) {
                    if (Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y) * 2) {
                        var middle = (a.X + b.X) / 2;
                        result.Add(new ChartPoint(middle, a.Y));
                        result.Add(new ChartPoint(middle, b.Y));
                    } else {
                        var middle = (a.Y + b.Y) / 2;
                        result.Add(new ChartPoint(a.X, middle));
                        result.Add(new ChartPoint(b.X, middle));
                    }
                }
            }

            result.Add(points[i]);
        }

        return result;
    }

    /// <summary>Moves route ends that stop short of their node (arrowhead gaps) onto the node border they approach.</summary>
    private static List<ChartPoint> AttachEnds(List<ChartPoint> points, Dictionary<string, ChartRect> bounds, string sourceId, string targetId) {
        if (points.Count < 2) return points;
        if (bounds.TryGetValue(sourceId, out var source)) points[0] = Attach(points[0], points[1], source);
        if (bounds.TryGetValue(targetId, out var target)) points[points.Count - 1] = Attach(points[points.Count - 1], points[points.Count - 2], target);
        return points;
    }

    private static ChartPoint Attach(ChartPoint end, ChartPoint neighbour, ChartRect node) {
        const double reach = 24;
        var horizontal = Math.Abs(end.Y - neighbour.Y) < 0.5;
        var vertical = Math.Abs(end.X - neighbour.X) < 0.5;
        if (horizontal && end.Y >= node.Top && end.Y <= node.Bottom) {
            if (end.X <= node.Left && node.Left - end.X <= reach) return new ChartPoint(node.Left, end.Y);
            if (end.X >= node.Right && end.X - node.Right <= reach) return new ChartPoint(node.Right, end.Y);
        }

        if (vertical && end.X >= node.Left && end.X <= node.Right) {
            if (end.Y <= node.Top && node.Top - end.Y <= reach) return new ChartPoint(end.X, node.Top);
            if (end.Y >= node.Bottom && end.Y - node.Bottom <= reach) return new ChartPoint(end.X, node.Bottom);
        }

        return end;
    }

    /// <summary>Moves route ends that meet a node on its corner or top/bottom border row onto the node's interior, so the arrow meets a side.</summary>
    private static void KeepEndsInside(List<TopologyCellPoint> points, List<TopologyCellNode> nodes, string sourceId, string targetId) {
        if (points.Count < 2) return;
        Clamp(points, 0, 1, nodes.FirstOrDefault(node => node.Id == sourceId));
        Clamp(points, points.Count - 1, points.Count - 2, nodes.FirstOrDefault(node => node.Id == targetId));
    }

    private static void Clamp(List<TopologyCellPoint> points, int end, int neighbour, TopologyCellNode? node) {
        if (node == null || node.Width < 3 || node.Height < 3) return;
        var point = points[end];
        var next = points[neighbour];
        var onSide = point.Column == node.Column || point.Column == node.Column + node.Width - 1;
        var onCap = point.Row == node.Row || point.Row == node.Row + node.Height - 1;
        if (onSide && point.Row == next.Row) {
            var row = Math.Max(node.Row + 1, Math.Min(node.Row + node.Height - 2, point.Row));
            if (row == point.Row) return;
            points[end] = new TopologyCellPoint(point.Column, row);
            points[neighbour] = new TopologyCellPoint(next.Column, row);
        } else if (onCap && point.Column == next.Column) {
            var column = Math.Max(node.Column + 1, Math.Min(node.Column + node.Width - 2, point.Column));
            if (column == point.Column) return;
            points[end] = new TopologyCellPoint(column, point.Row);
            points[neighbour] = new TopologyCellPoint(column, next.Row);
        }
    }

    /// <summary>The widest part of a horizontal run that crosses no group border, so a label never covers a boundary.</summary>
    private static (double From, double To) LabelGap(ChartPoint a, ChartPoint b, IEnumerable<ChartRect> groups) {
        var from = Math.Min(a.X, b.X);
        var to = Math.Max(a.X, b.X);
        var cuts = new List<double> { from, to };
        foreach (var group in groups) {
            if (a.Y < group.Top || a.Y > group.Bottom) continue;
            foreach (var border in new[] { group.Left, group.Right }) {
                if (border > from + .25 && border < to - .25) cuts.Add(border);
            }
        }

        cuts.Sort();
        var best = (From: from, To: to);
        var widest = -1.0;
        for (var i = 0; i + 1 < cuts.Count; i++) {
            if (cuts[i + 1] - cuts[i] <= widest) continue;
            widest = cuts[i + 1] - cuts[i];
            best = (cuts[i], cuts[i + 1]);
        }

        return best;
    }

    private static int LongestHorizontalRun(IReadOnlyList<ChartPoint> points) {
        var best = -1;
        var length = 0.0;
        for (var i = 0; i + 1 < points.Count; i++) {
            if (Math.Abs(points[i].Y - points[i + 1].Y) > 0.5) continue;
            var run = Math.Abs(points[i + 1].X - points[i].X);
            if (run <= length) continue;
            length = run;
            best = i;
        }

        return best;
    }

    private static TopologyCellPoint? VerticalLabelPosition(IReadOnlyList<TopologyCellPoint> route) {
        var best = -1;
        var length = 0;
        for (var i = 0; i + 1 < route.Count; i++) {
            if (route[i].Column != route[i + 1].Column) continue;
            var run = Math.Abs(route[i + 1].Row - route[i].Row);
            if (run <= length) continue;
            length = run;
            best = i;
        }

        if (best < 0 || length < 2) return null;
        return new TopologyCellPoint(route[best].Column + 2, (route[best].Row + route[best + 1].Row) / 2);
    }

    private static List<TopologyCellPoint> Orthogonalize(List<TopologyCellPoint> points) {
        var route = new List<TopologyCellPoint>();
        foreach (var point in points) {
            if (route.Count > 0 && route[route.Count - 1].Equals(point)) continue;
            if (route.Count > 0) {
                var last = route[route.Count - 1];
                if (last.Column != point.Column && last.Row != point.Row) {
                    if (Math.Abs(point.Column - last.Column) >= Math.Abs(point.Row - last.Row)) {
                        var middle = (last.Column + point.Column) / 2;
                        route.Add(new TopologyCellPoint(middle, last.Row));
                        route.Add(new TopologyCellPoint(middle, point.Row));
                    } else {
                        var middle = (last.Row + point.Row) / 2;
                        route.Add(new TopologyCellPoint(last.Column, middle));
                        route.Add(new TopologyCellPoint(point.Column, middle));
                    }
                }
            }

            route.Add(point);
        }

        for (var i = route.Count - 2; i > 0; i--) {
            var before = route[i - 1];
            var after = route[i + 1];
            if (route[i].Equals(before) || before.Column == route[i].Column && route[i].Column == after.Column || before.Row == route[i].Row && route[i].Row == after.Row) route.RemoveAt(i);
        }

        return route;
    }

    /// <summary>One axis of the projection: sorted coordinates with minimum cell spans between them.</summary>
    private sealed class CellAxis {
        private readonly List<double> _coordinates = new();
        private readonly List<(double From, double To, int Cells)> _spans = new();

        public double Extent => _coordinates.Count == 0 ? 0 : _coordinates.Max() - _coordinates.Min();

        public void Add(double value) {
            if (!double.IsNaN(value) && !double.IsInfinity(value)) _coordinates.Add(value);
        }

        /// <summary>Requires at least <paramref name="cells"/> cells from <paramref name="from"/> to <paramref name="to"/>, both ends included.</summary>
        public void Span(double from, double to, int cells) {
            Add(from); Add(to);
            if (to > from) _spans.Add((from, to, cells));
        }

        public CellMap Solve(double scale) {
            var keys = new List<double>();
            foreach (var value in _coordinates.OrderBy(value => value)) {
                if (keys.Count == 0 || value - keys[keys.Count - 1] > 0.25) keys.Add(value);
            }

            if (keys.Count == 0) keys.Add(0);
            var positions = new int[keys.Count];
            var spansByEnd = _spans.Select(span => (From: Index(keys, span.From), To: Index(keys, span.To), span.Cells))
                .Where(span => span.To > span.From).GroupBy(span => span.To).ToDictionary(group => group.Key, group => group.ToArray());
            for (var i = 1; i < keys.Count; i++) {
                var position = positions[i - 1] + (int)Math.Round((keys[i] - keys[i - 1]) * scale, MidpointRounding.AwayFromZero);
                if (spansByEnd.TryGetValue(i, out var spans)) {
                    foreach (var span in spans) position = Math.Max(position, positions[span.From] + span.Cells - 1);
                }

                positions[i] = position;
            }

            return new CellMap(keys, positions);
        }

        private static int Index(List<double> keys, double value) {
            var best = 0;
            for (var i = 1; i < keys.Count; i++) {
                if (Math.Abs(keys[i] - value) < Math.Abs(keys[best] - value)) best = i;
            }

            return best;
        }
    }

    private sealed class CellMap {
        private readonly List<double> _keys;
        private readonly int[] _positions;

        public CellMap(List<double> keys, int[] positions) { _keys = keys; _positions = positions; }

        public int Size => _positions[_positions.Length - 1] + 1;

        public int Map(double value) {
            if (value <= _keys[0]) return _positions[0];
            for (var i = 1; i < _keys.Count; i++) {
                if (value > _keys[i] + 0.25) continue;
                if (Math.Abs(value - _keys[i]) <= 0.25) return _positions[i];
                var t = (value - _keys[i - 1]) / (_keys[i] - _keys[i - 1]);
                return _positions[i - 1] + (int)Math.Round(t * (_positions[i] - _positions[i - 1]), MidpointRounding.AwayFromZero);
            }

            return _positions[_positions.Length - 1];
        }
    }
}
