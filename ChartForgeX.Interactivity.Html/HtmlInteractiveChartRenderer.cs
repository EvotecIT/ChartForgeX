using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Html;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Renders a ChartForgeX chart as a self-contained interactive HTML document.
/// </summary>
public sealed class HtmlInteractiveChartRenderer {
    /// <summary>
    /// Renders the specified chart to a complete interactive HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(Chart chart) => RenderPage(chart, null);

    /// <summary>
    /// Renders the specified chart to a complete interactive HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="configure">An optional configuration callback for the HTML interaction adapter.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(Chart chart, Action<HtmlChartInteractionOptions>? configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new HtmlChartInteractionOptions();
        configure?.Invoke(options);

        var title = options.PageTitle ?? ChartTitle(chart, "ChartForgeX interactive chart");
        var writer = HtmlInteractivePage.StartDocument(title);
        writer.StartElement("main").Attribute("class", "cfx-shell").EndStartElement().Line()
            .RawTrusted(BuildChartSection(chart, options, title)).Line()
            .EndElement().Line();
        HtmlInteractivePage.EndDocument(writer, options.ScriptNonce);
        return writer.Build();
    }

    internal static string InteractiveStyle => HtmlInteractiveAssets.Style;

    internal static string InteractiveScript => HtmlInteractiveAssets.Script;

    internal static string BuildChartSection(Chart chart, HtmlChartInteractionOptions options, string titleFallback) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (options == null) throw new ArgumentNullException(nameof(options));
        var scope = options.IdScope ?? options.Interaction.ChartId ?? Slugify(ChartTitle(chart, titleFallback));
        var chartId = options.Interaction.ChartId ?? scope;
        var scenarioControls = options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Scenarios) && options.Interaction.Scenarios.Count > 0;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("section")
            .Attribute("class", "cfx-interactive-chart")
            .Attribute("data-cfx-chart-id", chartId)
            .Attribute("data-cfx-interaction-features", options.Interaction.Features.ToString())
            .Attribute("data-cfx-interaction-group", options.Interaction.GroupName)
            .Attribute("data-cfx-scenario-count", options.Interaction.Scenarios.Count)
            .Attribute("data-cfx-active-scenario", options.Interaction.ActiveScenarioId)
            .Attribute("data-cfx-deep-link-state", scenarioControls && options.Interaction.EnableDeepLinkState)
            .Attribute("data-cfx-scenario-playback", scenarioControls && options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.StepPlayback) ? "idle" : null)
            .Attribute("data-cfx-scenario-playback-delay", scenarioControls && options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.StepPlayback) ? "900" : null)
            .EndStartElement().Line()
            .StartElement("div").Attribute("class", "cfx-toolbar").EndStartElement()
            .RawTrusted(BuildToolbar(options))
            .EndElement().Line();
        if (scenarioControls) {
            writer.RawTrusted(BuildScenarioControls(options));
            writer.RawTrusted(BuildScenarioPanel(options));
        }

        writer.StartElement("div").Attribute("class", "cfx-stage").EndStartElement().Line()
            .RawTrusted(chart.ToSvg(scope)).Line()
            .StartElement("div").Attribute("class", "cfx-brush-box").BooleanAttribute("hidden").EndStartElement().EndElement().Line()
            .StartElement("div").Attribute("class", "cfx-crosshair").BooleanAttribute("hidden").EndStartElement().Line()
            .StartElement("span").Attribute("class", "cfx-crosshair__line cfx-crosshair__line--x").EndStartElement().EndElement()
            .StartElement("span").Attribute("class", "cfx-crosshair__line cfx-crosshair__line--y").EndStartElement().EndElement()
            .StartElement("span").Attribute("class", "cfx-crosshair__label").Attribute("data-cfx-crosshair-label", "true").EndStartElement().EndElement()
            .EndElement().Line()
            .RawTrusted(BuildRevealLayer(options))
            .RawTrusted(BuildCompareTray(options))
            .EndElement().Line()
            .StartElement("div").Attribute("class", "cfx-tooltip").Attribute("role", "status").Attribute("aria-live", "polite").BooleanAttribute("hidden").EndStartElement().EndElement().Line()
            .EndElement();
        return writer.Build();
    }

    private static string BuildCompareTray(HtmlChartInteractionOptions options) {
        if (!options.Interaction.HasFeature(ChartInteractionFeatures.CompareMarkers)) return string.Empty;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("div")
            .Attribute("class", "cfx-compare-tray")
            .Attribute("data-cfx-compare-tray", "true")
            .Attribute("aria-live", "polite")
            .BooleanAttribute("hidden")
            .EndStartElement()
            .EndElement()
            .Line();
        return writer.Build();
    }

    private static string BuildRevealLayer(HtmlChartInteractionOptions options) {
        if (!options.Interaction.HasFeature(ChartInteractionFeatures.RevealLabels)) return string.Empty;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("div")
            .Attribute("class", "cfx-reveal-layer")
            .Attribute("data-cfx-reveal-layer", "true")
            .Attribute("aria-live", "polite")
            .BooleanAttribute("hidden")
            .EndStartElement()
            .EndElement()
            .Line();
        return writer.Build();
    }

    private static string BuildScenarioControls(HtmlChartInteractionOptions options) {
        var writer = new HtmlMarkupWriter();
        writer.StartElement("div").Attribute("class", "cfx-scenarios").Attribute("aria-label", "Chart scenarios").EndStartElement();
        AppendScenarioButton(writer, string.Empty, "All", "Show all scenarios", string.IsNullOrWhiteSpace(options.Interaction.ActiveScenarioId), null, null, null, null);
        foreach (var scenario in options.Interaction.Scenarios) {
            var active = string.Equals(options.Interaction.ActiveScenarioId, scenario.Id, StringComparison.Ordinal);
            var title = string.IsNullOrWhiteSpace(scenario.Description) ? scenario.Label : scenario.Description!;
            AppendScenarioButton(writer, scenario.Id, scenario.Label, title, active, scenario.Color, scenario.Description, ScenarioStepsJson(scenario), ScenarioMetadataJson(scenario.Metadata));
        }

        writer.EndElement().Line();
        return writer.Build();
    }

    private static string BuildScenarioPanel(HtmlChartInteractionOptions options) {
        var scenario = ActiveScenario(options);
        var stepPlayback = options.Interaction.HasFeature(ChartInteractionFeatures.StepPlayback);
        var writer = new HtmlMarkupWriter();
        writer.StartElement("section")
            .Attribute("class", "cfx-scenario-panel")
            .Attribute("data-cfx-scenario-panel", "true")
            .Attribute("data-cfx-panel-active-scenario", scenario?.Id)
            .Attribute("aria-live", "polite")
            .Attribute("style", scenario == null || string.IsNullOrWhiteSpace(scenario.Color) ? null : "--cfx-scenario-color:" + scenario.Color!.Trim())
            .EndStartElement().Line()
            .StartElement("div").Attribute("class", "cfx-scenario-panel__summary").EndStartElement()
            .StartElement("div").Attribute("class", "cfx-scenario-panel__title").Attribute("data-cfx-scenario-panel-title", "true").EndStartElement().Text(scenario?.Label ?? "All").EndElement()
            .StartElement("div").Attribute("class", "cfx-scenario-panel__meta").Attribute("data-cfx-scenario-panel-meta", "true").EndStartElement().Text(ScenarioSummary(scenario)).EndElement()
            .EndElement().Line();
        if (stepPlayback) {
            writer.StartElement("div").Attribute("class", "cfx-scenario-panel__controls").Attribute("data-cfx-scenario-step-controls", "true").EndStartElement();
            AppendToolbarButton(writer, "Prev", ("data-cfx-scenario-step-control", "previous"), ("title", "Previous step"));
            AppendToolbarButton(writer, "Play", ("data-cfx-scenario-step-control", "play"), ("data-cfx-scenario-play-label", "Play"), ("data-cfx-scenario-pause-label", "Pause"), ("aria-pressed", "false"), ("title", "Play scenario"));
            AppendToolbarButton(writer, "Next", ("data-cfx-scenario-step-control", "next"), ("title", "Next step"));
            AppendToolbarButton(writer, "Clear", ("data-cfx-scenario-step-control", "reset"), ("title", "Clear step"));
            if (options.Interaction.EnableDeepLinkState) AppendToolbarButton(writer, "Link", ("data-cfx-scenario-step-control", "link"), ("title", "Copy scenario link"));
            writer.EndElement().Line();
            writer.StartElement("div")
                .Attribute("class", "cfx-scenario-progress")
                .Attribute("data-cfx-scenario-progress", "true")
                .Attribute("role", "progressbar")
                .Attribute("aria-valuemin", "0")
                .Attribute("aria-valuemax", scenario == null ? "0" : scenario.Steps.Count.ToString(CultureInfo.InvariantCulture))
                .Attribute("aria-valuenow", "0")
                .BooleanAttribute("hidden")
                .EndStartElement()
                .StartElement("span").Attribute("class", "cfx-scenario-progress__bar").Attribute("data-cfx-scenario-progress-bar", "true").EndStartElement().EndElement()
                .StartElement("span").Attribute("class", "cfx-scenario-progress__text").Attribute("data-cfx-scenario-progress-text", "true").EndStartElement().Text("Step 0 / " + (scenario == null ? "0" : scenario.Steps.Count.ToString(CultureInfo.InvariantCulture))).EndElement()
                .EndElement().Line();
        }

        writer.StartElement("ol").Attribute("class", "cfx-scenario-panel__steps").Attribute("data-cfx-scenario-panel-steps", "true").EndStartElement();
        if (stepPlayback && scenario != null) {
            var activeScenario = scenario;
            for (var i = 0; i < activeScenario.Steps.Count; i++) WriteScenarioStep(writer, activeScenario.Steps[i], i);
        }

        writer.EndElement().Line().EndElement().Line();
        return writer.Build();
    }

    private static void AppendScenarioButton(HtmlMarkupWriter writer, string id, string label, string title, bool active, string? color, string? description, string? stepsJson, string? metadataJson) {
        writer.StartElement("button")
            .Attribute("class", "cfx-scenario")
            .Attribute("type", "button")
            .Attribute("data-cfx-scenario", id)
            .Attribute("data-cfx-scenario-color", color)
            .Attribute("data-cfx-scenario-label", label)
            .Attribute("data-cfx-scenario-description", description)
            .Attribute("data-cfx-scenario-steps", stepsJson)
            .Attribute("data-cfx-scenario-metadata", metadataJson)
            .Attribute("aria-pressed", active)
            .Attribute("title", title)
            .Attribute("style", string.IsNullOrWhiteSpace(color) ? null : "--cfx-scenario-color:" + color!.Trim())
            .EndStartElement()
            .Text(label)
            .EndElement();
    }

    private static void WriteScenarioStep(HtmlMarkupWriter writer, ChartInteractionScenarioStep step, int index) {
        writer.StartElement("li")
            .Attribute("data-cfx-scenario-step-index", index + 1)
            .Attribute("data-cfx-scenario-target-kind", step.TargetKind)
            .Attribute("data-cfx-scenario-target-id", step.TargetId)
            .Attribute("role", "button")
            .Attribute("tabindex", "0")
            .EndStartElement()
            .Text(string.IsNullOrWhiteSpace(step.Label) ? step.TargetId : step.Label!)
            .EndElement();
    }

    private static ChartInteractionScenario? ActiveScenario(HtmlChartInteractionOptions options) {
        if (string.IsNullOrWhiteSpace(options.Interaction.ActiveScenarioId)) return null;
        foreach (var scenario in options.Interaction.Scenarios) {
            if (string.Equals(scenario.Id, options.Interaction.ActiveScenarioId, StringComparison.Ordinal)) return scenario;
        }

        return null;
    }

    private static string ScenarioSummary(ChartInteractionScenario? scenario) {
        if (scenario == null) return "All chart data visible";
        if (!string.IsNullOrWhiteSpace(scenario.Description)) return scenario.Description!;
        return scenario.Steps.Count.ToString("0", CultureInfo.InvariantCulture) + " steps";
    }

    private static string ScenarioStepsJson(ChartInteractionScenario scenario) {
        var items = new List<string>();
        for (var i = 0; i < scenario.Steps.Count; i++) {
            var step = scenario.Steps[i];
            items.Add("{\"index\":" + i.ToString(CultureInfo.InvariantCulture) +
                ",\"targetKind\":\"" + JsonEscape(step.TargetKind) +
                "\",\"targetId\":\"" + JsonEscape(step.TargetId) +
                "\",\"label\":\"" + JsonEscape(step.Label) +
                "\",\"description\":\"" + JsonEscape(step.Description) +
                "\",\"metadata\":" + ScenarioMetadataJson(step.Metadata) + "}");
        }

        return "[" + string.Join(",", items.ToArray()) + "]";
    }

    private static string ScenarioMetadataJson(Dictionary<string, string> metadata) {
        if (metadata.Count == 0) return "{}";
        var items = new List<string>();
        foreach (var item in metadata.OrderBy(item => item.Key, StringComparer.Ordinal)) items.Add("\"" + JsonEscape(item.Key) + "\":\"" + JsonEscape(item.Value) + "\"");
        return "{" + string.Join(",", items.ToArray()) + "}";
    }

    private static string JsonEscape(string? value) {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var builder = new System.Text.StringBuilder(value!.Length);
        foreach (var ch in value) {
            switch (ch) {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(ch)) builder.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    else builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }

    private static string BuildToolbar(HtmlChartInteractionOptions options) {
        var writer = new HtmlMarkupWriter();
        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Zoom)) {
            AppendToolbarButton(writer, "Zoom +", ("data-cfx-zoom", "in"), ("title", "Zoom in"));
            AppendToolbarButton(writer, "Zoom -", ("data-cfx-zoom", "out"), ("title", "Zoom out"));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Pan)) {
            AppendToolbarButton(writer, "Pan", ("data-cfx-mode-button", "pan"), ("aria-pressed", "false"), ("title", "Pan chart"));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Brush)) {
            AppendToolbarButton(writer, "Brush", ("data-cfx-mode-button", "brush"), ("aria-pressed", "false"), ("title", "Brush select region"));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Export)) {
            AppendToolbarButton(writer, "SVG", ("data-cfx-export", "svg"), ("title", "Download SVG"));
            AppendToolbarButton(writer, "PNG", ("data-cfx-export", "png"), ("title", "Download PNG"));
        }

        if (options.IncludeResetButton) {
            AppendToolbarButton(writer, "Reset", ("data-cfx-reset", "true"));
        }

        return writer.Build();
    }

    private static void AppendToolbarButton(HtmlMarkupWriter writer, string label, params (string Name, string Value)[] attributes) {
        writer.StartElement("button")
            .Attribute("class", "cfx-tool")
            .Attribute("type", "button");
        for (var i = 0; i < attributes.Length; i++) {
            writer.Attribute(attributes[i].Name, attributes[i].Value);
        }

        writer.EndStartElement().Text(label).EndElement();
    }

    internal static string ChartTitle(Chart chart, string fallback) => string.IsNullOrWhiteSpace(chart.Title) ? fallback : chart.Title;

    internal static string Slugify(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var sb = new System.Text.StringBuilder(value.Length);
        var previousDash = false;
        foreach (var ch in value) {
            if (char.IsLetterOrDigit(ch)) {
                sb.Append(char.ToLowerInvariant(ch));
                previousDash = false;
            } else if (!previousDash && sb.Length > 0) {
                sb.Append('-');
                previousDash = true;
            }
        }

        if (previousDash && sb.Length > 0) sb.Length--;
        return sb.Length == 0 ? "chart" : sb.ToString();
    }
}
