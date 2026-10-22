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
            sequence.Theme = Theme(sequence.Theme, document.Configuration, host);
        } else if (model is TopologyChart topology) {
            topology.Accessibility.Name = document.Accessibility.Name;
            topology.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) topology.ThemeMode = Mode(document.Theme);
            if (host?.ThemeMode != null) topology.ThemeMode = host.ThemeMode.Value;
            var sizing = host?.TopologySizing ?? MermaidTopologySizing.Natural;
            topology.DefaultRenderOptions ??= new TopologyRenderOptions();
            topology.DefaultRenderOptions.FitContentToViewport = sizing == MermaidTopologySizing.FixedViewport;
            topology.Theme = Theme(topology.Theme, document.Configuration, host);
        } else if (model is Chart chart) {
            chart.Accessibility.Name = document.Accessibility.Name;
            chart.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) chart.WithThemeMode(Mode(document.Theme));
            chart.WithTheme(Theme(chart.Options.Theme, document.Configuration, host));
            document.Configuration.ChartColors?.Apply(chart);
            document.Configuration.RadarTheme?.Apply(chart);
            if (host?.Theme != null) chart.WithTheme(host.Theme.WithTypography(Typography(host.Theme.Typography, document.Configuration, host)));
            if (host?.ThemeMode != null) chart.WithThemeMode(host.ThemeMode.Value);
        } else if (model is IVisualBlock block) {
            block.Options.Accessibility.Name = document.Accessibility.Name;
            block.Options.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) block.Options.ThemeMode = Mode(document.Theme);
            if (host?.ThemeMode != null) block.Options.ThemeMode = host.ThemeMode.Value;
            block.Options.Theme = Theme(block.Options.Theme, document.Configuration, host);
        }
        return model;
    }

    private static VisualTheme Theme(VisualTheme source, MermaidSourceConfiguration configuration, MermaidPresentationOptions? host) {
        var theme = host?.Theme ?? configuration.ThemeColors?.Apply(source) ?? source;
        return theme.WithTypography(Typography(theme.Typography, configuration, host));
    }

    private static VisualTypography Typography(VisualTypography source, MermaidSourceConfiguration configuration, MermaidPresentationOptions? host) {
        var fontSize = host?.FontSize ?? configuration.FontSize;
        var scale = fontSize.HasValue ? fontSize.Value / source.DataLabelSize : 1;
        return new VisualTypography(host?.FontFamily ?? configuration.FontFamily ?? source.Family, source.TitleSize * scale,
            source.SubtitleSize * scale, source.AxisSize * scale, source.LegendSize * scale,
            source.DataLabelSize * scale, source.ScalarValueSize * scale, source.CenterValueSize * scale,
            source.AxisTitleSize * scale, source.MonospaceFamily);
    }
    private static VisualThemeMode Mode(string theme) => string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? VisualThemeMode.Dark : VisualThemeMode.Light;
}
