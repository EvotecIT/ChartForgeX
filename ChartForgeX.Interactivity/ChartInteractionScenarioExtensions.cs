using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Provides fluent helpers for host-neutral interaction scenarios.
/// </summary>
public static class ChartInteractionScenarioExtensions {
    /// <summary>
    /// Adds a named scenario to the interaction options and enables scenario interactivity.
    /// </summary>
    /// <param name="options">The interaction options.</param>
    /// <param name="id">The scenario id.</param>
    /// <param name="label">The scenario label.</param>
    /// <param name="configure">Optional scenario configuration.</param>
    /// <returns>The current interaction options.</returns>
    public static ChartInteractionOptions AddScenario(this ChartInteractionOptions options, string id, string label, Action<ChartInteractionScenario>? configure = null) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        var scenario = new ChartInteractionScenario {
            Id = id,
            Label = label
        };
        configure?.Invoke(scenario);
        options.Scenarios.Add(scenario);
        options.Enable(ChartInteractionFeatures.Scenarios);
        if (scenario.Steps.Count > 0) options.Enable(ChartInteractionFeatures.StepPlayback);
        return options;
    }

    /// <summary>
    /// Sets the active scenario id and enables scenario interactivity.
    /// </summary>
    /// <param name="options">The interaction options.</param>
    /// <param name="scenarioId">The active scenario id.</param>
    /// <returns>The current interaction options.</returns>
    public static ChartInteractionOptions WithActiveScenario(this ChartInteractionOptions options, string scenarioId) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.ActiveScenarioId = scenarioId;
        options.Enable(ChartInteractionFeatures.Scenarios);
        return options;
    }

    /// <summary>
    /// Clears the active scenario id.
    /// </summary>
    /// <param name="options">The interaction options.</param>
    /// <returns>The current interaction options.</returns>
    public static ChartInteractionOptions WithoutActiveScenario(this ChartInteractionOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.ActiveScenarioId = null;
        return options;
    }

    /// <summary>
    /// Controls whether adapter state may be synchronized with a deep link.
    /// </summary>
    /// <param name="options">The interaction options.</param>
    /// <param name="enabled">Whether deep-link state is enabled.</param>
    /// <returns>The current interaction options.</returns>
    public static ChartInteractionOptions WithDeepLinkState(this ChartInteractionOptions options, bool enabled = true) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        options.EnableDeepLinkState = enabled;
        if (enabled) options.Enable(ChartInteractionFeatures.DeepLinks);
        else options.Disable(ChartInteractionFeatures.DeepLinks);
        return options;
    }

    /// <summary>
    /// Sets an optional scenario accent color.
    /// </summary>
    /// <param name="scenario">The interaction scenario.</param>
    /// <param name="color">The scenario color.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario WithColor(this ChartInteractionScenario scenario, string? color) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        scenario.Color = color;
        return scenario;
    }

    /// <summary>
    /// Sets an optional scenario description.
    /// </summary>
    /// <param name="scenario">The interaction scenario.</param>
    /// <param name="description">The scenario description.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario WithDescription(this ChartInteractionScenario scenario, string? description) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        scenario.Description = description;
        return scenario;
    }

    /// <summary>
    /// Adds host-readable metadata to an interaction scenario.
    /// </summary>
    /// <param name="scenario">The interaction scenario.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario WithMetadata(this ChartInteractionScenario scenario, string key, string? value) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        scenario.Metadata[ChartInteractionText.RequiredText(key, nameof(key), "Interaction scenario metadata keys")] = value ?? string.Empty;
        return scenario;
    }

    /// <summary>
    /// Adds an ordered target reference to an interaction scenario.
    /// </summary>
    /// <param name="scenario">The interaction scenario.</param>
    /// <param name="targetKind">The adapter-defined target kind.</param>
    /// <param name="targetId">The adapter-defined target id.</param>
    /// <param name="label">The optional step label.</param>
    /// <param name="description">The optional step description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario AddStep(this ChartInteractionScenario scenario, string targetKind, string targetId, string? label = null, string? description = null, Action<ChartInteractionScenarioStep>? configure = null) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        var step = new ChartInteractionScenarioStep {
            TargetKind = targetKind,
            TargetId = targetId,
            Label = label,
            Description = description
        };
        configure?.Invoke(step);
        scenario.Steps.Add(step);
        return scenario;
    }

    /// <summary>
    /// Adds an ordered step that targets a rendered chart series.
    /// </summary>
    /// <param name="scenario">The interaction scenario.</param>
    /// <param name="seriesId">The adapter-defined series id or zero-based series index.</param>
    /// <param name="label">The optional step label.</param>
    /// <param name="description">The optional step description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario AddSeriesStep(this ChartInteractionScenario scenario, string seriesId, string? label = null, string? description = null, Action<ChartInteractionScenarioStep>? configure = null) {
        return AddStep(scenario, ChartInteractionTargetKinds.Series, seriesId, label, description, configure);
    }

    /// <summary>
    /// Adds an ordered step that targets a rendered chart point.
    /// </summary>
    /// <param name="scenario">The interaction scenario.</param>
    /// <param name="pointId">The adapter-defined point id.</param>
    /// <param name="label">The optional step label.</param>
    /// <param name="description">The optional step description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario AddPointStep(this ChartInteractionScenario scenario, string pointId, string? label = null, string? description = null, Action<ChartInteractionScenarioStep>? configure = null) {
        return AddStep(scenario, ChartInteractionTargetKinds.Point, pointId, label, description, configure);
    }

    /// <summary>
    /// Adds an ordered step that targets a rendered annotation or mark.
    /// </summary>
    /// <param name="scenario">The interaction scenario.</param>
    /// <param name="annotationId">The adapter-defined annotation id.</param>
    /// <param name="label">The optional step label.</param>
    /// <param name="description">The optional step description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario AddAnnotationStep(this ChartInteractionScenario scenario, string annotationId, string? label = null, string? description = null, Action<ChartInteractionScenarioStep>? configure = null) {
        return AddStep(scenario, ChartInteractionTargetKinds.Annotation, annotationId, label, description, configure);
    }

    /// <summary>
    /// Adds an ordered step that targets a rendered element by SVG id or adapter data id.
    /// </summary>
    /// <param name="scenario">The interaction scenario.</param>
    /// <param name="elementId">The rendered SVG id or adapter data id.</param>
    /// <param name="label">The optional step label.</param>
    /// <param name="description">The optional step description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario AddElementStep(this ChartInteractionScenario scenario, string elementId, string? label = null, string? description = null, Action<ChartInteractionScenarioStep>? configure = null) {
        return AddStep(scenario, ChartInteractionTargetKinds.Element, elementId, label, description, configure);
    }

    /// <summary>
    /// Adds an ordered step that targets a rendered element by role plus value or label.
    /// </summary>
    /// <param name="scenario">The interaction scenario.</param>
    /// <param name="role">The rendered element role.</param>
    /// <param name="valueOrLabel">The rendered element value or label.</param>
    /// <param name="label">The optional step label.</param>
    /// <param name="description">The optional step description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario AddRoleStep(this ChartInteractionScenario scenario, string role, string valueOrLabel, string? label = null, string? description = null, Action<ChartInteractionScenarioStep>? configure = null) {
        return AddStep(scenario, role, valueOrLabel, label, description, configure);
    }

    /// <summary>
    /// Adds host-readable metadata to an interaction scenario step.
    /// </summary>
    /// <param name="step">The interaction scenario step.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current scenario step.</returns>
    public static ChartInteractionScenarioStep WithMetadata(this ChartInteractionScenarioStep step, string key, string? value) {
        if (step == null) throw new ArgumentNullException(nameof(step));
        step.Metadata[ChartInteractionText.RequiredText(key, nameof(key), "Interaction scenario step metadata keys")] = value ?? string.Empty;
        return step;
    }
}
