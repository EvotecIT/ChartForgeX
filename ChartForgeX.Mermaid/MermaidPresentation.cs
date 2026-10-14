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
    internal static T Apply<T>(T model, MermaidDocument document) where T : class {
        if (model is VisualArtifact artifact) {
            artifact.Accessibility.Name = document.Accessibility.Name;
            artifact.Accessibility.Description = document.Accessibility.Description;
            if (document.Configuration.Theme != null) artifact.Metadata["mermaid.config.theme"] = document.Configuration.Theme;
            if (document.Configuration.Layout != null) artifact.Metadata["mermaid.config.layout"] = document.Configuration.Layout;
            if (document.Configuration.Look != null) artifact.Metadata["mermaid.config.look"] = document.Configuration.Look;
            if (document.Configuration.FontFamily != null) artifact.Metadata["mermaid.config.fontFamily"] = document.Configuration.FontFamily;
            if (document.Configuration.FontSize.HasValue) artifact.Metadata["mermaid.config.fontSize"] = document.Configuration.FontSize.Value.ToString(CultureInfo.InvariantCulture);
            if (artifact.Model != null) Apply(artifact.Model, document);
        } else if (model is SequenceArtifact sequence) {
            sequence.Accessibility.Name = document.Accessibility.Name;
            sequence.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) sequence.ThemeMode = string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? VisualThemeMode.Dark : VisualThemeMode.Light;
            sequence.Theme = sequence.Theme.WithTypography(Typography(sequence.Theme.Typography, document.Configuration));
        } else if (model is TopologyChart topology) {
            topology.Accessibility.Name = document.Accessibility.Name;
            topology.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) topology.Theme = string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? TopologyTheme.Dark() : TopologyTheme.Light();
            if (document.Configuration.FontFamily != null) {
                topology.Theme ??= TopologyTheme.Light();
                topology.Theme.FontFamily = document.Configuration.FontFamily;
            }
            if (document.Configuration.FontSize.HasValue) {
                topology.Theme ??= TopologyTheme.Light();
                topology.Theme.Typography = Typography(topology.Theme.Typography, document.Configuration);
            }
        } else if (model is Chart chart) {
            chart.Accessibility.Name = document.Accessibility.Name;
            chart.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) chart.WithTheme(SourceTheme(document.Theme));
            if (document.Configuration.FontFamily != null) chart.WithFontFamily(document.Configuration.FontFamily);
            ScaleChartTypography(chart.Options.Theme, document.Configuration.FontSize);
        } else if (model is IVisualBlock block) {
            block.Options.Accessibility.Name = document.Accessibility.Name;
            block.Options.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) block.Options.Theme = SourceTheme(document.Theme);
            if (document.Configuration.FontFamily != null) block.Options.Theme.FontFamily = document.Configuration.FontFamily;
            ScaleChartTypography(block.Options.Theme, document.Configuration.FontSize);
        }
        return model;
    }

    // Charts and blocks start from the Graphite theme, so the source's light or dark choice must swap only the
    // Graphite colours; the legacy report presets would also change typography, corners and spacing.
    private static ChartTheme SourceTheme(string theme) =>
        string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight();

    private static VisualTypography Typography(VisualTypography source, MermaidSourceConfiguration configuration) {
        var scale = configuration.FontSize.HasValue ? configuration.FontSize.Value / source.DataLabelSize : 1;
        return new VisualTypography(configuration.FontFamily ?? source.Family, source.TitleSize * scale,
            source.SubtitleSize * scale, source.AxisSize * scale, source.LegendSize * scale,
            source.DataLabelSize * scale, source.ScalarValueSize * scale, source.CenterValueSize * scale);
    }

    private static void ScaleChartTypography(ChartTheme theme, double? fontSize) {
        if (!fontSize.HasValue) return;
        var scale = fontSize.Value / theme.DataLabelFontSize;
        theme.WithTypography(theme.TitleFontSize * scale, theme.SubtitleFontSize * scale,
            theme.AxisTitleFontSize * scale, theme.TickLabelFontSize * scale,
            theme.LegendFontSize * scale, fontSize.Value);
    }
}
