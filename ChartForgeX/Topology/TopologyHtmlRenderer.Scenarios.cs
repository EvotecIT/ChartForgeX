using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Html;

namespace ChartForgeX.Topology;

public sealed partial class TopologyHtmlRenderer {
    private static void WriteScenarioControls(HtmlMarkupWriter writer, string scenariosClass, TopologyChart chart, string? activeScenarioId, TopologyHtmlScenarioControlMode mode, TopologyHtmlText messages) {
        writer.StartElement("div").Attribute("class", scenariosClass).Attribute("aria-label", messages["TopologyScenarios"]).EndStartElement();
        if (mode == TopologyHtmlScenarioControlMode.Checkboxes) {
            foreach (var scenario in chart.Scenarios) {
                var active = string.Equals(activeScenarioId, scenario.Id, StringComparison.Ordinal);
                writer.StartElement("label")
                    .Attribute("data-cfx-topology-scenario-label", scenario.Id)
                    .Attribute("style", string.IsNullOrWhiteSpace(scenario.Color) ? null : "--cfx-topology-scenario-color:" + scenario.Color!.Trim())
                    .EndStartElement();
                writer.StartElement("input")
                    .Attribute("type", "checkbox")
                    .Attribute("data-cfx-topology-scenario-toggle", scenario.Id)
                    .Attribute("data-cfx-scenario-color", scenario.Color)
                    .Attribute("data-cfx-scenario-label", scenario.Label)
                    .Attribute("data-cfx-scenario-description", scenario.Description)
                    .Attribute("data-cfx-scenario-step-count", scenario.Steps.Count)
                    .Attribute("data-cfx-scenario-playback-delay", scenario.PlaybackDelayMilliseconds)
                    .Attribute("data-cfx-scenario-loop", scenario.LoopPlayback)
                    .Attribute("data-cfx-scenario-autoplay", scenario.AutoPlay)
                    .Attribute("data-cfx-scenario-focus-mode", scenario.Spotlight ? "spotlight" : "highlight")
                    .Attribute("data-cfx-scenario-steps", TopologyScenarioJson.Steps(scenario, chart))
                    .Attribute("data-cfx-scenario-metadata", TopologyScenarioJson.Metadata(scenario))
                    .Attribute("title", string.IsNullOrWhiteSpace(scenario.Description) ? scenario.Label : scenario.Description)
                    .Attribute("aria-label", scenario.Label)
                    .Attribute("checked", active ? "checked" : null)
                    .EndVoidElement();
                writer.StartElement("span").EndStartElement().Text(scenario.Label).EndElement();
                writer.EndElement();
            }

            writer.EndElement();
            return;
        }

        WriteButton(writer, "data-cfx-topology-scenario", string.Empty, messages["TopologyShowAll"], messages["ShowAllScenarios"], string.IsNullOrWhiteSpace(activeScenarioId), messages["All"]);
        foreach (var scenario in chart.Scenarios) {
            var active = string.Equals(activeScenarioId, scenario.Id, StringComparison.Ordinal);
            writer.StartElement("button")
                .Attribute("type", "button")
                .Attribute("data-cfx-topology-scenario", scenario.Id)
                .Attribute("data-cfx-scenario-color", scenario.Color)
                .Attribute("data-cfx-scenario-label", scenario.Label)
                .Attribute("data-cfx-scenario-description", scenario.Description)
                .Attribute("data-cfx-scenario-step-count", scenario.Steps.Count)
                .Attribute("data-cfx-scenario-playback-delay", scenario.PlaybackDelayMilliseconds)
                .Attribute("data-cfx-scenario-loop", scenario.LoopPlayback)
                .Attribute("data-cfx-scenario-autoplay", scenario.AutoPlay)
                .Attribute("data-cfx-scenario-focus-mode", scenario.Spotlight ? "spotlight" : "highlight")
                .Attribute("data-cfx-scenario-steps", TopologyScenarioJson.Steps(scenario, chart))
                .Attribute("data-cfx-scenario-metadata", TopologyScenarioJson.Metadata(scenario))
                .Attribute("title", string.IsNullOrWhiteSpace(scenario.Description) ? scenario.Label : scenario.Description)
                .Attribute("aria-label", scenario.Label)
                .Attribute("aria-pressed", active)
                .Attribute("style", string.IsNullOrWhiteSpace(scenario.Color) ? null : "--cfx-topology-scenario-color:" + scenario.Color!.Trim())
                .EndStartElement()
                .Text(scenario.Label)
                .EndElement();
        }

        writer.EndElement();
    }

