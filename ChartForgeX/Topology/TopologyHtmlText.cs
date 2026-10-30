using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Topology;

/// <summary>Resolved interactive HTML text supplied by the optional adapter; native facts and rendering stay separate.</summary>
internal sealed class TopologyHtmlText {
    internal static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["ZoomIn"] = "Zoom in",
        ["ZoomOut"] = "Zoom out",
        ["Clear"] = "Clear",
        ["All"] = "All",
        ["ShowAllScenarios"] = "Show all scenarios",
        ["Steps"] = "{count} steps",
        ["Previous"] = "Prev",
        ["PreviousStep"] = "Previous step",
        ["Play"] = "Play",
        ["Next"] = "Next",
        ["NextStep"] = "Next step",
        ["ClearStep"] = "Clear step",
        ["Link"] = "Link",
        ["ExplorerOverview"] = "Overview",
        ["ExplorerFocus"] = "Focus",
        ["ExplorerStatus"] = "Status",
        ["ExplorerKind"] = "Kind",
        ["ExplorerNode"] = "Node",
        ["ExplorerEdge"] = "Edge",
        ["ExplorerGroup"] = "Group",
        ["ExplorerLabel"] = "Label",
        ["ExplorerSelection"] = "Selection",
        ["ExplorerNoItemSelected"] = "No item selected",
        ["ExplorerId"] = "ID",
        ["ExplorerRelatedNodes"] = "Related nodes",
        ["ExplorerRelatedEdges"] = "Related edges",
        ["TopologyControls"] = "Topology controls",
        ["TopologyDiagram"] = "Topology diagram",
        ["TopologyFit"] = "Fit topology",
        ["TopologyResetViewport"] = "Reset viewport",
        ["TopologyOpenFullscreen"] = "Open topology fullscreen",
        ["TopologyExportSvg"] = "Export SVG",
        ["TopologyExportPng"] = "Export PNG",
        ["TopologyForceControls"] = "Force graph controls",
        ["TopologyFindPlaceholder"] = "Find node, edge, group",
        ["TopologyFind"] = "Find node, edge, or group",
        ["TopologyHealthy"] = "Healthy",
        ["TopologyWarning"] = "Warning",
        ["TopologyCritical"] = "Critical",
        ["TopologyUnknown"] = "Unknown",
        ["TopologyDisabled"] = "Disabled",
        ["TopologyEdges"] = "Edges",
        ["TopologyLabels"] = "Labels",
        ["TopologyGroups"] = "Groups",
        ["TopologyQuietPan"] = "Quiet pan",
        ["TopologyGraphReady"] = "Graph ready",
        ["TopologyAllGroups"] = "All groups",
        ["TopologyScenarios"] = "Topology scenarios",
        ["TopologyShowAll"] = "Show all",
        ["TopologyAllRoutesVisible"] = "All topology routes visible",
        ["TopologyPlayRoute"] = "Play route",
        ["TopologyCopyRouteLink"] = "Copy route link",
        ["TopologyTimelineStep"] = "Topology route timeline step",
        ["TopologyVisibleSummary"] = "{nodes} nodes / {edges} edges visible",
    };

    private readonly IReadOnlyDictionary<string, string> _messages;
    internal TopologyHtmlText(IReadOnlyDictionary<string, string>? messages = null, string? json = null, string? language = null, string? direction = null) {
        _messages = messages ?? Defaults; Json = json; Language = language; Direction = direction;
    }
    internal string? Json { get; }
    internal string? Language { get; }
    internal string? Direction { get; }
    internal string this[string key] => _messages.TryGetValue(key, out var message) ? message : Defaults[key];
    internal string Format(string key, params (string Name, string Value)[] values) => FormatTemplate(this[key], values);

    /// <summary>Replaces named values without interpreting authored text as markup or changing numeric formatting.</summary>
    internal static string FormatTemplate(string template, params (string Name, string Value)[] values) {
        return Regex.Replace(template, @"\{([^{}]+)\}", match => {
            foreach (var value in values) if (value.Name == match.Groups[1].Value) return value.Value;
            return match.Value;
        });
    }
}
