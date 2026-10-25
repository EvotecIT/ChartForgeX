using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    /// <summary>Converts source-ordered columns and factual task cards through the generic vertical-stack group policy.</summary>
    public static TopologyChart ToTopologyChart(this MermaidKanbanDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        ValidateKanban(document); options ??= new MermaidTopologyRenderOptions();
        var chart = CreateTopology(options, "mermaid-kanban", ResolveTitle(document, options, "Mermaid kanban"), TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight);
        var settings = document.Configuration.Kanban;
        if (settings?.Padding is double padding) chart.Viewport.Padding = padding;
        var taskWidth = settings?.SectionWidth is double sectionWidth ? Math.Max(1, sectionWidth - 48) : 180;
        var render = new TopologyRenderOptions { IncludeLegend = false, IncludeStatusBadges = false, IncludeGroupStatusDots = false,
            WrapNodeLabels = true, AllowMultilineNodeLabels = true, CardSubtitleMode = TopologyCardSubtitleMode.Text,
            CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Plain, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card };
        chart.DefaultRenderOptions = render;
        var columnIndex = 0;
        foreach (var column in document.Columns) {
            var label = KanbanCaption(column.Label ?? column.Title);
            var subtitle = column.Tasks.Count.ToString(CultureInfo.InvariantCulture) + " tasks";
            foreach (var field in KanbanFields(column.Metadata)) subtitle += "\n" + field;
            chart.AddAutoGroup(column.Id, label, TopologyHealthStatus.Unknown, subtitle: subtitle, cssClass: "cfx-mermaid-kanban-column");
            var group = chart.Groups[chart.Groups.Count - 1]; group.LayoutPolicy = TopologyGroupLayoutPolicy.VerticalStack;
            if (settings?.SectionWidth is double requestedWidth) {
                group.Width = requestedWidth;
                group.Metadata["mermaid.config.kanban.sectionWidth"] = requestedWidth.ToString("R", CultureInfo.InvariantCulture);
            }
            if (settings?.Padding is double requestedPadding)
                group.Metadata["mermaid.config.kanban.padding"] = requestedPadding.ToString("R", CultureInfo.InvariantCulture);
            group.Href = document.Configuration.Kanban?.Link(column.Ticket);
            KanbanFacts(group.Metadata, column.Id, column.Title, column.Metadata, column.MetadataSource, column.Span, columnIndex);
            var taskIndex = 0;
            foreach (var task in column.Tasks) {
                var caption = KanbanCaption(task.Label ?? task.Title);
                render.MaxNodeLabelLines = Math.Max(render.MaxNodeLabelLines, caption.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length);
                chart.AddAutoNode(task.Id, caption, TopologyNodeKind.Generic, TopologyHealthStatus.Unknown, groupId: column.Id,
                    width: taskWidth, height: 64, cssClass: "cfx-mermaid-kanban-task");
                var node = chart.Nodes[chart.Nodes.Count - 1]; node.Shape = TopologyNodeShape.Rounded;
                node.DisplayMode = TopologyNodeDisplayMode.Card; node.FitLabelToContent = true; node.PreserveDisplayModeSize = true;
                node.MaximumLabelCharacters = caption.Length; node.ShowStatusBadge = false; node.PaletteIndex = columnIndex;
                node.Href = document.Configuration.Kanban?.Link(task.Ticket);
                foreach (var field in KanbanFields(task.Metadata))
                    node.Details.Add(new TopologyNodeDetail { Text = field.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", " ⏎ ") });
                node.Metadata["mermaid.column"] = column.Id;
                KanbanFacts(node.Metadata, task.Id, task.Title, task.Metadata, task.MetadataSource, task.Span, taskIndex++);
            }
            columnIndex++;
        }
        return MermaidPresentation.Apply(chart, document, options.Presentation);
    }

    private static string KanbanCaption(string text) => MermaidLabelText.DisplayCaption(text);

    private static IEnumerable<string> KanbanFields(Dictionary<string, string> values) {
        foreach (var key in new[] { "assigned", "priority", "ticket" }) if (values.TryGetValue(key, out var value))
            yield return char.ToUpperInvariant(key[0]) + key.Substring(1) + ": " + value;
        foreach (var field in values.Where(field => !MermaidKanbanMetadata.Known(field.Key)).OrderBy(field => field.Key, StringComparer.OrdinalIgnoreCase))
            yield return field.Key + ": " + field.Value;
    }

    private static void KanbanFacts(Dictionary<string, string> target, string id, string title, Dictionary<string, string> values,
        string? raw, MermaidSourceSpan span, int order) {
        target["mermaid.id"] = id; target["mermaid.source.label"] = title;
        target["mermaid.source.line"] = span.Line.ToString(CultureInfo.InvariantCulture);
        target["mermaid.source.column"] = span.Column.ToString(CultureInfo.InvariantCulture);
        target["mermaid.source.length"] = span.Length.ToString(CultureInfo.InvariantCulture);
        target["mermaid.order"] = order.ToString(CultureInfo.InvariantCulture);
        if (raw != null) target["mermaid.metadata.source"] = raw;
        foreach (var field in values) target["mermaid.meta." + field.Key] = field.Value;
    }

    private static void ValidateKanban(MermaidKanbanDocument document) {
        MermaidResourceGuard.ValidateDocument(document);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in document.Columns) {
            MermaidResourceGuard.Checkpoint();
            Identity(column.Id); Metadata(column.Title, column.Metadata);
            foreach (var task in column.Tasks) { MermaidResourceGuard.Checkpoint(); Identity(task.Id); Metadata(task.Title, task.Metadata); }
        }
        void Identity(string id) {
            if (!MermaidKanbanMetadata.Identity(id) || !ids.Add(id)) throw new ArgumentException("Kanban columns and tasks require unique finite identities.", nameof(document));
        }
        void Metadata(string title, Dictionary<string, string> values) {
            MermaidKanbanMetadata.Scalar("label", title);
            MermaidKanbanMetadata.Bag(values);
            if (string.IsNullOrWhiteSpace(KanbanCaption(MermaidKanbanMetadata.Get(values, "label") ?? title)))
                throw new ArgumentException("Kanban display captions must not be empty.", nameof(document));
        }
    }
}
