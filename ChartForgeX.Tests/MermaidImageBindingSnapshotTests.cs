using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidImageBindingSnapshotTests {
    [Fact]
    public void PublicOptionCloneDetachesCallerPixelsAndPreservesIndependentViewDescriptors() {
        var bytes = new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 };
        var source = Options(new() { ["wide"] = new RgbaImage(2, 1, bytes), ["tall"] = new RgbaImage(1, 2, bytes), ["short"] = new RgbaImage(1, 1, bytes) });
        var copy = (MermaidFlowchartRenderOptions)source.Clone(); var views = copy.ImageBindings!;
        Assert.NotSame(source.ImageBindings, views); Assert.NotSame(bytes, views["wide"].Pixels);
        Assert.Same(views["wide"].Pixels, views["tall"].Pixels); Assert.NotSame(views["wide"].Pixels, views["short"].Pixels);
        Assert.Equal((2, 1), (views["wide"].Width, views["wide"].Height)); Assert.Equal((1, 2), (views["tall"].Width, views["tall"].Height));
        bytes[0] = 17; source.ImageBindings!.Clear(); Assert.Equal(255, views["wide"].Pixels[0]); Assert.Equal(3, views.Count);
        var second = (MermaidFlowchartRenderOptions)copy.Clone(); views["wide"].Pixels[0] = 99;
        Assert.Equal(255, second.ImageBindings!["wide"].Pixels[0]);
    }

    [Fact]
    public void All256KeysCanShareOneOwnedViewWithoutChargingPerPlacementOrIntroducingOtherAliases() {
        var bytes = new byte[1_052_672]; // 514*512*4; charging256 identical views would exceed the selected256MiB bound.
        var bindings = new Dictionary<string, RgbaImage>();
        for (var i = 0; i < 256; i++) bindings.Add("image-" + i, new RgbaImage(514, 512, bytes));
        var copy = (MermaidFlowchartRenderOptions)Options(bindings).Clone();
        Assert.Equal(256, copy.ImageBindings!.Count); Assert.NotSame(bytes, copy.ImageBindings["image-0"].Pixels);
        Assert.All(copy.ImageBindings.Values, image => Assert.Same(copy.ImageBindings["image-0"].Pixels, image.Pixels));
        bindings.Add("one-too-many", new RgbaImage(1, 1, bytes));
        Assert.Throws<ArgumentException>(() => Options(bindings).Clone());
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void InvalidDefaultViewsAndEmptyKeysFailAtOptionAdmission(bool emptyKey) {
        var bindings = emptyKey ? new Dictionary<string, RgbaImage> { [" "] = new RgbaImage(1, 1, new byte[4]) }
            : new Dictionary<string, RgbaImage> { ["invalid"] = default };
        Assert.Throws<ArgumentException>(() => Options(bindings).Clone());
    }

    [Fact]
    public void EqualPixelsInIndependentCallerArraysKeepIndependentOwnedCopies() {
        var a = new byte[] { 1, 2, 3, 255 }; var b = (byte[])a.Clone();
        var copy = (MermaidFlowchartRenderOptions)Options(new() { ["a"] = new RgbaImage(1, 1, a), ["b"] = new RgbaImage(1, 1, b) }).Clone();
        Assert.NotSame(copy.ImageBindings!["a"].Pixels, copy.ImageBindings["b"].Pixels);
        copy.ImageBindings["a"].Pixels[0] = 9; Assert.Equal(1, copy.ImageBindings["b"].Pixels[0]); Assert.Equal(1, a[0]); Assert.Equal(1, b[0]);
    }

    // The constructor must capture options before a later fence is parsed; source asset paint contracts live in the asset phase.
    [Fact]
    public void MarkdownOptionSnapshotSurvivesLaterCallerDictionaryAndPixelMutationForAnOrdinaryControl() {
        var pixels = new byte[] { 1, 2, 3, 255 }; var bindings = new Dictionary<string, RgbaImage> { ["opaque-key"] = new RgbaImage(1, 1, pixels) };
        var parser = new MermaidVisualMarkupParser(new MermaidRenderOptions { Flowchart = Options(bindings) });
        pixels[0] = 99; bindings.Clear();
        var result = parser.Parse("```mermaid\nflowchart LR\nA --> B\n```");
        Assert.False(result.HasErrors); Assert.Single(result.Artifacts);
    }

    private static MermaidFlowchartRenderOptions Options(Dictionary<string, RgbaImage> bindings) => new() { ImageBindings = bindings };
}
