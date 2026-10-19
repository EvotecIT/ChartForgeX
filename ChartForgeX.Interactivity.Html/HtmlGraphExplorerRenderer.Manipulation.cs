using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WriteManipulationPanel(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options, string graphId) {
        var text = options.Localization;
        if (!options.IncludeManipulationControls || !scene.Options.HasFeature(GraphSceneFeatures.Manipulation)) return;
        var capabilities = scene.Options.Manipulation;
        writer.Append("<aside class=\"cfx-graph-editor\" data-cfx-role=\"graph-editor\" aria-label=\"" + Text(text[HtmlChartText.GraphEditor]) + "\" hidden>");
        writer.Append("<header class=\"cfx-graph-editor__header\"><div><strong>" + Text(text[HtmlChartText.GraphEditor]) + "</strong><span>" + Text(text[HtmlChartText.GraphEditorHint]) + "</span></div><button type=\"button\" data-cfx-graph-editor-close aria-label=\"" + Text(text[HtmlChartText.GraphCloseEditor]) + "\">&#215;</button></header>");
        writer.Append("<div class=\"cfx-graph-editor__actions\" role=\"toolbar\" aria-label=\"" + Text(text[HtmlChartText.GraphEditActions]) + "\">");
        if (capabilities.CanAddNodes) WriteEditorAction(writer, "add-node", text[HtmlChartText.GraphAddNode]);
        if (capabilities.CanEditNodes) WriteEditorAction(writer, "edit-node", text[HtmlChartText.GraphEditNode]);
        if (capabilities.CanAddEdges) WriteEditorAction(writer, "add-edge", text[HtmlChartText.GraphAddEdge]);
        if (capabilities.CanEditEdges) WriteEditorAction(writer, "edit-edge", text[HtmlChartText.GraphEditEdge]);
        if (capabilities.CanDeleteNodes || capabilities.CanDeleteEdges) WriteEditorAction(writer, "delete", text[HtmlChartText.GraphDeleteSelected]);
        writer.Append("</div>");
        WriteNodeEditor(writer, graphId, capabilities, text);
        WriteEdgeEditor(writer, graphId, capabilities, text);
        writer.Append("<output class=\"cfx-graph-editor__status\" data-cfx-graph-editor-status aria-live=\"polite\"></output></aside>");
    }

    private static void WriteEditorAction(StringBuilder writer, string action, string label) {
        writer.Append("<button type=\"button\" data-cfx-graph-editor-action=\"").Append(action).Append("\">").Append(Text(label)).Append("</button>");
    }

    private static void WriteNodeEditor(StringBuilder writer, string graphId, GraphManipulationOptions capabilities, HtmlChartLocalization text) {
        if (!capabilities.CanAddNodes && !capabilities.CanEditNodes) return;
        writer.Append("<form class=\"cfx-graph-editor__form\" data-cfx-graph-editor-form=\"node\" hidden><h2 data-cfx-graph-editor-title>" + Text(text[HtmlChartText.ExplorerNode]) + "</h2>");
        WriteEditorField(writer, graphId + "-editor-node-id", text[HtmlChartText.GraphNodeId], "id", true);
        WriteEditorField(writer, graphId + "-editor-node-label", text[HtmlChartText.ExplorerLabel], "label", true);
        WriteEditorField(writer, graphId + "-editor-node-kind", text[HtmlChartText.ExplorerKind], "kind", false);
        writer.Append("<div class=\"cfx-graph-editor__field-row\">");
        WriteEditorField(writer, graphId + "-editor-node-x", text[HtmlChartText.GraphAxisX], "x", false, "number");
        WriteEditorField(writer, graphId + "-editor-node-y", text[HtmlChartText.GraphAxisY], "y", false, "number");
        writer.Append("</div><button class=\"cfx-graph-editor__submit\" type=\"submit\">" + Text(text[HtmlChartText.GraphApplyNode]) + "</button></form>");
    }

    private static void WriteEdgeEditor(StringBuilder writer, string graphId, GraphManipulationOptions capabilities, HtmlChartLocalization text) {
        if (!capabilities.CanAddEdges && !capabilities.CanEditEdges) return;
        writer.Append("<form class=\"cfx-graph-editor__form\" data-cfx-graph-editor-form=\"edge\" hidden><h2 data-cfx-graph-editor-title>" + Text(text[HtmlChartText.ExplorerEdge]) + "</h2>");
        WriteEditorField(writer, graphId + "-editor-edge-id", text[HtmlChartText.GraphEdgeId], "id", true);
        WriteEditorField(writer, graphId + "-editor-edge-source", text[HtmlChartText.GraphSourceNode], "source", true);
        WriteEditorField(writer, graphId + "-editor-edge-target", text[HtmlChartText.GraphTargetNode], "target", true);
        WriteEditorField(writer, graphId + "-editor-edge-label", text[HtmlChartText.ExplorerLabel], "label", false);
        writer.Append("<button class=\"cfx-graph-editor__submit\" type=\"submit\">" + Text(text[HtmlChartText.GraphApplyEdge]) + "</button></form>");
    }

    private static void WriteEditorField(StringBuilder writer, string id, string label, string field, bool required, string type = "text") {
        writer.Append("<label class=\"cfx-graph-editor__field\" for=\"").Append(Text(id)).Append("\"><span>").Append(Text(label)).Append("</span><input id=\"").Append(Text(id)).Append("\" type=\"").Append(type).Append("\" data-cfx-graph-editor-field=\"").Append(field).Append('"');
        if (required) writer.Append(" required");
        writer.Append("></label>");
    }
}
