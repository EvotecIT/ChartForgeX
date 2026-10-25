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
    /// <param name="viewport">Optional explicit X, primary Y or XY windows for supported vertical Cartesian charts.
    /// Null prepares the full domain, including when resetting a previous viewport. An omitted axis retains its full-data domain;
    /// primary Y windows require no secondary-axis series.</param>
    /// <param name="configure">An optional callback configuring the HTML interaction adapter.</param>
    /// <returns>The section markup and the prepared scene used to render it.</returns>
    /// <remarks>The host owns native re-preparation after a browser viewport request. Use the returned
    /// PreparedVisual.XProjection and eligible YProjection to convert logical gesture positions through their ConstrainViewport methods,
    /// then prepare the next section. Unsupported and empty charts have no native projections.
    /// An enabled native navigator prepares an additional full-domain scene for a windowed section.</remarks>
    /// <exception cref="ArgumentNullException">The chart is null.</exception>
    /// <exception cref="NotSupportedException">An explicit viewport is requested for an unsupported chart.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An explicit window extends outside its full domain or cannot be mapped.</exception>
    /// <exception cref="InvalidOperationException">An explicit viewport is requested without observations or a usable plot.</exception>
    public HtmlChartSection PrepareSection(Chart chart, ChartCartesianViewport? viewport = null, Action<HtmlChartInteractionOptions>? configure = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new HtmlChartInteractionOptions();
        configure?.Invoke(options);
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
        VisualRenderContext? drawingContext = null, bool narrow = false, PreparedVisual? preparedDrawing = null, PreparedVisual? fullDrawing = null, PreparedVisual? narrowDrawing = null, PreparedVisual? fullNarrowDrawing = null, ChartPolarViewport? polarViewport = null, bool requirePolar = false, ChartVisibilityView? visibility = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (options == null) throw new ArgumentNullException(nameof(options));
        var identityScope = options.IdScope ?? options.Interaction.ChartId ?? Slugify(ChartTitle(chart, titleFallback));
        var scope = narrow ? identityScope + "-narrow" : identityScope;
        var chartId = options.Interaction.ChartId ?? identityScope;
        var request = VisualExportRequest.ForChart(chart);
        // The authored request remains the intrinsic host basis when a narrow drawing becomes active.
        var context = (drawingContext ?? request.Context).WithNativeValueSource(options.CaptureNativeValueSource);
        var adaptive = options.ResponsiveLayout == HtmlChartResponsiveLayout.Adaptive;
        if (adaptive) ValidateAdaptiveOptions(options.Adaptive);
        if (adaptive && !narrow && preparedDrawing == null) {
            if (requirePolar) {
                preparedDrawing = chart.PreparePolar(context, polarViewport);
                narrowDrawing = chart.PreparePolar(AdaptiveDrawingContext(context, options.Adaptive), polarViewport);
            } else {
                var aligned = visibility == null
                    ? chart.PrepareAligned(context, AdaptiveDrawingContext(context, options.Adaptive), viewport,
                        retainFullDrawings: options.XNavigation.Enabled && options.XNavigation.IncludeNavigator)
                    : chart.PrepareAlignedVisibility(context, AdaptiveDrawingContext(context, options.Adaptive), visibility, viewport,
                        retainFullDrawings: options.XNavigation.Enabled && options.XNavigation.IncludeNavigator);
                preparedDrawing = aligned.First; narrowDrawing = aligned.Second;
                fullDrawing = aligned.FullFirst; fullNarrowDrawing = aligned.FullSecond;
            }
        }
        var prepared = preparedDrawing ?? (requirePolar ? chart.PreparePolar(context, polarViewport) : visibility == null ? chart.Prepare(context, viewport) : chart.PrepareVisibility(context, visibility, viewport));
        if (options.CaptureNativeValueSource && !VisualValueCapture.TryRawSource(prepared, out _, out _))
            throw new NotSupportedException("Native raw value capture requires an authored Pie/Donut source and bounded captured formatting/font policy; ordinary preparation remains available.");
        if (!Enum.IsDefined(typeof(HtmlChartResponsiveLayout), options.ResponsiveLayout)) throw new ArgumentOutOfRangeException(nameof(options.ResponsiveLayout));
        if (!Enum.IsDefined(typeof(HtmlChartToolbarLayout), options.Toolbar)) throw new ArgumentOutOfRangeException(nameof(options.Toolbar));
        if (!Enum.IsDefined(typeof(HtmlChartTooltipMode), options.Tooltip.Mode)) throw new ArgumentOutOfRangeException(nameof(options.Tooltip.Mode));
        if (options.Tooltip.DelayMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(options.Tooltip.DelayMilliseconds));
        return BuildPreparedSceneSection(new PreparedSceneSectionSource {
            Drawing = prepared, Title = ChartTitle(chart, titleFallback), Language = chart.Accessibility.Language,
            Metadata = PreparedChartMetadata(chart, prepared), AssetSource = assetSource, ChartId = chartId,
            Context = context, IntrinsicWidth = request.Context.Layout.Size.Width, Narrow = narrow,
            ThemeTokens = GraphiteInteractionTokens(chart),
            SvgOptions = new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(scope), chart.Options.SvgColorVariables),
            NativeNavigation = BuildXNavigation(chart, options, prepared, context, viewport, scope, fullDrawing),
            HierarchyNavigation = BuildHierarchyNavigation(options, prepared),
            PrepareNarrow = adaptive && !narrow ? () => PrepareChartSection(chart, options, titleFallback, assetSource, viewport,
                AdaptiveDrawingContext(context, options.Adaptive), true, narrowDrawing, fullNarrowDrawing,
                polarViewport: polarViewport, requirePolar: requirePolar, visibility: visibility) : null
        }, options);
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
