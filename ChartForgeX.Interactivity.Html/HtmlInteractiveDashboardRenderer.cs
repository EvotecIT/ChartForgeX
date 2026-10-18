using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Html;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Renders several ChartForgeX charts as one self-contained interactive HTML dashboard.
/// </summary>
public sealed class HtmlInteractiveDashboardRenderer {
    /// <summary>
    /// Renders the specified charts to a complete interactive dashboard document.
    /// </summary>
    /// <param name="charts">The charts to render.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(IEnumerable<Chart> charts) => RenderPage(charts, null);

    /// <summary>
    /// Renders the specified charts to a complete interactive dashboard document.
    /// </summary>
    /// <param name="charts">The charts to render.</param>
    /// <param name="configure">An optional configuration callback for the HTML dashboard adapter.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(IEnumerable<Chart> charts, Action<HtmlInteractiveDashboardOptions>? configure) {
        if (charts == null) throw new ArgumentNullException(nameof(charts));
        var chartArray = charts.ToArray();
        if (chartArray.Length == 0) throw new ArgumentException("Interactive dashboards require at least one chart.", nameof(charts));
        for (var i = 0; i < chartArray.Length; i++) {
            if (chartArray[i] == null) throw new ArgumentException("Interactive dashboards cannot contain null charts.", nameof(charts));
        }

        var options = new HtmlInteractiveDashboardOptions();
        configure?.Invoke(options);
        var title = options.PageTitle ?? "ChartForgeX interactive dashboard";
        var scope = options.IdScope ?? HtmlInteractiveChartRenderer.Slugify(title);
        var groupName = options.Interaction.GroupName ?? scope;

        var surfaceStyle = HtmlInteractivePage.ChartSurfaceStyle(chartArray[0]);
        var writer = HtmlInteractivePage.StartDocument(title, options.ExternalAssets, surfaceStyle,
            options.Localization.ResolveLanguage(chartArray[0].Accessibility.Language), options.Localization.DirectionAttribute);
        writer.StartElement("main").Attribute("class", "cfx-shell").Attribute("style", surfaceStyle).EndStartElement().Line()
            .StartElement("div").Attribute("class", "cfx-dashboard").Attribute("style", "--cfx-dashboard-columns:" + options.Columns.ToString(System.Globalization.CultureInfo.InvariantCulture)).EndStartElement().Line();
        for (var i = 0; i < chartArray.Length; i++) {
            var childOptions = CreateChildOptions(options, scope, groupName, i);
            writer.RawTrusted(HtmlInteractiveChartRenderer.BuildChartSection(chartArray[i], childOptions, title, "document")).Line();
        }

        writer.EndElement().Line()
            .EndElement().Line();
        HtmlInteractivePage.EndDocument(writer, options.ScriptNonce, options.ExternalAssets);
        return writer.Build();
    }

    private static HtmlChartInteractionOptions CreateChildOptions(HtmlInteractiveDashboardOptions options, string scope, string groupName, int index) {
        var chartId = scope + "-" + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var childOptions = new HtmlChartInteractionOptions {
            IdScope = chartId,
            IncludeResetButton = options.IncludeResetButton,
            ResponsiveLayout = options.ResponsiveLayout,
            Toolbar = options.Toolbar
        };
        childOptions.Adaptive.NarrowWidth = options.Adaptive.NarrowWidth;
        childOptions.Adaptive.NarrowHeight = options.Adaptive.NarrowHeight;
        childOptions.Adaptive.Breakpoint = options.Adaptive.Breakpoint;
        HtmlChartTooltipOptionsCopy.Copy(options.Tooltip, childOptions.Tooltip);
        options.Highlight.CopyTo(childOptions.Highlight);
        options.Localization.CopyTo(childOptions.Localization);
        options.Crosshair.CopyTo(childOptions.Crosshair);
        childOptions.Interaction.ChartId = chartId;
        childOptions.Interaction.GroupName = groupName;
        childOptions.Interaction.Features = options.Interaction.Features;
        childOptions.Interaction.SelectionMode = options.Interaction.SelectionMode;
        childOptions.Interaction.BrushAxis = options.Interaction.BrushAxis;
        childOptions.Interaction.ActiveScenarioId = options.Interaction.ActiveScenarioId;
        childOptions.Interaction.EnableDeepLinkState = options.Interaction.EnableDeepLinkState;
        foreach (var scenario in options.Interaction.Scenarios) childOptions.Interaction.Scenarios.Add(CloneScenario(scenario));
        return childOptions;
    }

    private static ChartInteractionScenario CloneScenario(ChartInteractionScenario scenario) {
        var clone = new ChartInteractionScenario {
            Id = scenario.Id,
            Label = scenario.Label,
            Description = scenario.Description,
            Color = scenario.Color,
            PlaybackDelayMilliseconds = scenario.PlaybackDelayMilliseconds,
            LoopPlayback = scenario.LoopPlayback,
            AutoPlay = scenario.AutoPlay,
            FocusMode = scenario.FocusMode
        };
        foreach (var item in scenario.Metadata) clone.Metadata[item.Key] = item.Value;
        foreach (var step in scenario.Steps) {
            var stepClone = new ChartInteractionScenarioStep {
                TargetKind = step.TargetKind,
                TargetId = step.TargetId,
                Label = step.Label,
                Description = step.Description,
                DurationMilliseconds = step.DurationMilliseconds
            };
            foreach (var item in step.Metadata) stepClone.Metadata[item.Key] = item.Value;
            clone.Steps.Add(stepClone);
        }

        return clone;
    }
}
