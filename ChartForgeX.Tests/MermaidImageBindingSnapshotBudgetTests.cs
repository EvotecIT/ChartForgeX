using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Public option-copy boundary contracts; each selected row can retain up to256MiB of owned pixels.</summary>
public sealed class MermaidImageBindingSnapshotBudgetTests {
    [Fact] [Trait("Category", "LargeResourceContract")]
    public void Exactly256MiBOfDistinctViewsIsAdmittedWithoutChargingTheirSharedCallerArrayPerKey() {
        // 255 distinct heights128..382 plus511 total65536 rows;1024*65536*4 is exactly256MiB.
        var source = Catalog(Enumerable.Range(128, 255).Append(511));
        var copy = (MermaidFlowchartRenderOptions)source.Clone();
        var total = copy.ImageBindings!.Values.Sum(image => image.Pixels.LongLength);
        Capture("boundary", new { count = copy.ImageBindings.Count, copiedBytes = total, expected = 256L * 1024 * 1024 });
        Assert.Equal(256, copy.ImageBindings.Count); Assert.Equal(256L * 1024 * 1024, total);
        Assert.Equal(256, copy.ImageBindings.Values.Select(image => image.Pixels).Distinct().Count());
        Assert.NotSame(source.ImageBindings!["height-511"].Pixels, copy.ImageBindings["height-511"].Pixels);
    }

    [Fact] [Trait("Category", "LargeResourceContract")]
    public void CumulativeDistinctViewAdmissionRejectsTheNextCopyBeforeAllocation() {
        // Exactly256keys remain valid, but heights129..384 require256.5MiB of distinct copies.
        var source = Catalog(Enumerable.Range(129, 256)); Exception? error = null;
        try { _ = source.Clone(); } catch (Exception failure) { error = failure; }
        var limit = error as VisualResourceLimitException;
        Capture("above-boundary", new { error = error?.GetType().FullName, message = error?.Message, limit = limit?.Limit,
            maximum = limit?.Maximum, observed = limit?.Observed, item = limit?.ItemId });
        var actual = Assert.IsType<VisualResourceLimitException>(error);
        Assert.Equal("SourceImageSnapshotBytes", actual.Limit); Assert.Equal(256L * 1024 * 1024, actual.Maximum);
        Assert.Equal(256L * 1024 * 1024 + 512L * 1024, actual.Observed); Assert.Equal("height-384", actual.ItemId);
    }

    private static MermaidFlowchartRenderOptions Catalog(IEnumerable<int> heights) {
        var values = heights.ToArray(); var bytes = new byte[checked(1024 * values.Max() * 4)];
        var bindings = new Dictionary<string, RgbaImage>();
        foreach (var height in values) bindings.Add("height-" + height, new RgbaImage(1024, height, bytes));
        return new MermaidFlowchartRenderOptions { ImageBindings = bindings };
    }
    private static void Capture(string name, object facts) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "image-snapshot-" + name + ".json"), System.Text.Json.JsonSerializer.Serialize(facts));
    }
}
