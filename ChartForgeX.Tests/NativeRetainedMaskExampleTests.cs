using System;
using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRetainedMaskExampleTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public void OrdinaryCoreProgramAndCompiledExampleUseSameRetainedPresentation(string variant, VisualThemeMode mode) {
        var source = NativeRetainedMaskExample.PrepareSource(variant, mode);
        var direct = source.WithMask(NativeRetainedMaskExample.CreateMask(source));
        var example = NativeRetainedMaskExample.Create(variant, mode);
        RetainedMaskFixture.Capture("retained-mask-example-" + variant + "-" + mode,
            source, example, RetainedMaskFixture.Pixels);
        Assert.Equal(direct.ToPng(), example.ToPng());
        var svg = example.ToSvg(new VisualSvgOptions(idPrefix: "retained-example", effectProfile: VisualSvgEffectProfile.NativeFilters));
        Assert.Contains("<mask", svg);
        Assert.Contains("href=\"#guide\"", svg);
        Assert.Contains(XDocument.Parse(svg).Descendants(), element => element.Name.LocalName == "text");
        var program = ExampleRecipeSource.EmitRetainedMask(variant, mode);
        Assert.DoesNotContain("{{", program);
        Assert.DoesNotContain("ChartForgeX.Examples", program);
        Assert.DoesNotContain("ChartForgeX.Composition", program);
        Assert.DoesNotContain("ChartForgeX.Mermaid", program);
        Assert.Contains("source.WithMask", program);
        Assert.Contains("unmasked-source-facts.json", program);
        var saved = example.ToArtifact().ToInterchangeJson();
        var restored = NativeRetainedMaskExample.Restore(saved, variant, mode);
        var edited = NativeRetainedMaskExample.Restore(saved, variant, mode, "Approved");
        RetainedMaskFixture.Capture("retained-mask-example-restored-" + variant + "-" + mode,
            source, restored, RetainedMaskFixture.Pixels);
        RetainedMaskFixture.Capture("retained-mask-example-edited-" + variant + "-" + mode,
            source, edited, RetainedMaskFixture.Pixels);
        Assert.True(example.ToArtifact().SupportsExport(VisualArtifactExportFormat.Json));
        Assert.Equal(example.ToPng(), restored.ToPng());
        Assert.Contains("Approved", edited.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        Assert.Contains("masked.json", program);
        Assert.Contains("ApplyCapturedEffects", program);
        Assert.Contains(".Messages[0].Text", program);
    }
}
