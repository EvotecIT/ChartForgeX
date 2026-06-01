using System;
using ChartForgeX.Html;

namespace ChartForgeX.Topology;

public sealed partial class TopologyHtmlRenderer {
    private static void WriteScenarioControls(HtmlMarkupWriter writer, string scenariosClass, TopologyChart chart, string? activeScenarioId, TopologyHtmlScenarioControlMode mode) {
        writer.StartElement("div").Attribute("class", scenariosClass).Attribute("aria-label", "Topology scenarios").EndStartElement();
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
                    .Attribute("data-cfx-scenario-steps", TopologyScenarioJson.Steps(scenario))
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

        WriteButton(writer, "data-cfx-topology-scenario", string.Empty, "Show all", "Show all scenarios", string.IsNullOrWhiteSpace(activeScenarioId), "All");
        foreach (var scenario in chart.Scenarios) {
            var active = string.Equals(activeScenarioId, scenario.Id, StringComparison.Ordinal);
            writer.StartElement("button")
                .Attribute("type", "button")
                .Attribute("data-cfx-topology-scenario", scenario.Id)
                .Attribute("data-cfx-scenario-color", scenario.Color)
                .Attribute("data-cfx-scenario-label", scenario.Label)
                .Attribute("data-cfx-scenario-description", scenario.Description)
                .Attribute("data-cfx-scenario-step-count", scenario.Steps.Count)
                .Attribute("data-cfx-scenario-steps", TopologyScenarioJson.Steps(scenario))
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

    private static void WriteScenarioPanel(HtmlMarkupWriter writer, string panelClass, TopologyChart chart, string? activeScenarioId, bool enableScenarioUrlState) {
        TopologyScenario? activeScenario = null;
        foreach (var scenario in chart.Scenarios) {
            if (!string.Equals(scenario.Id, activeScenarioId, StringComparison.Ordinal)) continue;
            activeScenario = scenario;
            break;
        }

        var title = activeScenario == null ? "All" : activeScenario.Label;
        var meta = activeScenario == null
            ? "All topology routes visible"
            : string.IsNullOrWhiteSpace(activeScenario.Description) ? activeScenario.Steps.Count.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " steps" : activeScenario.Description!;
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
        WriteScenarioPanelButton(writer, "previous", "Previous step", "Prev");
        WriteScenarioPanelButton(writer, "play", "Play route", "Play", pressed: false);
        WriteScenarioPanelButton(writer, "next", "Next step", "Next");
        WriteScenarioPanelButton(writer, "reset", "Clear step", "Clear");
        if (enableScenarioUrlState) WriteScenarioPanelButton(writer, "link", "Copy route link", "Link");
        writer.EndElement();
        writer.StartElement("ol").Attribute("class", panelClass + "__steps").Attribute("data-cfx-scenario-panel-steps", "true").EndStartElement();
        if (activeScenario != null) {
            for (var index = 0; index < activeScenario.Steps.Count; index++) {
                var step = activeScenario.Steps[index];
                writer.StartElement("li")
                    .Attribute("data-cfx-scenario-step-kind", step.Kind.ToString())
                    .Attribute("data-cfx-scenario-step-id", step.Id)
                    .Attribute("data-cfx-scenario-step-index", index + 1)
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
