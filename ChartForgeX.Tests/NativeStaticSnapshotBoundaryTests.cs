using System.Text.Json;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeStaticSnapshotBoundaryTests {
    [Fact]
    public void ImplicitWrappingRetainsDetachedLazySemanticsAndARejectedJsonReadCannotDisableStaticOutput() {
        var prepared = Chart().Prepare(); var semantics = prepared.ToInterchangeEnvelope();
        for (var index = 0; index < 1025; index++) semantics.Nodes[0].Metrics.Add(new VisualArtifactInterchangeMetric { Name = "metric-" + index, Value = "1" });
        semantics.Extensions["capture"] = "original";
        var captured = new PreparedVisual(prepared.Visual.Scene, prepared.Visual.Accessibility, semantics);
        var native = captured.ToPng();
        var first = captured.ToArtifact("first", VisualArtifactKind.Topology);
        var clone = first.Clone(); var second = captured.ToArtifact("second", VisualArtifactKind.Topology);
        semantics.Nodes[0].Metrics.Clear(); semantics.Nodes[0].Label = "Changed caller"; semantics.Extensions["capture"] = "changed caller";
        first.Title = "Changed host"; first.Metadata["capture"] = "changed host"; first.Regions[0].Label = "Changed region";
        var firstPng = Observe("lazy-static.first.png", () => first.ToPng());
        var clonePng = Observe("lazy-static.clone.png", () => clone.ToPng());
        var secondPng = Observe("lazy-static.second.png", () => second.ToPng());
        var json = Observe("lazy-static.rejected.json", () => first.ToInterchangeJson());
        var after = Observe("lazy-static.after-rejected-json.png", () => first.ToPng());
        Assert.Null(firstPng.Error); Assert.Null(clonePng.Error); Assert.Null(secondPng.Error); Assert.Null(after.Error);
        Assert.Equal(native, firstPng.Value); Assert.Equal(native, clonePng.Value); Assert.Equal(native, secondPng.Value); Assert.Equal(native, after.Value);
        Assert.IsType<ArgumentOutOfRangeException>(json.Error);
        Assert.Equal("original", clone.Metadata["capture"]); Assert.Equal("original", second.Metadata["capture"]);
        Assert.NotEqual(first.Regions[0].Label, clone.Regions[0].Label); Assert.NotEqual(first.Title, clone.Title);
        var reader = captured.CapturedSemanticSnapshot!; reader.Nodes[0].Metrics.Clear(); reader.Nodes[0].Label = "Changed detached reader";
        Assert.Equal(1025, captured.CapturedSemanticSnapshot!.Nodes[0].Metrics.Count);
        Assert.Equal("Node", captured.CapturedSemanticSnapshot!.Nodes[0].Label);
    }

    [Fact]
    public void ExplicitCallerSemanticsRetainEagerValidationAndIndependentValidCapture() {
        var prepared = Chart().Prepare(); var semantics = prepared.ToInterchangeEnvelope();
        var valid = prepared.Visual.ToArtifact(semantics.Id, semantics.Kind, semantics);
        var original = valid.ToInterchangeJson(); semantics.Nodes[0].Label = "Changed source";
        var retained = Observe("explicit-static.valid.json", () => valid.ToInterchangeJson());
        for (var index = 0; index < 1025; index++) semantics.Nodes[0].Metrics.Add(new VisualArtifactInterchangeMetric { Name = "metric-" + index, Value = "1" });
        var invalid = Observe("explicit-static.invalid.json", () => prepared.Visual.ToArtifact(semantics.Id, semantics.Kind, semantics).ToInterchangeJson());
        Assert.Null(retained.Error); Assert.Equal(original, retained.Value); Assert.IsType<ArgumentOutOfRangeException>(invalid.Error);
        var portable = VisualArtifactInterchangeEnvelope.FromJson(retained.Value!); portable.Nodes[0].Label = "Changed portable reader";
        Assert.Equal(original, valid.Clone().ToInterchangeJson());
    }

    private static TopologyChart Chart() {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(240, 160, 8).WithLegend(null); chart.Id = "native-static"; chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Node", X = 30, Y = 30, Width = 100, Height = 50, ShowStatusBadge = false }); return chart;
    }
    private static (T? Value, Exception? Error) Observe<T>(string name, Func<T> action) {
        try { var value = action(); if (value is string text) Write(name, text); else if (value is byte[] bytes) Write(name, bytes); return (value, null); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException or OverflowException) {
            Write(name + ".error.json", JsonSerializer.Serialize(new { type = exception.GetType().FullName, exception.Message })); return (default, exception);
        }
    }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static void Write(string name, byte[] bytes) { var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name), bytes); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_RASTER_RESOURCE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
