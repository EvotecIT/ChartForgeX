using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRetainedAppearanceExampleTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public void OrdinaryCoreProgramSavesEditsAndReplaysRetainedAndTerminalPresentation(string variant, VisualThemeMode mode) {
        var root = NativeRetainedAppearanceExample.Create(variant, mode);
        var saved = root.ToArtifact().ToInterchangeJson();
        var replayed = NativeRetainedAppearanceExample.RestoreRoot(saved, variant, mode);
        var edited = NativeRetainedAppearanceExample.RestoreRoot(saved, variant, mode, "Approved");
        var layer = NativeRetainedAppearanceExample.Capture(variant, mode);
        var captureSaved = layer.ToArtifact().ToInterchangeJson();
        var restored = NativeRetainedAppearanceExample.RestoreLayer(captureSaved, variant, mode);
        var editedLayer = NativeRetainedAppearanceExample.RestoreLayer(captureSaved, variant, mode, "Approved");
        RetainedAppearanceFixture.Save("appearance-example-root-" + variant + "-" + mode, root);
        RetainedAppearanceFixture.Save("appearance-example-edited-" + variant + "-" + mode, edited);
        RetainedAppearanceFixture.Save("appearance-example-terminal-" + variant + "-" + mode, layer, captureSaved);
        Assert.Equal(root.ToPng(), replayed.ToPng());
        Assert.Equal(layer.Bounds, restored.Bounds); Assert.Equal(layer.ToImage().Pixels, restored.ToImage().Pixels);
        var options = new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters);
        var document = XDocument.Parse(root.ToSvg(options));
        foreach (var text in new[] { "Review", "Archive", "Save", "Stored", "Facts stay editable" })
            Assert.Contains(document.Descendants(), node => node.Name.LocalName == "text" && node.Value == text);
        Assert.Single(document.Descendants(), node => node.Name.LocalName == "a" && (string?)node.Attribute("href") == "#guide");
        Assert.DoesNotContain(document.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
        Assert.Contains("Approved", edited.ToSvg(options));
        Assert.Contains("Approved", editedLayer.Source.ToSvg(options));
        Assert.False(layer.ToImage().Pixels.SequenceEqual(editedLayer.ToImage().Pixels));
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(saved);
        Assert.Throws<NotSupportedException>(() => envelope.ToSequenceArtifact());
        var terminal = VisualArtifactInterchangeEnvelope.FromJson(captureSaved);
        Assert.Equal(root.Size.Width, terminal.Width); Assert.Equal(root.Size.Height, terminal.Height);
        Assert.Equal(5, terminal.Presentation!.CapturedEffects.Count);
        var program = ExampleRecipeSource.EmitNativeRetainedAppearance(variant, mode);
        Assert.DoesNotContain("{{", program); Assert.DoesNotContain("ChartForgeX.Examples", program);
        Assert.DoesNotContain("ChartForgeX.Composition", program); Assert.DoesNotContain("ChartForgeX.Mermaid", program);
        Assert.Contains("ApplyCapturedLayer", program); Assert.Contains("CapturedEffects.Clear", program);
    }
}
