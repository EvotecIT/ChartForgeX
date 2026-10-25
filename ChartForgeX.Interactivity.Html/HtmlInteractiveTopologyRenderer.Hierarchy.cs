using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Html;
using ChartForgeX.Interactivity;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveTopologyRenderer {
    /// <summary>Prepares genuine native organization hierarchy markup, exports and optional adaptive drawing.</summary>
    /// <param name="chart">An unchanged layered topology created from explicit hierarchy/team inputs.</param>
    /// <param name="view">Copied collapsed node IDs, sibling tie order and native direction, or null for the full source.</param>
    /// <param name="configure">Optional HTML configuration. Numeric brush, chart comparison, scenarios, progress editing and axis navigators are unavailable.</param>
    /// <returns>A section using the common native acceptance, selection, keyboard, export and bookmark owner.</returns>
    /// <remarks>The host transports hierarchy requests to its retained source and returns this same producer's section and a new basis.
    /// No browser layout, numeric series or axes are manufactured. Ordinary topology RenderPage/RenderFragment output is unchanged.</remarks>
    public HtmlChartSection PrepareHierarchySection(TopologyChart chart, TopologyHierarchyView? view = null, Action<HtmlChartInteractionOptions>? configure = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new HtmlChartInteractionOptions();
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation
            | ChartInteractionFeatures.HierarchyNavigation | ChartInteractionFeatures.StateBookmarks | ChartInteractionFeatures.Export | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan;
        options.Tooltip.Mode = HtmlChartTooltipMode.Single; Localization.CopyTo(options.Localization); configure?.Invoke(options);
        ValidateHierarchyInteractionOptions(options);
        var nativeOptions = chart.ResolveRenderOptions(null);
        var context = VisualExportRequest.ForTopology(chart, nativeOptions).Context;
        var prepared = chart.PrepareHierarchy(context, view, nativeOptions);
        var scope = options.IdScope ?? options.Interaction.ChartId ?? (string.IsNullOrWhiteSpace(chart.Id) ? "topology-hierarchy" : chart.Id!);
        var title = options.PageTitle ?? (string.IsNullOrWhiteSpace(chart.Title) ? chart.Labels.UntitledTopology : chart.Title!);
        var chartId = options.Interaction.ChartId ?? scope;
        HtmlChartSection Build(PreparedVisual drawing, VisualRenderContext drawingContext, bool narrow, Func<HtmlChartSection>? prepareNarrow) =>
            HtmlInteractiveChartRenderer.BuildPreparedSceneSection(new HtmlInteractiveChartRenderer.PreparedSceneSectionSource {
                Drawing = drawing, Title = title, Language = chart.Accessibility.Language,
                Metadata = PreparedTopologyHierarchyMetadata(drawing), ChartId = chartId, Context = drawingContext,
                IntrinsicWidth = context.Layout.Size.Width, Narrow = narrow, ThemeTokens = HtmlInteractiveChartRenderer.PreparedInteractionTokens(drawingContext),
                SvgOptions = new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(narrow ? scope + "-narrow" : scope), nativeOptions.SvgColorVariables),
                HierarchyNavigation = BuildTopologyHierarchyNavigation(options, drawing), LocalizationJson = options.Localization.ToTopologyHierarchyJson(), PrepareNarrow = prepareNarrow
            }, options);
        Func<HtmlChartSection>? narrowFactory = null;
        if (options.ResponsiveLayout == HtmlChartResponsiveLayout.Adaptive) {
            HtmlInteractiveChartRenderer.ValidateAdaptiveOptions(options.Adaptive);
            var narrowContext = HtmlInteractiveChartRenderer.AdaptiveDrawingContext(context, options.Adaptive);
            var narrow = chart.PrepareHierarchy(narrowContext, view, nativeOptions);
            narrowFactory = () => Build(narrow, narrowContext, true, null);
        }
        return Build(prepared, context, false, narrowFactory);
    }

    private static void ValidateHierarchyInteractionOptions(HtmlChartInteractionOptions options) {
        const ChartInteractionFeatures unsupported = ChartInteractionFeatures.Brush | ChartInteractionFeatures.Crosshair | ChartInteractionFeatures.CompareMarkers
            | ChartInteractionFeatures.LegendToggles | ChartInteractionFeatures.Scenarios | ChartInteractionFeatures.StepPlayback | ChartInteractionFeatures.DeepLinks;
        if ((options.Interaction.Features & unsupported) != 0 || options.ProgressEditing.Enabled || options.XNavigation.Enabled || options.Interaction.Scenarios.Count != 0)
            throw new NotSupportedException("Organization hierarchy sections do not expose numeric chart, scenario or progress-editing operations.");
        if (!Enum.IsDefined(typeof(HtmlChartResponsiveLayout), options.ResponsiveLayout) || !Enum.IsDefined(typeof(HtmlChartToolbarLayout), options.Toolbar)
            || !Enum.IsDefined(typeof(HtmlChartTooltipMode), options.Tooltip.Mode)) throw new ArgumentOutOfRangeException(nameof(options));
        if (options.Tooltip.DelayMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(options.Tooltip.DelayMilliseconds));
    }

    private static string PreparedTopologyHierarchyMetadata(PreparedVisual prepared) {
        var captured = prepared.TopologyHierarchyPreparation ?? throw new InvalidOperationException("A native organization source is required.");
        var source = captured.CopySource(); var view = captured.View;
        var json = new StringBuilder("{\"series\":[],\"xLabels\":[],\"regions\":[");
        for (var index = 0; index < prepared.Regions.Count; index++) {
            if (index > 0) json.Append(','); var region = prepared.Regions[index]; var bounds = region.Bounds;
            json.Append("{\"id\":").Append(HtmlJsonString.Encode(region.Id)).Append(",\"role\":").Append(HtmlJsonString.Encode(region.Role))
                .Append(",\"label\":").Append(HtmlJsonString.Encode(region.Label ?? string.Empty)).Append(",\"x\":").Append(bounds.X.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"y\":").Append(bounds.Y.ToString("R", CultureInfo.InvariantCulture)).Append(",\"width\":").Append(bounds.Width.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"height\":").Append(bounds.Height.ToString("R", CultureInfo.InvariantCulture)).Append('}');
        }
        json.Append("],\"hierarchy\":{\"kind\":\"topology\",\"sourceId\":").Append(HtmlJsonString.Encode(source.Id ?? string.Empty))
            .Append(",\"layoutDirection\":").Append(view.LayoutDirection.HasValue ? HtmlJsonString.Encode(view.LayoutDirection.Value.ToString()) : "null")
            .Append(",\"collapsedNodeIds\":"); AppendTopologyHierarchyIds(json, view.CollapsedNodeIds);
        json.Append(",\"nodeOrder\":"); AppendTopologyHierarchyIds(json, view.NodeOrder); json.Append(",\"nodes\":[");
        for (var index = 0; index < source.Nodes.Count; index++) {
            if (index > 0) json.Append(','); var node = source.Nodes[index];
            json.Append("{\"id\":").Append(HtmlJsonString.Encode(node.Id)).Append(",\"label\":").Append(HtmlJsonString.Encode(node.Label))
                .Append(",\"parentId\":").Append(node.Metadata.TryGetValue("hierarchy.parentId", out var parent) ? HtmlJsonString.Encode(parent) : "null")
                .Append(",\"hierarchy\":").Append(node.Metadata.ContainsKey("hierarchy.level") ? "true" : "false").Append('}');
        }
        return json.Append("]}}").ToString();
    }
    private static void AppendTopologyHierarchyIds(StringBuilder json, System.Collections.Generic.IEnumerable<string> ids) =>
        json.Append('[').Append(string.Join(",", ids.Select(HtmlJsonString.Encode))).Append(']');

    private static string BuildTopologyHierarchyNavigation(HtmlChartInteractionOptions options, PreparedVisual prepared) {
        if (!options.Interaction.HasFeature(ChartInteractionFeatures.HierarchyNavigation)) return string.Empty;
        var captured = prepared.TopologyHierarchyPreparation!; var source = captured.CopySource();
        var visible = new System.Collections.Generic.HashSet<string>(prepared.Regions.Where(region => region.Role == "topology-node").Select(region => region.Id), StringComparer.Ordinal);
        var parents = new System.Collections.Generic.HashSet<string>(source.Nodes.Select(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent) ? parent : null)
            .Where(parent => parent != null).Select(parent => parent!), StringComparer.Ordinal);
        var writer = new HtmlMarkupWriter();
        writer.StartElement("nav").Attribute("class", "cfx-hierarchy-navigation").Attribute("data-cfx-hierarchy-navigation", "true")
            .Attribute("aria-label", options.Localization[HtmlChartText.HierarchyNavigation]).EndStartElement();
        writer.StartElement("button").Attribute("type", "button").Attribute("class", "cfx-tool").Attribute("data-cfx-hierarchy-control", "home")
            .BooleanAttribute("disabled").EndStartElement().Text(options.Localization[HtmlChartText.HierarchyHome]).EndElement();
        foreach (var node in source.Nodes.Where(node => visible.Contains(node.Id) && parents.Contains(node.Id) && node.Metadata.ContainsKey("hierarchy.level"))) {
            var collapsed = captured.View.CollapsedNodeIds.Contains(node.Id);
            writer.StartElement("button").Attribute("type", "button").Attribute("class", "cfx-tool").Attribute("data-cfx-hierarchy-control", "toggle")
                .Attribute("data-cfx-hierarchy-node", node.Id).Attribute("aria-expanded", collapsed ? "false" : "true").BooleanAttribute("disabled")
                .EndStartElement().Text(options.Localization.Format(collapsed ? HtmlChartText.HierarchyExpand : HtmlChartText.HierarchyCollapse, ("label", node.Label))).EndElement();
        }
        writer.StartElement("span").Attribute("data-cfx-hierarchy-status", "true").Attribute("role", "status").Attribute("aria-live", "polite")
            .EndStartElement().EndElement().EndElement(); return writer.Build();
    }
}
