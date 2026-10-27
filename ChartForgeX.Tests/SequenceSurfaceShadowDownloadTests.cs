using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Complete source downloads capture native filters in the same measured frame as their ordinary exports.</summary>
public sealed class SequenceSurfaceShadowDownloadTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public void NativeFilterCaptureUsesTheCompletedSourceFrame(string variant, VisualThemeMode mode) {
        const string source = "sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\nA->>B: Save\nloop Audit\nB-->>A: Stored\nend\nNote over A,B: Retain audit facts\n";
        var result = SequenceSurfaceShadowSourceRecipe.Parse(source, variant, mode);
        var expected = XElement.Parse(result.Artifact!.ToSvg());
        var prepared = SequenceSurfaceShadowSourceRecipe.PrepareCapture(result);
        var actual = XElement.Parse(prepared.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        Assert.Equal(expected.Attribute("viewBox")!.Value, actual.Attribute("viewBox")!.Value);
        Assert.Contains(actual.Descendants(), element => element.Name.LocalName == "filter");
        Assert.NotEmpty(prepared.ToPng());
    }
}
