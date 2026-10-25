using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyActorCaptionMixedRoutingTests {
    [Theory]
    [InlineData(TopologyEdgeRouting.Straight, false)]
    [InlineData(TopologyEdgeRouting.Straight, true)]
    [InlineData(TopologyEdgeRouting.Orthogonal, false)]
    [InlineData(TopologyEdgeRouting.Orthogonal, true)]
    public void MixedActualActorAndObstaclePoliciesUseFiniteCaptionClearance(TopologyEdgeRouting policy, bool readable) {
        var model = UseCaseAdvancedActorsRecipe.Create("wide", VisualThemeMode.Dark);
        model.WithTheme(model.Theme.WithFontFamily(BrowserProofFonts.Family), VisualThemeMode.Dark);
        model.DefaultRenderOptions!.ReadableDenseLayout = readable;
        foreach (var edge in model.Edges) edge.Routing = policy;
        model.Edges.Single(edge => edge.Id == "system").Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
        var prepared = model.Prepare(); var envelope = prepared.ToInterchangeEnvelope(); var json = envelope.ToJson();
        var name = "actor-caption-mixed-" + policy + "-" + readable;
        Capture(name, prepared.ToSvg(), prepared.ToPng(), json);
        var observation = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var options = model.DefaultRenderOptions!.Clone();
        options.ResolvedEdgeLabelScale = model.Theme.Typography.DataLabelSize / 11;
        var measurement = new TextMeasurementContext(BrowserProofFonts.Family);
        var collisions = new List<object>();
        foreach (var edge in envelope.Edges) foreach (var node in observation.Nodes) {
            if (node.Id == edge.SourceId || node.Id == edge.TargetId || !TopologyActorCaption.Applies(node, options)) continue;
            var caption = TopologyActorCaption.Prepare(node, options, measurement).Bounds;
            for (var index = 1; index < edge.ResolvedRoute.Count; index++) {
                var first = edge.ResolvedRoute[index - 1]; var last = edge.ResolvedRoute[index];
                if (CrossesInterior(caption, new ChartPoint(first.X, first.Y), new ChartPoint(last.X, last.Y))) collisions.Add(new {
                    edge = edge.Id, actor = node.Id, segment = index - 1, first.X, first.Y, targetX = last.X, targetY = last.Y,
                    caption.Left, caption.Top, caption.Width, caption.Height
                });
            }
        }
        Write(name + ".collisions.json", JsonSerializer.Serialize(collisions));
        // Existing native artifacts, numeric routes and measured-caption collisions precede the expected assertion.
        Assert.Empty(collisions);
    }

    // Independent clipping observer for the public resolved route, using an open rectangle interior so a shared boundary is not counted as ink through text.
    private static bool CrossesInterior(ChartRect bounds, ChartPoint start, ChartPoint end) {
        const double inset = .001;
        var left = bounds.Left + inset; var top = bounds.Top + inset; var right = bounds.Right - inset; var bottom = bounds.Bottom - inset;
        if (right <= left || bottom <= top) return false;
        var first = 0d; var last = 1d; var dx = end.X - start.X; var dy = end.Y - start.Y;
        return Clip(-dx, start.X - left) && Clip(dx, right - start.X) && Clip(-dy, start.Y - top) && Clip(dy, bottom - start.Y) && last >= first;
        bool Clip(double direction, double distance) {
            if (direction == 0) return distance >= 0;
            var value = distance / direction;
            if (direction < 0) { if (value > last) return false; if (value > first) first = value; }
            else { if (value < first) return false; if (value < last) last = value; }
            return true;
        }
    }
    private static void Capture(string name, string svg, byte[] png, string json) { Write(name + ".svg", svg); Write(name + ".json", json); var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png); }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
