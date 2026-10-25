using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>Applies source presentation to the reusable model before either SVG or PNG rendering.</summary>
internal static class MermaidPresentation {
    internal static T Apply<T>(T model, MermaidDocument document, MermaidPresentationOptions? host = null) where T : class {
        host?.Validate();
        if (model is VisualArtifact artifact) {
            artifact.Accessibility.Name = document.Accessibility.Name;
            artifact.Accessibility.Description = document.Accessibility.Description;
            if (document.Configuration.Theme != null) artifact.Metadata["mermaid.config.theme"] = document.Configuration.Theme;
            if (document.Configuration.Layout != null) artifact.Metadata["mermaid.config.layout"] = document.Configuration.Layout;
            if (document.Configuration.Look != null) artifact.Metadata["mermaid.config.look"] = document.Configuration.Look;
            if (document.Configuration.FontFamily != null) artifact.Metadata["mermaid.config.fontFamily"] = document.Configuration.FontFamily;
            if (document.Configuration.FontSize.HasValue) artifact.Metadata["mermaid.config.fontSize"] = document.Configuration.FontSize.Value.ToString(CultureInfo.InvariantCulture);
            if (artifact.Model is TopologyChart topologyModel && topologyModel.DefaultRenderOptions?.FitContentToViewport == false)
                artifact.HtmlSizing = VisualArtifactHtmlSizing.PreserveSize;
            MermaidArtifactPolicy.Attach(artifact, document);

        } else if (model is SequenceArtifact sequence) {
            sequence.Accessibility.Name = document.Accessibility.Name;
            sequence.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) sequence.ThemeMode = string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? VisualThemeMode.Dark : VisualThemeMode.Light;
            if (host?.ThemeMode != null) sequence.ThemeMode = host.ThemeMode.Value;
            var preset=document.Configuration.Preset?.Resolve(sequence.ThemeMode,host?.Theme!=null,document.Configuration.Theme);
            sequence.Theme = Theme(sequence.Theme, document.Configuration, host,preset);
            if(document.Configuration.Look==null||document.Configuration.Look=="neo")sequence.Theme=sequence.Theme.Configure(builder=>builder.BarRadius=6);
            if(preset?.TryGet("rectBkgColor",out var rectangle)==true)foreach(var block in sequence.Blocks)if(block.Kind==SequenceArtifactBlockKind.Rect&&block.Text.Length==0)block.BackgroundColor=rectangle;
            document.Configuration.Sequence?.Apply(sequence, document is MermaidSequenceDocument sourceSequence && sourceSequence.Autonumber != null, host,preset,document.Configuration.Look==null||document.Configuration.Look=="neo");
            MermaidSequenceSurfaceEffects.Apply(sequence, document.Configuration, preset, host);
            if (document is MermaidSequenceDocument numbering && (numbering.Autonumber != null || document.Configuration.Sequence?.ShowSequenceNumbers == true))
                sequence.Layout = (sequence.Layout ?? new SequenceLayoutPolicy()).WithNumberPresentation(SequenceNumberPresentation.Badge);
        } else if (model is TopologyChart topology) {
            topology.Accessibility.Name = document.Accessibility.Name;
            topology.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) topology.ThemeMode = Mode(document.Theme);
            if (host?.ThemeMode != null) topology.ThemeMode = host.ThemeMode.Value;
            var sizing = host?.TopologySizing ?? MermaidTopologySizing.Natural;
            topology.DefaultRenderOptions ??= new TopologyRenderOptions();
            topology.DefaultRenderOptions.FitContentToViewport = sizing == MermaidTopologySizing.FixedViewport;
            topology.Theme = Theme(topology.Theme, document.Configuration, host, defaultFontSize: TopologyDefaultFontSize(document, host));
        } else if (model is Chart chart) {
            chart.Accessibility.Name = document.Accessibility.Name;
            chart.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) chart.WithThemeMode(Mode(document.Theme));
            chart.WithTheme(Theme(chart.Options.Theme, document.Configuration, host));
            // Native charts are transparent by default. An authored canvas color also requests its paint.
            if (document.Configuration.ThemeColors?.HasBackground == true) chart.WithTransparentBackground(false);
            document.Configuration.ChartColors?.Apply(chart);
            document.Configuration.RadarTheme?.Apply(chart);
            if (host?.ThemeMode != null) chart.WithThemeMode(host.ThemeMode.Value);
        } else if (model is IVisualBlock block) {
            block.Options.Accessibility.Name = document.Accessibility.Name;
            block.Options.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) block.Options.ThemeMode = Mode(document.Theme);
            if (host?.ThemeMode != null) block.Options.ThemeMode = host.ThemeMode.Value;
            block.Options.Theme = Theme(block.Options.Theme, document.Configuration, host,
                preserveHostTheme: block is JourneyDiagramBlock or TimelineDiagramBlock);
            // An explicit source preset or canvas requests paint; an unstyled diagram keeps its transparent adapter canvas.
            if (block is JourneyDiagramBlock || block is TimelineDiagramBlock) {
                var canvasRequested = document.Configuration.ThemeColors?.HasBackground == true
                    || MermaidThemePreset.IsKnown(document.Configuration.Theme);
                block.Options.TransparentBackground = !canvasRequested
                    || block.Options.Theme.Resolve(block.Options.ThemeMode).Background.A == 0;
            }
        }
        return model;
    }

    private static VisualTheme Theme(VisualTheme source, MermaidSourceConfiguration configuration, MermaidPresentationOptions? host, MermaidThemePreset? resolved = null, double? defaultFontSize = null, bool preserveHostTheme = false) {
        var preset = resolved ?? configuration.Preset?.Resolve(host?.ThemeMode ?? Mode(configuration.Theme ?? "default"), host?.Theme != null, configuration.Theme);
        // Journey/Timeline already expose an explicit host/family palette override.
        // The selected Sequence/preset role precedence does not migrate that incumbent contract.
        var theme = preserveHostTheme && host?.Theme is VisualTheme explicitTheme
            ? explicitTheme : preset?.Apply(host?.Theme ?? source) ?? host?.Theme ?? source;
        return theme.WithTypography(Typography(theme.Typography, configuration, host, defaultFontSize));
    }

    // Explicit host themes retain their native typography when neither host nor source selects a font size.
    // Only the pinned Flowchart/Swimlane family chain is qualified for the implicit16 source default.
    private static double? TopologyDefaultFontSize(MermaidDocument document, MermaidPresentationOptions? host) => host?.Theme != null
        ? null : document.Kind is MermaidDiagramKind.Flowchart or MermaidDiagramKind.Swimlane ? 16 : (double?)null;

    private static VisualTypography Typography(VisualTypography source, MermaidSourceConfiguration configuration, MermaidPresentationOptions? host, double? defaultFontSize = null) {
        var fontSize = host?.FontSize ?? configuration.FontSize ?? defaultFontSize;
        var scale = fontSize.HasValue ? fontSize.Value / source.DataLabelSize : 1;
        return new VisualTypography(host?.FontFamily ?? configuration.FontFamily ?? source.Family, source.TitleSize * scale,
            source.SubtitleSize * scale, source.AxisSize * scale, source.LegendSize * scale,
            source.DataLabelSize * scale, source.ScalarValueSize * scale, source.CenterValueSize * scale,
            source.AxisTitleSize * scale, source.MonospaceFamily);
    }
    private static VisualThemeMode Mode(string theme) => string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? VisualThemeMode.Dark : VisualThemeMode.Light;
}
