using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>Explains geometric readability risks at a concrete display or document size.</summary>
public sealed class TopologyReadabilityReport {
    private TopologyReadabilityReport(double scale, int collisions, int outside, IReadOnlyList<string> recommendations) {
        FitScale = scale;
        NodeCollisionCount = collisions;
        OutOfBoundsNodeCount = outside;
        Recommendations = recommendations;
    }

    /// <summary>Gets the uniform scale needed to fit the prepared viewport into the requested target.</summary>
    public double FitScale { get; }
    /// <summary>Gets the number of intersecting node pairs.</summary>
    public int NodeCollisionCount { get; }
    /// <summary>Gets the number of nodes extending beyond the prepared viewport.</summary>
    public int OutOfBoundsNodeCount { get; }
    /// <summary>Gets actionable explanations for the detected geometric risks.</summary>
    public IReadOnlyList<string> Recommendations { get; }
    /// <summary>Gets whether this geometry needs a detail view, pagination, or layout correction.</summary>
    /// <remarks>This is a geometry check, not a guarantee of text, contrast, or semantic clarity.</remarks>
    public bool NeedsDetailViews => Recommendations.Count != 0;

    internal static TopologyReadabilityReport Create(TopologyLayoutDiagnosticReport layout, TopologyChart chart, TopologyRenderOptions options, double width, double height, double minimumScale) {
        Positive(width, nameof(width));
        Positive(height, nameof(height));
        Positive(minimumScale, nameof(minimumScale));
        if (minimumScale > 1) throw new ArgumentOutOfRangeException(nameof(minimumScale), "Minimum scale must be at most one.");
        double scale = Math.Min(width / layout.Width, height / layout.Height);
        var visibleNodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (TopologyRenderPrimitives.EffectiveNodeDisplayMode(node, options) != TopologyNodeDisplayMode.Hidden) visibleNodeIds.Add(node.Id);
        }
        int outside = 0;
        foreach (var node in layout.Nodes) {
            if (!visibleNodeIds.Contains(node.Id)) continue;
            var bounds = node.Bounds;
            if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > layout.Width || bounds.Bottom > layout.Height) outside++;
        }
        int collisions = 0;
        foreach (var collision in layout.Collisions) {
            if (visibleNodeIds.Contains(collision.FirstId) && visibleNodeIds.Contains(collision.SecondId)) collisions++;
        }
        var recommendations = new List<string>();
        if (collisions > 0) recommendations.Add("Node bounds overlap. Use bounded report pages or a less dense layout.");
        if (outside > 0) recommendations.Add("Nodes extend outside the viewport. Expand the page or correct explicit coordinates.");
        if (scale < minimumScale) recommendations.Add("Fitting this diagram would reduce its contents below the requested minimum scale. Use an overview with detail pages.");
        return new TopologyReadabilityReport(scale, collisions, outside, recommendations.AsReadOnly());
    }

    private static void Positive(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "Value must be positive and finite.");
    }
}
