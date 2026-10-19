using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WriteHeader(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options, IReadOnlyList<GraphSceneCluster> clusters, string domId) {
        var text = options.Localization;
        writer.Append("<header class=\"cfx-graph-header\"><div class=\"cfx-graph-heading\"><span class=\"cfx-graph-eyebrow\">" + Text(text[HtmlChartText.GraphEyebrow]) + "</span><h1 class=\"cfx-graph-title\"");
        Attribute(writer, "id", domId + "-heading");
        writer.Append('>');
        writer.Append(Text(scene.Title));
        writer.Append("</h1>");
        if (!string.IsNullOrWhiteSpace(scene.Subtitle)) {
            writer.Append("<p class=\"cfx-graph-subtitle\">");
            writer.Append(Text(scene.Subtitle!));
            writer.Append("</p>");
        }

        writer.Append("</div><div class=\"cfx-graph-toolbar\" aria-label=\"" + Text(text[HtmlChartText.GraphFindAndAppearanceControls]) + "\">");
        WriteQueryControls(writer, scene, options, clusters, domId);
        if (options.IncludeThemeToggle) {
            BeginControlGroup(writer, text[HtmlChartText.GraphAppearance], "cfx-graph-toolbar-group--appearance");
            WriteButton(writer, "theme", text[HtmlChartText.GraphThemeSystem], false, false, text[HtmlChartText.GraphChangeTheme]);
            EndControlGroup(writer);
        }

        writer.Append("</div>");
        WritePhysicsConfigurator(writer, scene, options);
        writer.Append("</header>");
    }

    private static void WriteAnnouncer(StringBuilder writer) {
        writer.Append("<output class=\"cfx-visually-hidden\" data-cfx-role=\"graph-announcer\" aria-live=\"polite\" aria-atomic=\"true\"></output>");
    }

    private static void WriteNeighborhoodNavigation(StringBuilder writer, GraphScene scene, HtmlChartLocalization text) {
        if (scene.Options.HasFeature(GraphSceneFeatures.NeighborhoodFocus)) {
            writer.Append("<nav class=\"cfx-graph-neighborhood-navigation\" data-cfx-role=\"graph-neighborhood-navigation\" aria-label=\"" + Text(text[HtmlChartText.GraphNeighborhoodNavigation]) + "\" hidden>");
            WriteButton(writer, "focus-home", text[HtmlChartText.ExplorerOverview], false, true);
            WriteButton(writer, "focus-back", text[HtmlChartText.GraphBack], false, true);
            WriteButton(writer, "focus-previous", text[HtmlChartText.GraphPreviousNeighbors], false, true);
            WriteButton(writer, "focus-next", text[HtmlChartText.GraphNextNeighbors], false, true);
            writer.Append("<span data-cfx-role=\"graph-neighborhood-summary\" aria-live=\"polite\"></span></nav>");
        }
    }

    private static void WriteStageControls(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options, IReadOnlyList<GraphSceneCluster> clusters) {
        var text = options.Localization;
        WriteHierarchyNavigation(writer, scene, text);
        writer.Append("<div class=\"cfx-graph-command-rail\" role=\"toolbar\" aria-label=\"" + Text(text[HtmlChartText.GraphCommands]) + "\"><div class=\"cfx-graph-command-scroll\">");
        WriteExploreControls(writer, scene, options, clusters);
        WriteSelectionControls(writer, scene, text);
        WriteViewportControls(writer, scene, options);
        WritePhysicsControls(writer, scene, options);
        WriteManipulationControls(writer, scene, options);
        writer.Append("</div>");
        WriteExportControls(writer, scene, text);
        writer.Append("</div>");
    }

    private static void WriteQueryControls(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options, IReadOnlyList<GraphSceneCluster> clusters, string domId) {
        var text = options.Localization;
        var includeSearch = options.IncludeSearch && scene.Options.HasFeature(GraphSceneFeatures.Search);
        var includeFilters = options.IncludeFilters && scene.Options.HasFeature(GraphSceneFeatures.Filtering);
        if (!includeSearch && !includeFilters) return;
        BeginControlGroup(writer, text[HtmlChartText.GraphFindAndFilter], "cfx-graph-toolbar-group--query");
        if (includeSearch) {
            writer.Append("<label class=\"cfx-graph-search-field\"");
            Attribute(writer, "for", domId + "-search");
            writer.Append("><span class=\"cfx-visually-hidden\">" + Text(text[HtmlChartText.GraphSearch]) + "</span>");
            WriteIcon(writer, "search");
            writer.Append("<input class=\"cfx-graph-search\" type=\"search\" data-cfx-graph-search=\"true\" placeholder=\"" + Text(text[HtmlChartText.GraphSearchPlaceholder]) + "\" autocomplete=\"off\"");
            Attribute(writer, "id", domId + "-search");
            Attribute(writer, "aria-describedby", domId + "-search-status");
            writer.Append("></label><output class=\"cfx-graph-search-status\" data-cfx-role=\"graph-search-status\" aria-live=\"polite\"");
            Attribute(writer, "id", domId + "-search-status");
            writer.Append("></output>");
        }

        if (includeFilters) {
            WriteFilter(writer, "status", scene.Nodes.Select(node => node.Status).Concat(scene.Edges.Select(edge => edge.Status)), text);
            var kindValues = scene.Nodes.Select(node => node.Kind).Concat(scene.Edges.Select(edge => edge.Kind));
            if (scene.Options.HasFeature(GraphSceneFeatures.Clustering)) kindValues = kindValues.Concat(clusters.Select(cluster => cluster.Kind));
            WriteFilter(writer, "kind", kindValues, text);
        }

        EndControlGroup(writer);
    }

    private static void WriteExploreControls(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options, IReadOnlyList<GraphSceneCluster> clusters) {
        var text = options.Localization;
        var clustersEnabled = options.IncludeClusterControls && clusters.Count > 0 && scene.Options.HasFeature(GraphSceneFeatures.Clustering);
        var focusEnabled = scene.Options.HasFeature(GraphSceneFeatures.Selection) && scene.Options.HasFeature(GraphSceneFeatures.NeighborhoodFocus);
        var selectionEnabled = scene.Options.HasFeature(GraphSceneFeatures.Selection);
        if (!clustersEnabled && !focusEnabled && !selectionEnabled) return;
        BeginControlGroup(writer, text[HtmlChartText.GraphExplore], null);
        if (clustersEnabled) WriteButton(writer, "clusters", text[HtmlChartText.GraphClusters], true, true);
        if (focusEnabled) WriteButton(writer, "focus", text[HtmlChartText.ExplorerFocus], true, true);
        if (selectionEnabled) WriteButton(writer, "clear-selection", text[HtmlChartText.Clear], false, true, text[HtmlChartText.GraphClearSelection]);
        EndControlGroup(writer);
    }

    private static void WriteViewportControls(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options) {
        var text = options.Localization;
        if (!ShouldRenderViewportControls(scene, options)) return;
        BeginControlGroup(writer, text[HtmlChartText.GraphView], null);
        WriteButton(writer, "fit", text[HtmlChartText.GraphFit], false, true, text[HtmlChartText.GraphFitToView]);
        WriteButton(writer, "zoom-in", text[HtmlChartText.ZoomIn], false, true);
        WriteButton(writer, "zoom-out", text[HtmlChartText.ZoomOut], false, true);
        EndControlGroup(writer);
    }

    private static void WriteSelectionControls(StringBuilder writer, GraphScene scene, HtmlChartLocalization text) {
        if (!scene.Options.HasFeature(GraphSceneFeatures.BoxSelection)) return;
        BeginControlGroup(writer, text[HtmlChartText.GraphSelect], null);
        WriteButton(writer, "box-select", text[HtmlChartText.GraphBoxSelect], true, true, text[HtmlChartText.GraphToggleBoxSelection]);
        EndControlGroup(writer);
    }

    private static void WriteManipulationControls(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options) {
        var text = options.Localization;
        if (!options.IncludeManipulationControls || !scene.Options.HasFeature(GraphSceneFeatures.Manipulation)) return;
        BeginControlGroup(writer, text[HtmlChartText.GraphEdit], null);
        WriteButton(writer, "edit", text[HtmlChartText.GraphEdit], true, true, text[HtmlChartText.GraphOpenEditor]);
        if (scene.Options.HasFeature(GraphSceneFeatures.History)) {
            WriteButton(writer, "undo", text[HtmlChartText.GraphUndo], false, true, text[HtmlChartText.GraphUndoChange]);
            WriteButton(writer, "redo", text[HtmlChartText.GraphRedo], false, true, text[HtmlChartText.GraphRedoChange]);
        }
        if (scene.Options.Manipulation.CanPersistPositions) WriteButton(writer, "save-positions", text[HtmlChartText.GraphSave], false, true, text[HtmlChartText.GraphPublishPositions]);
        EndControlGroup(writer);
    }

    private static void WritePhysicsControls(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options) {
        var text = options.Localization;
        if (!options.IncludePhysicsControls || !CanRunRuntimePhysics(scene)) return;
        BeginControlGroup(writer, text[HtmlChartText.GraphPhysics], null);
        WriteButton(writer, "physics", text[HtmlChartText.GraphPhysics], true, true);
        if (scene.Options.HasFeature(GraphSceneFeatures.Stabilization)) WriteButton(writer, "stabilize", text[HtmlChartText.GraphStabilize], false, true);
        EndControlGroup(writer);
    }

    private static void WriteExportControls(StringBuilder writer, GraphScene scene, HtmlChartLocalization text) {
        if (!scene.Options.HasFeature(GraphSceneFeatures.Export)) return;
        BeginControlGroup(writer, text[HtmlChartText.GraphExport], null);
        writer.Append("<details class=\"cfx-graph-command-menu\"><summary class=\"cfx-graph-tool cfx-graph-tool--compact\" role=\"button\" aria-label=\"" + Text(text[HtmlChartText.GraphExportGraph]) + "\" aria-expanded=\"false\" data-cfx-tooltip=\"" + Text(text[HtmlChartText.GraphExportGraph]) + "\">");
        WriteIcon(writer, "download");
        writer.Append("<span class=\"cfx-graph-tool-label\">" + Text(text[HtmlChartText.GraphExport]) + "</span></summary><div class=\"cfx-graph-command-menu-panel\" role=\"group\" aria-label=\"" + Text(text[HtmlChartText.GraphExportFormat]) + "\">");
        WriteButton(writer, "export-svg", text[HtmlChartText.GraphSvgDocument], false, false, text[HtmlChartText.GraphExportSvg]);
        WriteButton(writer, "export-png", text[HtmlChartText.GraphPngImage], false, false, text[HtmlChartText.GraphExportPng]);
        WriteButton(writer, "export-json", text[HtmlChartText.GraphJsonData], false, false, text[HtmlChartText.GraphExportJson]);
        writer.Append("</div></details>");
        EndControlGroup(writer);
    }

    private static void BeginControlGroup(StringBuilder writer, string label, string? additionalClass) {
        writer.Append("<div class=\"cfx-graph-toolbar-group");
        if (!string.IsNullOrWhiteSpace(additionalClass)) writer.Append(' ').Append(additionalClass);
        writer.Append("\" role=\"group\"");
        Attribute(writer, "aria-label", label);
        writer.Append('>');
    }

    private static void EndControlGroup(StringBuilder writer) => writer.Append("</div>");

    private static void WriteFilter(StringBuilder writer, string name, IEnumerable<string?> values, HtmlChartLocalization text) {
        var options = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (options.Length == 0) return;
        var label = text[name == "status" ? HtmlChartText.GraphFilterStatus : HtmlChartText.GraphFilterKind];
        var all = text[name == "status" ? HtmlChartText.GraphAllStatus : HtmlChartText.GraphAllKind];
        writer.Append("<label class=\"cfx-graph-filter-field\"><span class=\"cfx-visually-hidden\">").Append(Text(label));
        writer.Append("</span><select class=\"cfx-graph-filter\"");
        Attribute(writer, "data-cfx-graph-filter", name); Attribute(writer, "aria-label", label);
        writer.Append("><option value=\"\">").Append(Text(all)).Append("</option>");
        foreach (var value in options) {
            writer.Append("<option"); Attribute(writer, "value", value);
            writer.Append('>').Append(Text(value)).Append("</option>");
        }
        writer.Append("</select></label>");
    }

    private static void WriteButton(StringBuilder writer, string action, string label, bool pressedState = false, bool compact = false, string? accessibleLabel = null) {
        writer.Append("<button class=\"cfx-graph-tool");
        if (compact) writer.Append(" cfx-graph-tool--compact");
        writer.Append("\" type=\"button\"");
        Attribute(writer, "data-cfx-graph-action", action);
        Attribute(writer, "aria-label", accessibleLabel ?? label);
        Attribute(writer, "data-cfx-tooltip", accessibleLabel ?? label);
        if (pressedState) writer.Append(" aria-pressed=\"false\"");
        writer.Append('>');
        WriteIcon(writer, action);
        writer.Append("<span class=\"cfx-graph-tool-label\">");
        writer.Append(Text(label));
        writer.Append("</span></button>");
    }

    private static void WriteIcon(StringBuilder writer, string name) {
        writer.Append("<svg class=\"cfx-graph-icon\" viewBox=\"0 0 24 24\" aria-hidden=\"true\" focusable=\"false\">");
        foreach (var path in IconPaths(name)) {
            writer.Append("<path d=\"").Append(path).Append("\"></path>");
        }
        writer.Append("</svg>");
    }

    private static string[] IconPaths(string name) {
        switch (name) {
            case "search": return new[] { "M21 21l-4.35-4.35", "M19 11a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z" };
            case "chevron-down": return new[] { "M7 9l5 5 5-5" };
            case "clusters": return new[] { "M12 3a2 2 0 1 0 0 4 2 2 0 0 0 0-4Z", "M6 17a2 2 0 1 0 0 4 2 2 0 0 0 0-4Z", "M18 17a2 2 0 1 0 0 4 2 2 0 0 0 0-4Z", "M12 7v4", "M6 15v-4h12v4" };
            case "focus": return new[] { "M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z", "M12 2v3", "M12 19v3", "M2 12h3", "M19 12h3" };
            case "clear-selection": return new[] { "M5 5h6v6H5Z", "M13 13h6v6h-6Z", "M4 20 20 4" };
            case "box-select": return new[] { "M4 8V4h4", "M16 4h4v4", "M20 16v4h-4", "M8 20H4v-4", "M8 8h8v8H8Z" };
            case "edit": return new[] { "M4 20h4l11-11-4-4L4 16v4Z", "M13.5 6.5l4 4" };
            case "undo": return new[] { "M9 7 4 12l5 5", "M5 12h8a6 6 0 0 1 6 6" };
            case "redo": return new[] { "M15 7l5 5-5 5", "M19 12h-8a6 6 0 0 0-6 6" };
            case "save-positions": return new[] { "M5 4h12l2 2v14H5Z", "M8 4v6h8V4", "M8 20v-6h8v6" };
            case "fit": return new[] { "M9 4H4v5", "M15 4h5v5", "M20 15v5h-5", "M9 20H4v-5" };
            case "zoom-in": return new[] { "M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16Z", "M8 12h8", "M12 8v8" };
            case "zoom-out": return new[] { "M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16Z", "M8 12h8" };
            case "physics": return new[] { "M3 12h3l2-6 4 12 3-9 2 3h4" };
            case "stabilize": return new[] { "M12 7a5 5 0 1 0 0 10 5 5 0 0 0 0-10Z", "M12 2v2", "M12 20v2", "M2 12h2", "M20 12h2", "M12 10v4", "M10 12h4" };
            case "download": return new[] { "M12 3v12", "M8 11l4 4 4-4", "M5 20h14" };
            case "export-svg": return new[] { "M8 7l-5 5 5 5", "M16 7l5 5-5 5", "M14 4 10 20" };
            case "export-png": return new[] { "M4 5h16v14H4z", "m5 14 4-5 3 3 2-2 2 4", "M9 9h.01" };
            case "export-json": return new[] { "M8 4H6a2 2 0 0 0-2 2v3a2 2 0 0 1-2 2 2 2 0 0 1 2 2v3a2 2 0 0 0 2 2h2", "M16 4h2a2 2 0 0 1 2 2v3a2 2 0 0 0 2 2 2 2 0 0 0-2 2v3a2 2 0 0 1-2 2h-2" };
            case "focus-home":
            case "hierarchy-home": return new[] { "M3 11.5 12 4l9 7.5", "M5 10v10h14V10", "M9 20v-6h6v6" };
            case "focus-back": return new[] { "M9 7 4 12l5 5", "M5 12h14" };
            case "focus-previous": return new[] { "M14 6 8 12l6 6" };
            case "focus-next": return new[] { "M10 6l6 6-6 6" };
            case "hierarchy-up": return new[] { "M5 12l7-7 7 7", "M12 5v14" };
            case "theme": return new[] { "M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z", "M12 2v2", "M12 20v2", "M2 12h2", "M20 12h2", "M4.93 4.93l1.42 1.42", "M17.65 17.65l1.42 1.42", "M19.07 4.93l-1.42 1.42", "M6.35 17.65l-1.42 1.42" };
            default: return new[] { "M5 12h14" };
        }
    }

    private static string Theme(HtmlGraphExplorerTheme theme) {
        switch (theme) {
            case HtmlGraphExplorerTheme.System: return "system";
            case HtmlGraphExplorerTheme.Light: return "light";
            case HtmlGraphExplorerTheme.Dark: return "dark";
            default: throw new InvalidOperationException("Graph explorer theme is unsupported: " + theme);
        }
    }
}
