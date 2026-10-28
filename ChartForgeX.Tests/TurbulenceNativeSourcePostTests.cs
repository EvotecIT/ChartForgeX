using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TurbulenceNativeSourcePostTests {
    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void NativeSourceUsesTheSameSourceFreePlanAtActualDeviceCellCenters(int density) {
        var effect = new VisualTurbulence(.071, .113, 2, 13, VisualTurbulenceType.FractalNoise);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(32, 24), 0),
            frame: new VisualFrame(transparentBackground: true));
        var prepared = effect.Prepare(context);
        var expected = effect.Generate(new VisualSize(32, 24), density);
        var actual = prepared.ToRgba(new VisualRenderOptions(density, 1));
        NativeEffectBeforeCapture.Write("turbulence-native-source-" + density, prepared.ToSvg(), actual.ToPng(), actual);
        // The retained source uses the incumbent image sink, including its established source-over byte conversion.
        var imageBasis = new VisualSceneBuilder(new VisualSize(32, 24), FontSpec.SystemSans());
        imageBasis.Image(expected, new ChartRect(0, 0, 32, 24));
        var expectedPaint = new PreparedVisual(imageBasis.Build()).ToRgba(new VisualRenderOptions(density, 1));
        Assert.Equal(expectedPaint.Pixels, actual.Pixels);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneProceduralSource>());
        Assert.Empty(prepared.Scene.Nodes.OfType<VisualSceneImage>());
        Assert.Single(prepared.Regions, row => row.Id == "turbulence" && row.Role == "procedural-source");
        Assert.Null(prepared.Scene.ValueCapture); Assert.Null(prepared.Scene.VisibilityCapture);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)] [InlineData(VisualThemeMode.Dark)]
    public void CommonFrameHeadingsAndInsetsStayIndependentOfTheSingleProceduralSurface(VisualThemeMode mode) {
        var effect = new VisualTurbulence(.071, .113, 2, 13, VisualTurbulenceType.FractalNoise);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(390, 280), new ChartPadding(17, 23, 31, 29)),
            themeMode: mode, frame: new VisualFrame("Noise as a source", "Four independent channels", showLegend: false));
        var prepared = effect.Prepare(context);
        var native = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        NativeEffectBeforeCapture.Write("turbulence-native-frame-" + mode, native.ToString(), prepared.ToPng(), prepared.ToRgba());
        var surface = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneProceduralSource>());
        Assert.Equal(17d, surface.Bounds.Left); Assert.Equal(359d, surface.Bounds.Right);
        Assert.True(surface.Bounds.Top > 23); Assert.Equal(251d, surface.Bounds.Bottom);
        Assert.Contains(native.Descendants(), node => node.Name.LocalName == "text" && node.Value == "Noise as a source");
        Assert.Contains(native.Descendants(), node => node.Name.LocalName == "text" && node.Value == "Four independent channels");
        Assert.Single(native.Descendants(), node => node.Name.LocalName == "feTurbulence");
        Assert.DoesNotContain(native.Descendants(), node => node.Name.LocalName is "script" or "foreignObject" or "use");
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.ImageResource != null);
    }

    [Fact]
    public void InitialNativeOpacityFramesRetainTheSameProceduralSourceAndGeneratedResourceIndexLaw() {
        var prepared = new VisualTurbulence(0, type: VisualTurbulenceType.FractalNoise)
            .Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(8, 6), 0),
                frame: new VisualFrame(transparentBackground: true)));
        var source = prepared.WithScene(prepared.Scene, new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        var frames = VisualValueCapture.InitialFadeFrames(source, 3);
        var actual = frames[1].ToRgba(new VisualRenderOptions(supersampling: 1));
        NativeEffectBeforeCapture.Write("turbulence-initial-opacity-middle", frames[1].ToSvg(), actual.ToPng(), actual);
        Assert.Same(source, frames[2]);
        Assert.True(frames[1].Scene.HasProceduralSources);
        Assert.Single(XDocument.Parse(frames[1].ToSvg()).Descendants(), node => node.Name.LocalName == "feTurbulence");
        Assert.Equal(64, actual.Pixels[3]);
        Assert.Equal(source.ToSvg(), frames[2].ToSvg());
        Assert.False(frames[1].ToArtifact().ExportFormats.HasFlag(VisualArtifactExportFormat.Json));
        Assert.Throws<NotSupportedException>(()=>frames[1].ToArtifact().ToInterchangeJson());
        Assert.True(frames[2].ToArtifact().ExportFormats.HasFlag(VisualArtifactExportFormat.Json));
    }

    [Fact]
    public void OutputProfilesAreExplicitAndTypedSourcePolicyIsRetainedWithoutInventingOtherSceneModels() {
        var source = new VisualTurbulence(.071, .113, int.MaxValue, 13, VisualTurbulenceType.FractalNoise);
        var prepared = source.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(32, 24), 0),
            frame: new VisualFrame(transparentBackground: true)));
        var native = prepared.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        var baked = prepared.ToSvg(new VisualSvgOptions(effectRasterOptions: new VisualRenderOptions(supersampling: 1)));
        NativeEffectBeforeCapture.Write("turbulence-profile-native", native, prepared.ToPng(), prepared.ToRgba());
        var nativeTree = XDocument.Parse(native); var bakedTree = XDocument.Parse(baked);
        Assert.Equal("9", Assert.Single(nativeTree.Descendants(), node => node.Name.LocalName == "feTurbulence").Attribute("numOctaves")?.Value);
        Assert.DoesNotContain(nativeTree.Descendants(), node => node.Name.LocalName == "image");
        Assert.Single(bakedTree.Descendants(), node => node.Name.LocalName == "image");
        Assert.DoesNotContain(bakedTree.Descendants(), node => node.Name.LocalName == "filter");
        var artifact = prepared.ToArtifact();
        Assert.True(artifact.ExportFormats.HasFlag(VisualArtifactExportFormat.Json));
        var saved=VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(int.MaxValue,saved.Presentation!.TurbulenceSource!.NumberOfOctaves);
        var unsupported=new VisualSceneBuilder(new VisualSize(32,24),FontSpec.SystemSans());
        unsupported.ProceduralSource(source,new ChartRect(0,0,32,24));
        var unrepresented=new PreparedVisual(unsupported.Build()).ToArtifact();
        Assert.False(unrepresented.ExportFormats.HasFlag(VisualArtifactExportFormat.Json));
        Assert.Throws<NotSupportedException>(()=>unrepresented.ToInterchangeJson());
        Assert.Throws<OperationCanceledException>(() => prepared.ToSvg(new VisualSvgOptions(), new CancellationToken(true)));
        Assert.Throws<OperationCanceledException>(() => prepared.ToPng(new VisualRenderOptions(), new CancellationToken(true)));
        Assert.Equal(int.MaxValue, source.NumberOfOctaves);
    }
    [Theory]
    [InlineData("compact",VisualThemeMode.Light)] [InlineData("compact",VisualThemeMode.Dark)]
    [InlineData("wide",VisualThemeMode.Light)] [InlineData("wide",VisualThemeMode.Dark)]
    public void CompleteCoreOnlyProgramSavesLoadsEditsAndDisplaysTheSameSharedSource(string variant,VisualThemeMode mode) {
        var saved=NativeTurbulenceExample.Save(variant,mode);
        var loaded=NativeTurbulenceExample.Load(saved,variant,mode);
        var edited=NativeTurbulenceExample.Edit(saved,14,variant,mode);
        TurbulencePortableFixture.Save("portable-example-loaded-"+variant+"-"+mode,loaded,loaded.ToArtifact().ToInterchangeEnvelope());
        TurbulencePortableFixture.Save("portable-example-edited-"+variant+"-"+mode,edited,edited.ToArtifact().ToInterchangeEnvelope());
        var program=ExampleRecipeSource.EmitNativeTurbulence(variant,mode);
        Assert.Equal(NativeTurbulenceExample.Create(variant,mode).ToPng(),loaded.ToPng());
        Assert.Equal(14,edited.ToArtifact().ToInterchangeEnvelope().Presentation!.TurbulenceSource!.Seed);
        Assert.Contains("PrepareTurbulenceSource",program);Assert.Contains("noise.source.json",program);
        Assert.DoesNotContain("ChartForgeX.Mermaid",program);Assert.DoesNotContain("ChartForgeX.Visuals",program);
        Assert.DoesNotContain("{{",program);Assert.DoesNotContain("using ",program.Substring(program.IndexOf("public static class NativeTurbulenceExample",StringComparison.Ordinal)));
    }

}
