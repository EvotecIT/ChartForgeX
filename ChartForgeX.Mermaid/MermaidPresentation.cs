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
            if (document.Theme != null) topology.ThemeMode = Mode(document.Theme);
            topology.Theme = topology.Theme.WithTypography(Typography(topology.Theme.Typography, document.Configuration));
        } else if (model is Chart chart) {
            chart.Accessibility.Name = document.Accessibility.Name;
            chart.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) chart.WithThemeMode(Mode(document.Theme));
            chart.WithTheme(chart.Options.Theme.WithTypography(Typography(chart.Options.Theme.Typography, document.Configuration)));
            document.Configuration.ChartColors?.Apply(chart);
            document.Configuration.RadarTheme?.Apply(chart);
        } else if (model is IVisualBlock block) {
            block.Options.Accessibility.Name = document.Accessibility.Name;
            block.Options.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) block.Options.ThemeMode = Mode(document.Theme);
            block.Options.Theme = block.Options.Theme.WithTypography(Typography(block.Options.Theme.Typography, document.Configuration));
        }
        return model;
    }

    private static VisualTypography Typography(VisualTypography source, MermaidSourceConfiguration configuration) {
        var scale = configuration.FontSize.HasValue ? configuration.FontSize.Value / source.DataLabelSize : 1;
        return new VisualTypography(configuration.FontFamily ?? source.Family, source.TitleSize * scale,
            source.SubtitleSize * scale, source.AxisSize * scale, source.LegendSize * scale,
            source.DataLabelSize * scale, source.ScalarValueSize * scale, source.CenterValueSize * scale,
            source.AxisTitleSize * scale, source.MonospaceFamily);
    }
    private static VisualThemeMode Mode(string theme) => string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? VisualThemeMode.Dark : VisualThemeMode.Light;
}
