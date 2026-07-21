using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WriteManipulationPanel(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options, string graphId) {
        if (!options.IncludeManipulationControls || !scene.Options.HasFeature(GraphSceneFeatures.Manipulation)) return;
        var capabilities = scene.Options.Manipulation;
        writer.Append("<aside class=\"cfx-graph-editor\" data-cfx-role=\"graph-editor\" aria-label=\"Graph editor\" hidden>");
        writer.Append("<header class=\"cfx-graph-editor__header\"><div><strong>Graph editor</strong><span>Validated changes stay local until the host accepts or exports them.</span></div><button type=\"button\" data-cfx-graph-editor-close aria-label=\"Close graph editor\">&#215;</button></header>");
        writer.Append("<div class=\"cfx-graph-editor__actions\" role=\"toolbar\" aria-label=\"Graph edit actions\">");
        if (capabilities.CanAddNodes) WriteEditorAction(writer, "add-node", "Add node");
        if (capabilities.CanEditNodes) WriteEditorAction(writer, "edit-node", "Edit node");
        if (capabilities.CanAddEdges) WriteEditorAction(writer, "add-edge", "Add edge");
        if (capabilities.CanEditEdges) WriteEditorAction(writer, "edit-edge", "Edit edge");
        if (capabilities.CanDeleteNodes || capabilities.CanDeleteEdges) WriteEditorAction(writer, "delete", "Delete selected");
        writer.Append("</div>");
        WriteNodeEditor(writer, graphId, capabilities);
        WriteEdgeEditor(writer, graphId, capabilities);
        writer.Append("<output class=\"cfx-graph-editor__status\" data-cfx-graph-editor-status aria-live=\"polite\"></output></aside>");
    }

    private static void WriteEditorAction(StringBuilder writer, string action, string label) {
        writer.Append("<button type=\"button\" data-cfx-graph-editor-action=\"").Append(action).Append("\">").Append(Text(label)).Append("</button>");
    }

    private static void WriteNodeEditor(StringBuilder writer, string graphId, GraphManipulationOptions capabilities) {
        if (!capabilities.CanAddNodes && !capabilities.CanEditNodes) return;
        writer.Append("<form class=\"cfx-graph-editor__form\" data-cfx-graph-editor-form=\"node\" hidden><h2 data-cfx-graph-editor-title>Node</h2>");
        WriteEditorField(writer, graphId + "-editor-node-id", "Node id", "id", true);
        WriteEditorField(writer, graphId + "-editor-node-label", "Label", "label", true);
        WriteEditorField(writer, graphId + "-editor-node-kind", "Kind", "kind", false);
        writer.Append("<div class=\"cfx-graph-editor__field-row\">");
        WriteEditorField(writer, graphId + "-editor-node-x", "X", "x", false, "number");
        WriteEditorField(writer, graphId + "-editor-node-y", "Y", "y", false, "number");
        writer.Append("</div><button class=\"cfx-graph-editor__submit\" type=\"submit\">Apply node</button></form>");
    }

    private static void WriteEdgeEditor(StringBuilder writer, string graphId, GraphManipulationOptions capabilities) {
        if (!capabilities.CanAddEdges && !capabilities.CanEditEdges) return;
        writer.Append("<form class=\"cfx-graph-editor__form\" data-cfx-graph-editor-form=\"edge\" hidden><h2 data-cfx-graph-editor-title>Edge</h2>");
        WriteEditorField(writer, graphId + "-editor-edge-id", "Edge id", "id", true);
        WriteEditorField(writer, graphId + "-editor-edge-source", "Source node", "source", true);
        WriteEditorField(writer, graphId + "-editor-edge-target", "Target node", "target", true);
        WriteEditorField(writer, graphId + "-editor-edge-label", "Label", "label", false);
        writer.Append("<button class=\"cfx-graph-editor__submit\" type=\"submit\">Apply edge</button></form>");
    }

    private static void WriteEditorField(StringBuilder writer, string id, string label, string field, bool required, string type = "text") {
        writer.Append("<label class=\"cfx-graph-editor__field\" for=\"").Append(Text(id)).Append("\"><span>").Append(Text(label)).Append("</span><input id=\"").Append(Text(id)).Append("\" type=\"").Append(type).Append("\" data-cfx-graph-editor-field=\"").Append(field).Append('"');
        if (required) writer.Append(" required");
        writer.Append("></label>");
    }
}
