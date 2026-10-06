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
            if (artifact.Model != null) Apply(artifact.Model, document);
        } else if (model is SequenceArtifact sequence) {
            sequence.Accessibility.Name = document.Accessibility.Name;
            sequence.Accessibility.Description = document.Accessibility.Description;
        } else if (model is TopologyChart topology) {
            topology.Accessibility.Name = document.Accessibility.Name;
            topology.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) topology.Theme = string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? TopologyTheme.Dark() : TopologyTheme.Light();
        } else if (model is Chart chart) {
            chart.Accessibility.Name = document.Accessibility.Name;
            chart.Accessibility.Description = document.Accessibility.Description;
            if (document.Theme != null) chart.WithTheme(string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? ChartTheme.Dark() : ChartTheme.Light());
        } else if (model is IVisualBlock block && document.Theme != null) {
            block.Options.Theme = string.Equals(document.Theme, "dark", StringComparison.OrdinalIgnoreCase) ? ChartTheme.Dark() : ChartTheme.Light();
        }
        return model;
    }
}
