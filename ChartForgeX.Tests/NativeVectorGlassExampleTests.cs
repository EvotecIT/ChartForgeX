using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeVectorGlassExampleTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public void OnlyTheExplicitBackdropIsBakedAndForegroundCaptionsUseTheIncumbentNativeTextOwner(string variant, VisualThemeMode mode) {
        var canvas = NativeVectorGlassExample.Create(variant, mode);
        var svg = canvas.ToSvg();
        var png = canvas.ToPng();
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, "vector-glass-" + variant + "-" + mode);
            File.WriteAllText(stem + ".svg", svg);
            File.WriteAllBytes(stem + ".png", png);
            File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitNativeVectorGlass(variant, mode));
        }
        var document = XDocument.Parse(svg);
        Assert.Single(document.Descendants(), node => node.Name.LocalName == "image");
        var text = document.Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value).ToArray();
        Assert.Contains("Service window", text);
        Assert.Contains("Explicit local backdrop", text);
        Assert.Contains("34 ready", text);
        Assert.Contains("Foreground remains native text", text);
        Assert.DoesNotContain(document.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
        // Shared VisualCanvas definitions are emitted even when unused; no foreground or backdrop
        // element in this recipe selects one. Only the explicit local backdrop is baked.
        Assert.DoesNotContain(document.Descendants().Attributes(), attribute => attribute.Name.LocalName == "filter");
        var program = ExampleRecipeSource.EmitNativeVectorGlass(variant, mode);
        Assert.DoesNotContain("{{", program);
        Assert.DoesNotContain("ChartForgeX.Examples", program);
        Assert.Contains("BlurRegion", program);
    }
}