    private static void WriteScenarioPanel(HtmlMarkupWriter writer, string panelClass, TopologyChart chart, string? activeScenarioId, bool enableScenarioUrlState, TopologyHtmlText messages) {
        TopologyScenario? activeScenario = null;
        foreach (var scenario in chart.Scenarios) {
            if (!string.Equals(scenario.Id, activeScenarioId, StringComparison.Ordinal)) continue;
            activeScenario = scenario;
            break;
        }

        var title = activeScenario == null ? messages["All"] : activeScenario.Label;
        var meta = activeScenario == null
            ? messages["TopologyAllRoutesVisible"]
            : string.IsNullOrWhiteSpace(activeScenario.Description) ? messages.Format("Steps", ("count", activeScenario.Steps.Count.ToString("0", System.Globalization.CultureInfo.InvariantCulture))) : activeScenario.Description!;
        writer.StartElement("section")
            .Attribute("class", panelClass)
            .Attribute("data-cfx-topology-scenario-panel", "true")
            .Attribute("data-cfx-panel-active-scenario", string.IsNullOrWhiteSpace(activeScenarioId) ? null : activeScenarioId)
            .Attribute("aria-live", "polite")
            .Attribute("style", activeScenario == null || string.IsNullOrWhiteSpace(activeScenario.Color) ? null : "--cfx-topology-scenario-color:" + activeScenario.Color!.Trim())
            .EndStartElement();
        writer.StartElement("div").Attribute("class", panelClass + "__summary").EndStartElement();
        writer.StartElement("div").Attribute("class", panelClass + "__title").Attribute("data-cfx-scenario-panel-title", "true").EndStartElement().Text(title).EndElement();
        writer.StartElement("div").Attribute("class", panelClass + "__meta").Attribute("data-cfx-scenario-panel-meta", "true").EndStartElement().Text(meta).EndElement();
        writer.EndElement();
        writer.StartElement("div").Attribute("class", panelClass + "__controls").Attribute("data-cfx-scenario-step-controls", "true").EndStartElement();
        WriteScenarioPanelButton(writer, "previous", messages["PreviousStep"], messages["Previous"]);
        WriteScenarioPanelButton(writer, "play", messages["TopologyPlayRoute"], messages["Play"], pressed: false);
        WriteScenarioPanelButton(writer, "next", messages["NextStep"], messages["Next"]);
        WriteScenarioPanelButton(writer, "reset", messages["ClearStep"], messages["Clear"]);
        if (enableScenarioUrlState) WriteScenarioPanelButton(writer, "link", messages["TopologyCopyRouteLink"], messages["Link"]);
        writer.EndElement();
        writer.StartElement("input")
            .Attribute("class", panelClass + "__scrubber")
            .Attribute("type", "range")
            .Attribute("min", "1")
            .Attribute("max", activeScenario == null ? "1" : Math.Max(1, activeScenario.Steps.Count).ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Attribute("value", "1")
            .Attribute("step", "1")
            .Attribute("data-cfx-scenario-scrubber", "true")
            .Attribute("aria-label", messages["TopologyTimelineStep"])
            .BooleanAttribute("disabled", activeScenario == null || activeScenario.Steps.Count == 0)
            .EndVoidElement();
        writer.StartElement("div").Attribute("class", panelClass + "__payload").Attribute("data-cfx-scenario-payload", "true").BooleanAttribute("hidden", true).EndStartElement();
        writer.StartElement("div").Attribute("class", panelClass + "__payload-route").Attribute("data-cfx-scenario-payload-route", "true").EndStartElement().EndElement();
        writer.StartElement("pre").Attribute("class", panelClass + "__payload-body").Attribute("data-cfx-scenario-payload-body", "true").EndStartElement().EndElement();
        writer.EndElement();
        writer.StartElement("ol").Attribute("class", panelClass + "__steps").Attribute("data-cfx-scenario-panel-steps", "true").EndStartElement();
        if (activeScenario != null) {
            for (var index = 0; index < activeScenario.Steps.Count; index++) {
                var step = activeScenario.Steps[index];
                writer.StartElement("li")
                    .Attribute("data-cfx-scenario-step-kind", step.Kind.ToString())
                    .Attribute("data-cfx-scenario-step-id", step.Id)
                    .Attribute("data-cfx-scenario-step-index", index + 1)
                    .Attribute("data-cfx-scenario-step-duration", step.DurationMilliseconds.HasValue ? step.DurationMilliseconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : null)
                    .Attribute("role", "button")
                    .Attribute("tabindex", "0")
                    .EndStartElement()
                    .Text(string.IsNullOrWhiteSpace(step.Label) ? step.Id : step.Label!)
                    .EndElement();
            }
        }

        writer.EndElement();
        writer.EndElement();
    }

    private sealed class ScenarioSequenceFigure {
        public ScenarioSequenceFigure(string id, string label, string svg, double width) { Id = id; Label = label; Svg = svg; Width = width; }
        public string Id { get; }
        public string Label { get; }
        public string Svg { get; }
        public double Width { get; }
    }

    private static List<ScenarioSequenceFigure> ScenarioSequenceFigures(TopologyChart chart, string chartId) {
        var figures = new List<ScenarioSequenceFigure>();
        for (var index = 0; index < chart.Scenarios.Count; index++) {
            var scenario = chart.Scenarios[index];
            if (TopologyScenarioResolver.TryResolve(chart, scenario) is not { } resolved || resolved.Steps.All(step => step.TargetNodeId == null)) continue;
            var prefix = "cfx-seq-" + SafeIdPart(chartId) + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var prepared = VisualArtifacts.SequencePreparedCompiler.PrepareDefault(chart.ToSequenceArtifact(scenario.Id));
            figures.Add(new ScenarioSequenceFigure(scenario.Id, scenario.Label, prepared.ToSvg(prefix), prepared.Size.Width));
        }

        return figures;
    }

    private static void WriteScenarioSequences(HtmlMarkupWriter writer, string sequencesClass, IReadOnlyList<ScenarioSequenceFigure> figures, string? activeScenarioId, bool keepTextSize) {
        writer.StartElement("div").Attribute("class", sequencesClass).Attribute("data-cfx-scenario-sequences", "true").EndStartElement();
        foreach (var figure in figures) {
            writer.StartElement("figure")
                .Attribute("data-cfx-scenario-sequence", figure.Id)
                .Attribute("aria-label", figure.Label)
                // Like the topology above it, a sequence keeps its type scale: it shrinks at most an eighth, then scrolls in its frame.
                .Attribute("style", keepTextSize ? "--cfx-sequence-min-width:" + (figure.Width * MinimumTextScale).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "px" : null)
                .BooleanAttribute("hidden", !string.Equals(activeScenarioId, figure.Id, StringComparison.Ordinal))
                .EndStartElement()
                .RawTrusted(figure.Svg)
                .EndElement();
        }

        writer.EndElement();
    }
    private static string SafeIdPart(string value) {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var ch in value) builder.Append(ch < 128 && char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
        return builder.ToString();
    }

    private static void WriteScenarioPanelButton(HtmlMarkupWriter writer, string action, string title, string text, bool? pressed = null) {
        writer.StartElement("button")
            .Attribute("type", "button")
            .Attribute("data-cfx-scenario-step-control", action)
            .Attribute("title", title)
            .Attribute("aria-label", title)
            .Attribute("aria-pressed", pressed)
            .EndStartElement()
            .Text(text)
            .EndElement();
    }

    private static string? ResolveActiveScenarioId(TopologyChart chart, string? activeScenarioId) {
        if (chart.Scenarios.Count == 0) return null;
        if (!string.IsNullOrWhiteSpace(activeScenarioId)) {
            var requested = TopologyScenarioSelection.ResolveActiveScenarioId(chart, activeScenarioId);
            if (!string.IsNullOrWhiteSpace(requested)) return requested;
        }

        return null;
    }
}
