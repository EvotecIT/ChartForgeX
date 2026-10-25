using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Checks the public wire migration for existing source-family viewport controls.</summary>
internal static class TopologyViewportPaddingFixtureContract {
    internal static void RequireCapturedInsets(VisualArtifactInterchangeEnvelope envelope, double expected) {
        Assert.Equal(2, envelope.Version);
        var padding = Assert.IsType<ChartForgeX.Primitives.ChartPadding>(envelope.Topology!.ViewportPadding);
        Assert.Equal((expected, expected, expected, expected), (padding.Left, padding.Top, padding.Right, padding.Bottom));
        var json = envelope.ToJson();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        Assert.Equal(expected, restored.Viewport.Padding);
        var downgraded = json.Replace("\"version\":2", "\"version\":1", StringComparison.Ordinal);
        Assert.NotEqual(json, downgraded);
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(downgraded));
    }
}
