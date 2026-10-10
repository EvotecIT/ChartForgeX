using System;
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
            if (artifact.Model != null) Apply(artifact.Model, document);
        } else if (model is SequenceArtifact sequence) {
            sequence.Accessibility.Name = document.Accessibility.Name;
            sequence.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) sequence.ThemeMode = string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? VisualThemeMode.Dark : VisualThemeMode.Light;
            if (document.Configuration.FontFamily != null) {
                var typography = sequence.Theme.Typography;
                sequence.Theme = sequence.Theme.WithTypography(new VisualTypography(document.Configuration.FontFamily,
                    typography.TitleSize, typography.SubtitleSize, typography.AxisSize, typography.LegendSize,
                    typography.DataLabelSize, typography.ScalarValueSize, typography.CenterValueSize));
            }
        } else if (model is TopologyChart topology) {
            topology.Accessibility.Name = document.Accessibility.Name;
            topology.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) topology.Theme = string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? TopologyTheme.Dark() : TopologyTheme.Light();
            if (document.Configuration.FontFamily != null) {
                topology.Theme ??= TopologyTheme.Light();
                topology.Theme.FontFamily = document.Configuration.FontFamily;
            }
        } else if (model is Chart chart) {
            chart.Accessibility.Name = document.Accessibility.Name;
            chart.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) chart.WithTheme(string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? ChartTheme.Dark() : ChartTheme.Light());
            if (document.Configuration.FontFamily != null) chart.WithFontFamily(document.Configuration.FontFamily);
        } else if (model is IVisualBlock block) {
            block.Options.Accessibility.Name = document.Accessibility.Name;
            block.Options.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) block.Options.Theme = string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? ChartTheme.Dark() : ChartTheme.Light();
            if (document.Configuration.FontFamily != null) block.Options.Theme.FontFamily = document.Configuration.FontFamily;
        }
        return model;
    }
}
