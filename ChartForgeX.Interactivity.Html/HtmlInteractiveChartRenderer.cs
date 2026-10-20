using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Renders a ChartForgeX chart as a self-contained interactive HTML document.
/// </summary>
public sealed partial class HtmlInteractiveChartRenderer {
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
        var surfaceStyle = HtmlInteractivePage.ChartSurfaceStyle(chart);
        var writer = HtmlInteractivePage.StartDocument(title, options.ExternalAssets, surfaceStyle,
            options.Localization.ResolveLanguage(chart.Accessibility.Language), options.Localization.DirectionAttribute);
        writer.StartElement("main").Attribute("class", "cfx-shell").Attribute("style", surfaceStyle).EndStartElement().Line()
            .RawTrusted(BuildChartSection(chart, options, title, "document")).Line()
            .EndElement().Line();
        HtmlInteractivePage.EndDocument(writer, options.ScriptNonce, options.ExternalAssets);
        return writer.Build();
    }

    /// <summary>
    /// Renders the specified chart to a self-contained embeddable interactive HTML fragment.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>An embeddable interactive HTML fragment.</returns>
    public string RenderFragment(Chart chart) => RenderFragment(chart, null);

    /// <summary>
    /// Renders the specified chart to a self-contained embeddable interactive HTML fragment.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="configure">An optional configuration callback for the HTML interaction adapter.</param>
    /// <returns>An embeddable interactive HTML fragment.</returns>
    public string RenderFragment(Chart chart, Action<HtmlChartInteractionOptions>? configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new HtmlChartInteractionOptions();
        configure?.Invoke(options);
        var title = options.PageTitle ?? ChartTitle(chart, "ChartForgeX interactive chart");
        var writer = new HtmlMarkupWriter();
        writer.StartElement("style")
            .Attribute("data-cfx-interactive-assets", "true")
            .RawTrusted(InteractiveFragmentStyle)
            .EndElement()
            .Line()
            .RawTrusted(BuildChartSection(chart, options, title, "inline"))
            .Line()
            .StartElement("script")
            .Attribute("nonce", options.ScriptNonce)
            .RawTrusted(HtmlInteractiveAssets.Script)
            .EndElement();
        return writer.Build();
    }

    /// <summary>
    /// Renders the specified chart to an embeddable interactive HTML fragment without stylesheet or script assets.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>An embeddable interactive HTML fragment that expects the caller to register interactive chart assets.</returns>
    public string RenderFragmentWithoutAssets(Chart chart) => RenderFragmentWithoutAssets(chart, null);

    /// <summary>
    /// Renders the specified chart to an embeddable interactive HTML fragment without stylesheet or script assets.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="configure">An optional configuration callback for the HTML interaction adapter.</param>
    /// <returns>An embeddable interactive HTML fragment that expects the caller to register interactive chart assets.</returns>
    public string RenderFragmentWithoutAssets(Chart chart, Action<HtmlChartInteractionOptions>? configure) {
        return PrepareSection(chart, configure: configure).Html;
    }

    /// <summary>Prepares an asset-free interactive section and its matching detached native scene.</summary>
    /// <param name="chart">The chart to prepare.</param>
    /// <param name="viewport">An optional explicit numeric or time X window for supported vertical point, financial and paired-boundary charts.
    /// Null prepares the full domain, including when resetting a previous viewport.</param>
    /// <param name="configure">An optional callback configuring the HTML interaction adapter.</param>
    /// <returns>The section markup and the prepared scene used to render it.</returns>
    /// <remarks>The host owns native re-preparation after a browser viewport request. Use the returned
    /// PreparedVisual.XProjection to convert logical gesture positions through its ConstrainViewport method,
    /// then prepare the next section. Unsupported and empty charts have no X projection.
    /// An enabled native navigator prepares an additional full-domain scene for a windowed section.</remarks>
    /// <exception cref="ArgumentNullException">The chart is null.</exception>
    /// <exception cref="NotSupportedException">An explicit viewport is requested for an unsupported chart.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An explicit viewport extends outside the full X domain or cannot be mapped.</exception>
    /// <exception cref="InvalidOperationException">An explicit viewport is requested without observations or a usable plot.</exception>
    public HtmlChartSection PrepareSection(Chart chart, ChartXViewport? viewport = null, Action<HtmlChartInteractionOptions>? configure = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new HtmlChartInteractionOptions();
        configure?.Invoke(options);
        var title = options.PageTitle ?? ChartTitle(chart, "ChartForgeX interactive chart");
        return PrepareChartSection(chart, options, title, "host", viewport == null ? null : new ChartCartesianViewport(x: viewport));
    }

    /// <summary>Prepares an interactive section within explicit native X, primary Y or XY source windows.</summary>
    /// <param name="chart">The mutable source chart, which preparation does not modify.</param>
    /// <param name="viewport">The explicit source windows.</param>
    /// <param name="configure">Optional HTML adapter policy.</param>
    /// <returns>The section and its matching detached native projections.</returns>
    public HtmlChartSection PrepareSection(Chart chart, ChartCartesianViewport viewport, Action<HtmlChartInteractionOptions>? configure = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (viewport == null) throw new ArgumentNullException(nameof(viewport));
        var options = new HtmlChartInteractionOptions(); configure?.Invoke(options);
        var title = options.PageTitle ?? ChartTitle(chart, "ChartForgeX interactive chart");
        return PrepareChartSection(chart, options, title, "host", viewport);
    }

    /// <summary>
    /// Builds the stylesheet used by embeddable interactive chart fragments.
    /// </summary>
    /// <returns>CSS ready to register once in a host document.</returns>
    public static string BuildFragmentStyle() => InteractiveFragmentStyle;

    /// <summary>
    /// Builds the JavaScript runtime used by embeddable interactive chart fragments.
    /// </summary>
    /// <returns>JavaScript ready to register once in a host document.</returns>
    public static string BuildInteractionScript() => InteractiveScript;

    internal static string InteractiveStyle => HtmlInteractiveAssets.Style;

    internal static string InteractiveFragmentStyle => HtmlInteractiveAssets.Style
        .Replace(":root {", ".cfx-interactive-chart {")
        .Replace("\n* {", "\n.cfx-interactive-chart, .cfx-interactive-chart * {")
        .Replace("\nbody {", "\n.cfx-interactive-chart {");

    internal static string InteractiveScript => HtmlInteractiveAssets.Script;

    internal static string BuildChartSection(Chart chart, HtmlChartInteractionOptions options, string titleFallback, string assetSource) =>
        PrepareChartSection(chart, options, titleFallback, assetSource, null).Html;

    private static HtmlChartSection PrepareChartSection(Chart chart, HtmlChartInteractionOptions options, string titleFallback, string assetSource, ChartCartesianViewport? viewport,
        VisualRenderContext? drawingContext = null, bool narrow = false, PreparedVisual? preparedDrawing = null, PreparedVisual? fullDrawing = null, PreparedVisual? narrowDrawing = null, PreparedVisual? fullNarrowDrawing = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (options == null) throw new ArgumentNullException(nameof(options));
        var identityScope = options.IdScope ?? options.Interaction.ChartId ?? Slugify(ChartTitle(chart, titleFallback));
        var scope = narrow ? identityScope + "-narrow" : identityScope;
        var chartId = options.Interaction.ChartId ?? identityScope;
        var request = VisualExportRequest.ForChart(chart);
        // The authored request remains the intrinsic host basis when a narrow drawing becomes active.
        var context = drawingContext ?? request.Context;
        var adaptive = options.ResponsiveLayout == HtmlChartResponsiveLayout.Adaptive;
        if (adaptive) ValidateAdaptiveOptions(options.Adaptive);
        if (adaptive && !narrow && preparedDrawing == null) {
            var aligned = chart.PrepareAligned(context, AdaptiveDrawingContext(context, options.Adaptive), viewport,
                retainFullDrawings: options.XNavigation.Enabled && options.XNavigation.IncludeNavigator);
            preparedDrawing = aligned.First; narrowDrawing = aligned.Second;
            fullDrawing = aligned.FullFirst; fullNarrowDrawing = aligned.FullSecond;
        }
        var prepared = preparedDrawing ?? (viewport == null ? chart.Prepare(context) : chart.Prepare(context, viewport));
        var scenarioControls = options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Scenarios) && options.Interaction.Scenarios.Count > 0;
        if (!Enum.IsDefined(typeof(HtmlChartResponsiveLayout), options.ResponsiveLayout)) throw new ArgumentOutOfRangeException(nameof(options.ResponsiveLayout));
        if (!Enum.IsDefined(typeof(HtmlChartToolbarLayout), options.Toolbar)) throw new ArgumentOutOfRangeException(nameof(options.Toolbar));
        if (!Enum.IsDefined(typeof(HtmlChartTooltipMode), options.Tooltip.Mode)) throw new ArgumentOutOfRangeException(nameof(options.Tooltip.Mode));
        if (options.Tooltip.DelayMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(options.Tooltip.DelayMilliseconds));
        var writer = new HtmlMarkupWriter();
        writer.StartElement("section")
            .Attribute("class", "cfx-interactive-chart")
            .Attribute("data-cfx-look", "graphite")
            .Attribute("role", "group")
            .Attribute("aria-label", ChartTitle(chart, titleFallback))
            .Attribute("lang", options.Localization.ResolveLanguage(chart.Accessibility.Language))
            .Attribute("dir", options.Localization.DirectionAttribute)
            .Attribute("data-cfx-ui", options.Localization.ToJson())
            .Attribute("data-cfx-label-value", options.Localization[HtmlChartText.Value])
            .Attribute("data-cfx-asset-source", assetSource)
            .Attribute("data-cfx-chart-id", chartId)
            .Attribute("data-cfx-prepared-chart", PreparedChartMetadata(chart, prepared))
            .Attribute("data-cfx-interaction-features", options.Interaction.Features.ToString())
            .Attribute("data-cfx-interaction-group", options.Interaction.GroupName)
            .Attribute("data-cfx-scenario-count", options.Interaction.Scenarios.Count)
            .Attribute("data-cfx-active-scenario", options.Interaction.ActiveScenarioId)
            .Attribute("data-cfx-deep-link-state", scenarioControls && options.Interaction.EnableDeepLinkState)
            .Attribute("data-cfx-scenario-playback", scenarioControls && options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.StepPlayback) ? "idle" : null)
            .Attribute("data-cfx-scenario-playback-delay", scenarioControls && options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.StepPlayback) ? "900" : null)
            .Attribute("data-cfx-responsive-layout", options.ResponsiveLayout.ToString().ToLowerInvariant())
            .Attribute("data-cfx-adaptive-breakpoint", adaptive ? options.Adaptive.Breakpoint.ToString(CultureInfo.InvariantCulture) : null)
            .Attribute("data-cfx-adaptive-drawing", adaptive ? narrow ? "narrow" : "wide" : null)
            .Attribute("data-cfx-toolbar", options.Toolbar.ToString().ToLowerInvariant())
            .Attribute("data-cfx-context-menu", options.IncludeContextMenu ? "true" : null)
            .Attribute("data-cfx-selection-mode", options.Interaction.SelectionMode.ToString().ToLowerInvariant())
            .Attribute("data-cfx-brush-axis", options.Interaction.BrushAxis.ToString().ToLowerInvariant())
            .Attribute("data-cfx-dim-others-selected", options.Highlight.DimOthersWhileSelected ? "true" : "false")
            .Attribute("data-cfx-pin-on-select", options.Tooltip.PinOnSelect ? "true" : "false")
            .Attribute("data-cfx-tooltip-mode", options.Tooltip.Mode == HtmlChartTooltipMode.Single ? "single" : "shared-x")
            .Attribute("data-cfx-tooltip-range", options.Tooltip.Range.SerializedKind)
            .Attribute("data-cfx-tooltip-distance", options.Tooltip.Range.CssPixels?.ToString("R", CultureInfo.InvariantCulture))
            .Attribute("data-cfx-tooltip-delay", options.Tooltip.DelayMilliseconds.ToString(CultureInfo.InvariantCulture))
            .Attribute("data-cfx-tooltip-anchor", options.Tooltip.Position.Anchor.ToString().ToLowerInvariant())
            .Attribute("data-cfx-tooltip-placements", options.Tooltip.Position.SerializedPlacements)
            .Attribute("data-cfx-tooltip-gap", options.Tooltip.Position.Gap.ToString("R", CultureInfo.InvariantCulture))
            .Attribute("data-cfx-tooltip-offset-x", options.Tooltip.Position.OffsetX.ToString("R", CultureInfo.InvariantCulture))
            .Attribute("data-cfx-tooltip-offset-y", options.Tooltip.Position.OffsetY.ToString("R", CultureInfo.InvariantCulture))
            .Attribute("data-cfx-crosshair-label", options.Crosshair.ShowLabel ? "true" : "false")
            .Attribute("data-cfx-crosshair-styled", options.Crosshair.HasLineOverrides ? "true" : null)
            .Attribute("style", "--cfx-intrinsic-width:" + request.Context.Layout.Size.Width.ToString(CultureInfo.InvariantCulture)
                + "px;--cfx-native-width:" + context.Layout.Size.Width.ToString(CultureInfo.InvariantCulture)
                + "px;--cfx-native-height:" + context.Layout.Size.Height.ToString(CultureInfo.InvariantCulture)
                + "px" + GraphiteInteractionTokens(chart) + CrosshairInteractionTokens(options.Crosshair));
        WriteHighlightOptions(writer, options.Highlight);
        writer.EndStartElement().Line();
        var toolbar = BuildToolbar(options);
        if (toolbar.Length > 0) {
            writer.StartElement("div").Attribute("class", "cfx-toolbar").Attribute("aria-label", options.Localization[HtmlChartText.ChartControls]).EndStartElement()
                .RawTrusted(toolbar)
                .EndElement().Line();
        }

        if (scenarioControls) {
            writer.RawTrusted(BuildScenarioControls(options));
            writer.RawTrusted(BuildScenarioPanel(options));
        }

        writer.StartElement("div").Attribute("class", "cfx-frame").EndStartElement().Line()
            .StartElement("div").Attribute("class", "cfx-stage").EndStartElement().Line()
            .RawTrusted(prepared.ToSvg(new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(scope), chart.Options.SvgColorVariables))).Line()
            .StartElement("div").Attribute("class", "cfx-brush-box").BooleanAttribute("hidden").EndStartElement().EndElement().Line()
            .StartElement("div").Attribute("class", "cfx-crosshair").BooleanAttribute("hidden").EndStartElement().Line()
            .StartElement("span").Attribute("class", "cfx-crosshair__line cfx-crosshair__line--x").EndStartElement().EndElement()
            .StartElement("span").Attribute("class", "cfx-crosshair__line cfx-crosshair__line--y").EndStartElement().EndElement()
            .StartElement("span").Attribute("class", "cfx-crosshair__label").Attribute("data-cfx-crosshair-label", "true").EndStartElement().EndElement()
            .EndElement().Line()
            .RawTrusted(BuildRevealLayer(options))
            .EndElement().Line()
            .RawTrusted(BuildResetControl(options))
            .RawTrusted(BuildXNavigation(chart, options, prepared, context, viewport, scope, fullDrawing))
            .RawTrusted(BuildCompareTray(options))
            .EndElement().Line()
            .StartElement("div").Attribute("class", "cfx-tooltip").Attribute("data-cfx-pinned-label", options.Localization[HtmlChartText.Pinned])
            .Attribute("role", "status").Attribute("aria-live", "polite").BooleanAttribute("hidden").EndStartElement().EndElement().Line();
        HtmlChartSection? narrowSection = null;
        if (adaptive && !narrow) {
            var narrowContext = AdaptiveDrawingContext(context, options.Adaptive);
            narrowSection = PrepareChartSection(chart, options, titleFallback, assetSource, viewport, narrowContext, true, narrowDrawing, fullNarrowDrawing);
            writer.StartElement("template").Attribute("data-cfx-adaptive-narrow", "true").EndStartElement()
                .RawTrusted(narrowSection.Html)
                .EndElement().Line();
        }
        writer.EndElement();
        return new HtmlChartSection(writer.Build(), prepared, narrowSection?.PreparedVisual);
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
        writer.StartElement("div").Attribute("class", "cfx-scenarios").Attribute("aria-label", options.Localization[HtmlChartText.ChartScenarios]).EndStartElement();
        AppendScenarioButton(writer, string.Empty, options.Localization[HtmlChartText.All], options.Localization[HtmlChartText.ShowAllScenarios], string.IsNullOrWhiteSpace(options.Interaction.ActiveScenarioId), null, null, null, null, null);
        foreach (var scenario in options.Interaction.Scenarios) {
            var active = string.Equals(options.Interaction.ActiveScenarioId, scenario.Id, StringComparison.Ordinal);
            var title = string.IsNullOrWhiteSpace(scenario.Description) ? scenario.Label : scenario.Description!;
            AppendScenarioButton(writer, scenario.Id, scenario.Label, title, active, scenario.Color, scenario.Description, ScenarioStepsJson(scenario), ScenarioMetadataJson(scenario.Metadata), scenario);
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
            .StartElement("div").Attribute("class", "cfx-scenario-panel__title").Attribute("data-cfx-scenario-panel-title", "true").EndStartElement().Text(scenario?.Label ?? options.Localization[HtmlChartText.All]).EndElement()
            .StartElement("div").Attribute("class", "cfx-scenario-panel__meta").Attribute("data-cfx-scenario-panel-meta", "true").EndStartElement().Text(ScenarioSummary(scenario, options.Localization)).EndElement()
            .EndElement().Line();
        if (stepPlayback) {
            writer.StartElement("div").Attribute("class", "cfx-scenario-panel__controls").Attribute("data-cfx-scenario-step-controls", "true").EndStartElement();
            AppendToolbarButton(writer, options.Localization[HtmlChartText.Previous], ("data-cfx-scenario-step-control", "previous"), ("title", options.Localization[HtmlChartText.PreviousStep]));
            AppendToolbarButton(writer, options.Localization[HtmlChartText.Play], ("data-cfx-scenario-step-control", "play"), ("data-cfx-scenario-play-label", options.Localization[HtmlChartText.Play]), ("data-cfx-scenario-pause-label", options.Localization[HtmlChartText.Pause]), ("aria-pressed", "false"), ("title", options.Localization[HtmlChartText.PlayScenario]));
            AppendToolbarButton(writer, options.Localization[HtmlChartText.Next], ("data-cfx-scenario-step-control", "next"), ("title", options.Localization[HtmlChartText.NextStep]));
            AppendToolbarButton(writer, options.Localization[HtmlChartText.Clear], ("data-cfx-scenario-step-control", "reset"), ("title", options.Localization[HtmlChartText.ClearStep]));
            if (options.Interaction.EnableDeepLinkState) AppendToolbarButton(writer, options.Localization[HtmlChartText.Link], ("data-cfx-scenario-step-control", "link"), ("title", options.Localization[HtmlChartText.CopyScenarioLink]));
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
                .StartElement("span").Attribute("class", "cfx-scenario-progress__text").Attribute("data-cfx-scenario-progress-text", "true").EndStartElement().Text(options.Localization.Format(HtmlChartText.StepProgress, "current", 0).Replace("{count}", (scenario?.Steps.Count ?? 0).ToString(CultureInfo.InvariantCulture))).EndElement()
                .EndElement().Line();
            writer.StartElement("input")
                .Attribute("class", "cfx-scenario-scrubber")
                .Attribute("type", "range")
                .Attribute("min", "1")
                .Attribute("max", scenario == null ? "1" : Math.Max(1, scenario.Steps.Count).ToString(CultureInfo.InvariantCulture))
                .Attribute("value", "1")
                .Attribute("step", "1")
                .Attribute("data-cfx-scenario-scrubber", "true")
                .Attribute("aria-label", options.Localization[HtmlChartText.ScenarioTimelineStep])
                .BooleanAttribute("disabled", scenario == null || scenario.Steps.Count == 0)
                .EndVoidElement().Line();
        }

        writer.StartElement("ol").Attribute("class", "cfx-scenario-panel__steps").Attribute("data-cfx-scenario-panel-steps", "true").EndStartElement();
        if (stepPlayback && scenario != null) {
            var activeScenario = scenario;
            for (var i = 0; i < activeScenario.Steps.Count; i++) WriteScenarioStep(writer, activeScenario.Steps[i], i);
        }

        writer.EndElement().Line().EndElement().Line();
        return writer.Build();
    }

    private static void AppendScenarioButton(HtmlMarkupWriter writer, string id, string label, string title, bool active, string? color, string? description, string? stepsJson, string? metadataJson, ChartInteractionScenario? scenario) {
        writer.StartElement("button")
            .Attribute("class", "cfx-scenario")
            .Attribute("type", "button")
            .Attribute("data-cfx-scenario", id)
            .Attribute("data-cfx-scenario-color", color)
            .Attribute("data-cfx-scenario-label", label)
            .Attribute("data-cfx-scenario-description", description)
            .Attribute("data-cfx-scenario-steps", stepsJson)
            .Attribute("data-cfx-scenario-metadata", metadataJson)
            .Attribute("data-cfx-scenario-playback-delay", scenario == null ? null : scenario.PlaybackDelayMilliseconds.ToString(CultureInfo.InvariantCulture))
            .Attribute("data-cfx-scenario-loop", scenario?.LoopPlayback)
            .Attribute("data-cfx-scenario-autoplay", scenario?.AutoPlay)
            .Attribute("data-cfx-scenario-focus-mode", scenario?.FocusMode.ToString().ToLowerInvariant())
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
            .Attribute("data-cfx-scenario-step-duration", step.DurationMilliseconds.HasValue ? step.DurationMilliseconds.Value.ToString(CultureInfo.InvariantCulture) : null)
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

    private static string ScenarioSummary(ChartInteractionScenario? scenario, HtmlChartLocalization localization) {
        if (scenario == null) return localization[HtmlChartText.AllDataVisible];
        if (!string.IsNullOrWhiteSpace(scenario.Description)) return scenario.Description!;
        return localization.Format(HtmlChartText.Steps, "count", scenario.Steps.Count);
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
                "\",\"durationMilliseconds\":" + (step.DurationMilliseconds.HasValue ? step.DurationMilliseconds.Value.ToString(CultureInfo.InvariantCulture) : "null") +
                ",\"metadata\":" + ScenarioMetadataJson(step.Metadata) + "}");
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
            AppendToolbarButton(writer, options.Localization[HtmlChartText.ZoomInButton], ("data-cfx-zoom", "in"), ("title", options.Localization[HtmlChartText.ZoomIn]));
            AppendToolbarButton(writer, options.Localization[HtmlChartText.ZoomOutButton], ("data-cfx-zoom", "out"), ("title", options.Localization[HtmlChartText.ZoomOut]));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Pan)) {
            AppendToolbarButton(writer, options.Localization[HtmlChartText.Pan], ("data-cfx-mode-button", "pan"), ("aria-pressed", "false"), ("title", options.Localization[HtmlChartText.PanChart]));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Brush)) {
            AppendToolbarButton(writer, options.Localization[HtmlChartText.Brush], ("data-cfx-mode-button", "brush"), ("aria-pressed", "false"), ("title", options.Localization[HtmlChartText.BrushRegion]));
        }

        if (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Export)) {
            AppendToolbarButton(writer, "SVG", ("data-cfx-export", "svg"), ("title", options.Localization[HtmlChartText.DownloadSvg]));
            AppendToolbarButton(writer, "PNG", ("data-cfx-export", "png"), ("title", options.Localization[HtmlChartText.DownloadPng]));
        }

        return writer.Build();
    }

    /// <summary>
    /// Builds the contextual reset control. It starts hidden; the runtime reveals it only while the view
    /// differs from its initial state, so the chart carries no permanent control chrome.
    /// </summary>
    private static string BuildResetControl(HtmlChartInteractionOptions options) {
        if (!options.IncludeResetButton) return string.Empty;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("button")
            .Attribute("class", "cfx-reset")
            .Attribute("type", "button")
            .Attribute("data-cfx-reset", "true")
            .BooleanAttribute("hidden")
            .EndStartElement()
            .Text(options.Localization[HtmlChartText.ResetView])
            .EndElement()
            .Line();
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
