using System;
using System.Globalization;
using ChartForgeX.Html;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    // One section shell for genuine native prepared scenes. Producers own their typed facts and native layout.
    internal sealed class PreparedSceneSectionSource {
        internal PreparedVisual Drawing { get; set; } = null!;
        internal string Title { get; set; } = string.Empty;
        internal string? Language { get; set; }
        internal string Metadata { get; set; } = string.Empty;
        internal string? LocalizationJson { get; set; }
        internal string AssetSource { get; set; } = "host";
        internal string ChartId { get; set; } = string.Empty;
        internal VisualRenderContext Context { get; set; } = null!;
        internal double IntrinsicWidth { get; set; }
        internal bool Narrow { get; set; }
        internal string ThemeTokens { get; set; } = string.Empty;
        internal VisualSvgOptions SvgOptions { get; set; } = null!;
        internal string NativeNavigation { get; set; } = string.Empty;
        internal string HierarchyNavigation { get; set; } = string.Empty;
        internal Func<HtmlChartSection>? PrepareNarrow { get; set; }
    }

    internal static HtmlChartSection BuildPreparedSceneSection(PreparedSceneSectionSource source, HtmlChartInteractionOptions options) {
        var prepared = source.Drawing;
        var svgOptions = source.SvgOptions.WithTextPresentation(HtmlCapturedFontAssets.Resolve(options.TextPresentation));
        var emission = (options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Export)
            ? prepared.ForInteractivePngExport() : prepared).ExportSvgEmission(svgOptions);
        var adaptive = options.ResponsiveLayout == HtmlChartResponsiveLayout.Adaptive;
        var scenarioControls = options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.Scenarios) && options.Interaction.Scenarios.Count > 0;
        var writer = new HtmlMarkupWriter();
        writer.StartElement("section")
            .Attribute("class", "cfx-interactive-chart")
            .Attribute("data-cfx-look", "graphite")
            .Attribute("role", "group")
            .Attribute("aria-label", source.Title)
            .Attribute("lang", options.Localization.ResolveLanguage(source.Language))
            .Attribute("dir", options.Localization.DirectionAttribute)
            .Attribute("data-cfx-ui", source.LocalizationJson ?? options.Localization.ToJson())
            .Attribute("data-cfx-label-value", options.Localization[HtmlChartText.Value])
            .Attribute("data-cfx-asset-source", source.AssetSource)
            .Attribute("data-cfx-chart-id", source.ChartId)
            .Attribute("data-cfx-prepared-chart", source.Metadata)
            .Attribute("data-cfx-interaction-features", options.Interaction.Features.ToString())
            .Attribute("data-cfx-interaction-group", options.Interaction.GroupName)
            .Attribute("data-cfx-scenario-count", options.Interaction.Scenarios.Count)
            .Attribute("data-cfx-active-scenario", options.Interaction.ActiveScenarioId)
            .Attribute("data-cfx-deep-link-state", scenarioControls && options.Interaction.EnableDeepLinkState)
            .Attribute("data-cfx-scenario-playback", scenarioControls && options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.StepPlayback) ? "idle" : null)
            .Attribute("data-cfx-scenario-playback-delay", scenarioControls && options.Interaction.HasFeature(ChartForgeX.Interactivity.ChartInteractionFeatures.StepPlayback) ? "900" : null)
            .Attribute("data-cfx-responsive-layout", options.ResponsiveLayout.ToString().ToLowerInvariant())
            .Attribute("data-cfx-adaptive-breakpoint", adaptive ? options.Adaptive.Breakpoint.ToString(CultureInfo.InvariantCulture) : null)
            .Attribute("data-cfx-adaptive-drawing", adaptive ? source.Narrow ? "narrow" : "wide" : null)
            .Attribute("data-cfx-toolbar", options.Toolbar.ToString().ToLowerInvariant())
            .Attribute("data-cfx-context-menu", options.IncludeContextMenu ? "true" : null)
            .Attribute("data-cfx-progress-editing", options.ProgressEditing.Enabled ? "true" : null)
            .Attribute("data-cfx-progress-step-ratio", options.ProgressEditing.Enabled ? options.ProgressEditing.StepRatio.ToString("R", CultureInfo.InvariantCulture) : null)
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
            .Attribute("style", "--cfx-intrinsic-width:" + source.IntrinsicWidth.ToString(CultureInfo.InvariantCulture)
                + "px;--cfx-native-width:" + source.Context.Layout.Size.Width.ToString(CultureInfo.InvariantCulture)
                + "px;--cfx-native-height:" + source.Context.Layout.Size.Height.ToString(CultureInfo.InvariantCulture)
                + "px" + source.ThemeTokens + CrosshairInteractionTokens(options.Crosshair));
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
            .RawTrusted(emission.Svg).Line()
            .StartElement("div").Attribute("class", "cfx-brush-box").BooleanAttribute("hidden").EndStartElement().EndElement().Line()
            .StartElement("div").Attribute("class", "cfx-crosshair").BooleanAttribute("hidden").EndStartElement().Line()
            .StartElement("span").Attribute("class", "cfx-crosshair__line cfx-crosshair__line--x").EndStartElement().EndElement()
            .StartElement("span").Attribute("class", "cfx-crosshair__line cfx-crosshair__line--y").EndStartElement().EndElement()
            .StartElement("span").Attribute("class", "cfx-crosshair__label").Attribute("data-cfx-crosshair-label", "true").EndStartElement().EndElement()
            .EndElement().Line()
            .RawTrusted(BuildRevealLayer(options))
            .EndElement().Line()
            .RawTrusted(BuildResetControl(options))
            .RawTrusted(source.NativeNavigation)
            .RawTrusted(source.HierarchyNavigation)
            .RawTrusted(BuildCompareTray(options))
            .EndElement().Line()
            .StartElement("div").Attribute("class", "cfx-tooltip").Attribute("data-cfx-pinned-label", options.Localization[HtmlChartText.Pinned])
            .Attribute("role", "status").Attribute("aria-live", "polite").BooleanAttribute("hidden").EndStartElement().EndElement().Line();
        HtmlChartSection? narrowSection = null;
        if (source.PrepareNarrow != null) {
            narrowSection = source.PrepareNarrow();
            writer.StartElement("template").Attribute("data-cfx-adaptive-narrow", "true").EndStartElement()
                .RawTrusted(narrowSection.Html)
                .EndElement().Line();
        }
        var fontPayloads = !source.Narrow ? HtmlCapturedFontAssets.Write(writer, emission.Fonts, narrowSection?.WideEmission?.Fonts) : null;
        writer.EndElement();
        return new HtmlChartSection(writer.Build(), prepared, narrowSection?.PreparedVisual, svgOptions, emission, narrowSection?.WideEmission, fontPayloads);
    }

}
