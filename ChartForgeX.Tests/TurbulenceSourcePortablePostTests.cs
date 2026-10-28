using System;
using System.IO;
using System.Linq;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TurbulenceSourcePortablePostTests {
    [Theory]
    [InlineData(-7.9,int.MaxValue)]
    [InlineData(1e300,0)]
    public void RawPolicyAndAuthoritativeEnvelopeFactsRestoreThroughTheSameExplicitContext(double seed,int octaves) {
        var policy=new VisualTurbulence(.071,.113,octaves,seed,VisualTurbulenceType.FractalNoise,true,VisualFilterColorSpace.LinearRgb);
        var context=TurbulencePortableFixture.Context();
        var first=policy.Prepare(context);
        var envelope=first.ToArtifact("noise-save").ToInterchangeEnvelope();
        envelope.SourceLanguage=VisualArtifactSourceLanguage.Native;
        envelope.Extensions["raw-origin"]="sensor/noise";
        envelope.AccessibleName="Procedural sample";
        var json=envelope.ToJson();
        var loaded=VisualArtifactInterchangeEnvelope.FromJson(json);
        var restored=loaded.PrepareTurbulenceSource(context);
        TurbulencePortableFixture.Save("portable-noise-raw-"+octaves,restored,loaded);
        var actual=restored.ToArtifact().ToInterchangeEnvelope();
        var presentation=Assert.IsType<VisualArtifactInterchangePresentation>(loaded.Presentation);
        var restoredPolicy=Assert.IsType<VisualTurbulence>(presentation.TurbulenceSource);
        Assert.Equal(2,loaded.Version);Assert.Equal(seed,restoredPolicy.Seed);
        Assert.Equal(octaves,restoredPolicy.NumberOfOctaves);
        Assert.Equal(policy.BaseFrequencyX,restoredPolicy.BaseFrequencyX);
        Assert.Equal(policy.BaseFrequencyY,restoredPolicy.BaseFrequencyY);
        Assert.True(restoredPolicy.StitchTiles);
        Assert.Equal(VisualFilterColorSpace.LinearRgb,restoredPolicy.ColorSpace);
        Assert.Equal("noise-save",actual.Id);Assert.Equal("sensor/noise",actual.Extensions["raw-origin"]);
        Assert.Equal("Procedural sample",actual.AccessibleName);
        Assert.Equal(first.ToPng(),restored.ToPng());
        var again=VisualArtifactInterchangeEnvelope.FromJson(actual.ToJson()).PrepareTurbulenceSource(context);
        Assert.Equal(restored.ToSvg(),again.ToSvg());Assert.Equal(restored.ToPng(),again.ToPng());
        var retained=restored.ToSvg();loaded.Extensions["raw-origin"]="changed";
        presentation.TurbulenceSource=new VisualTurbulence(0);
        Assert.Equal(retained,restored.ToSvg());Assert.Equal(seed,restored.ToArtifact().ToInterchangeEnvelope().Presentation!.TurbulenceSource!.Seed);
    }

    [Fact]
    public void FreshContextOwnsFrameInsetsPaintFontAndExplicitEmptyHeadingsWithoutReconstructingOldEvidence() {
        var oldContext=TurbulencePortableFixture.Context();
        var source=new VisualTurbulence(.071,.113,2,13,VisualTurbulenceType.FractalNoise).Prepare(oldContext);
        var saved=source.ToArtifact().ToInterchangeEnvelope();
        var theme=VisualTheme.Graphite().Configure(builder=>builder.Dark.Background=ChartColor.FromHex("#102030"));
        var fresh=new VisualRenderContext(new VisualLayoutOptions(oldContext.Layout.Size,new ChartPadding(7,9,11,13)),
            theme,VisualThemeMode.Dark,new VisualFrame("", "",transparentBackground:true));
        var result=saved.PrepareTurbulenceSource(fresh);
        TurbulencePortableFixture.Save("portable-noise-fresh-context",result,saved);
        var surface=Assert.Single(result.Scene.Nodes.OfType<VisualSceneProceduralSource>());
        Assert.Equal(new ChartRect(7,9,110,74),surface.Bounds);
        var actual=result.ToArtifact().ToInterchangeEnvelope();
        Assert.Equal("",actual.Title);Assert.Equal("",actual.Subtitle);
        Assert.Equal("#102030",actual.Presentation!.Theme!.Background);
        Assert.NotEqual(source.ToSvg(),result.ToSvg());
        Assert.Throws<ArgumentException>(()=>saved.PrepareTurbulenceSource(new VisualRenderContext(
            new VisualLayoutOptions(new VisualSize(127,96),0))));
    }

    [Theory]
    [InlineData("topology")][InlineData("hierarchy")][InlineData("sequence")][InlineData("railroad")]
    [InlineData("tree")][InlineData("waterfall")][InlineData("journey")][InlineData("timeline")]
    public void ExistingTypedModelImportersCannotSilentlyDiscardTheProceduralSource(string family) {
        var envelope=TurbulencePortableFixture.Envelope();
        var error=Assert.Throws<NotSupportedException>(()=>{
            switch(family) {
                case "topology":_=envelope.ToTopologyChart();break;
                case "hierarchy":_=envelope.ToTopologyHierarchySource();break;
                case "sequence":_=envelope.ToSequenceArtifact();break;
                case "railroad":_=envelope.ToRailroadDiagramBlock();break;
                case "tree":_=envelope.ToTreeViewBlock();break;
                case "waterfall":_=envelope.ToWaterfallChart();break;
                case "journey":_=envelope.ToJourneyDiagramBlock();break;
                case "timeline":_=envelope.ToTimelineDiagramBlock();break;
            }
        });
        Assert.Contains("PrepareTurbulenceSource",error.Message);
    }

    [Fact]
    public void RootAndTerminalAppearanceReplayUseOneChainAndFreshSourceFacts() {
        var context=TurbulencePortableFixture.Context();
        var source=new VisualTurbulence(.071,.113,2,13,VisualTurbulenceType.FractalNoise).Prepare(context);
        var pixels=new byte[128*96*4];for(var index=3;index<pixels.Length;index+=4)pixels[index]=128;
        var root=source.WithDropShadow(new VisualDropShadow(ChartColor.Black,2,2,0,orderedDither:false,opacity:.2))
            .WithMask(new VisualMask(new RgbaImage(128,96,pixels),mode:VisualMaskMode.Alpha,orderedDither:false));
        var original=root.ToArtifact().ToInterchangeEnvelope();
        Assert.Throws<NotSupportedException>(()=>original.PrepareTurbulenceSource(context));
        var editable=VisualArtifactInterchangeEnvelope.FromJson(original.ToJson());
        editable.Presentation!.CapturedEffects.Clear();
        editable.Extensions["edit-revision"]="2";
        var bare=editable.PrepareTurbulenceSource(context);
        var replay=original.ApplyCapturedEffects(bare);
        TurbulencePortableFixture.Save("portable-noise-root-replay",replay,original);
        Assert.Equal(root.ToPng(),replay.ToPng());Assert.Equal(2,replay.ToArtifact().ToInterchangeEnvelope().Presentation!.CapturedEffects.Count);
        Assert.Equal("2",replay.ToArtifact().ToInterchangeEnvelope().Extensions["edit-revision"]);
        var layer=root.CaptureLayer(new VisualLayerBlur(1,orderedDither:false),new VisualRenderOptions(supersampling:1));
        var terminal=layer.ToArtifact().ToInterchangeEnvelope();
        var restored=terminal.ApplyCapturedLayer(bare);
        Assert.Equal(layer.Bounds,restored.Bounds);Assert.Equal(layer.ToImage().Pixels,restored.ToImage().Pixels);
        Assert.Equal("2",restored.ToArtifact().ToInterchangeEnvelope().Extensions["edit-revision"]);
        Assert.Throws<OperationCanceledException>(()=>terminal.ApplyCapturedLayer(bare,new CancellationToken(true)));
        var before=replay.ToPng();original.Presentation!.CapturedEffects[1].Mask!.Pixels[3]=0;
        Assert.Equal(before,replay.ToPng());
    }

    [Fact]
    public void UnknownScalarsDowngradeContradictionsAndForgedSourceFactsReject() {
        var prepared=new VisualTurbulence(.071,.113,2,13).Prepare(TurbulencePortableFixture.Context());
        var envelope=prepared.ToArtifact().ToInterchangeEnvelope();var json=envelope.ToJson();
        TurbulencePortableFixture.Save("portable-noise-invalid-controls",prepared,envelope);
        Assert.Throws<NotSupportedException>(()=>VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2","\"version\":1",StringComparison.Ordinal)));
        Assert.Throws<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"seed\":13","\"seed\":13,\"unknownSource\":true",StringComparison.Ordinal)));
        var frequencyToken="\"baseFrequencyX\":"+.071.ToString("G17",System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains(frequencyToken,json);
        Assert.Throws<ArgumentOutOfRangeException>(()=>VisualArtifactInterchangeEnvelope.FromJson(json.Replace(frequencyToken,"\"baseFrequencyX\":-1",StringComparison.Ordinal)));
        var wrong=VisualArtifactInterchangeEnvelope.FromJson(json);
        var wrongPresentation=Assert.IsType<VisualArtifactInterchangePresentation>(wrong.Presentation);
        wrongPresentation.TurbulenceSource=new VisualTurbulence(.072,.113,2,13);
        Assert.Throws<ArgumentException>(()=>prepared.ToArtifact(wrong.Id,wrong.Kind,wrong));
        wrongPresentation.TurbulenceSource=null;
        Assert.Throws<ArgumentException>(()=>prepared.ToArtifact(wrong.Id,wrong.Kind,wrong));
        var plain=new VisualSceneBuilder(new VisualSize(128,96),ChartForgeX.Typography.FontSpec.SystemSans());
        plain.Rect(new ChartRect(0,0,128,96),ChartColor.White);
        Assert.Throws<ArgumentException>(()=>new PreparedVisual(plain.Build()).ToArtifact(envelope.Id,envelope.Kind,envelope));
        envelope.Nodes.Add(new VisualArtifactInterchangeNode { Id="extra",Label="Do not discard" });
        Assert.Throws<ArgumentException>(()=>envelope.Validate());
    }

    [Fact]
    public void ExhaustedFrameStillRetainsItsPureSourceAndOrdinarySourceAbsenceAddsNoFacts() {
        // A valid viewport can exhaust its content through an explicitly authored large frame gap.
        // Do not create contradictory outer insets merely to force a zero-area procedural source.
        var theme=VisualTheme.Graphite().Configure(builder=>builder.Spacing=128);
        var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(64,96),4),theme,
            frame:new VisualFrame("No source room",showLegend:false));
        var source=new VisualTurbulence(0).Prepare(context);
        var envelope=source.ToArtifact().ToInterchangeEnvelope();
        var restored=envelope.PrepareTurbulenceSource(context);
        TurbulencePortableFixture.Save("portable-noise-empty",restored,envelope);
        Assert.Empty(source.Scene.Nodes.OfType<VisualSceneProceduralSource>());
        Assert.Contains(source.Diagnostics,diagnostic=>diagnostic.Code=="frame.insufficient-space");
        Assert.NotNull(envelope.Presentation!.TurbulenceSource);
        Assert.Equal(source.ToSvg(),restored.ToSvg());Assert.Equal(source.ToPng(),restored.ToPng());
        var ordinary=SequenceArtifact.Create("ordinary").AddParticipant("A").AddParticipant("B").AddMessage("A","B","Save");
        Assert.DoesNotContain("turbulenceSource",ordinary.ToVisualArtifact().ToInterchangeJson());
    }
}

internal static class TurbulencePortableFixture {
    internal static VisualRenderContext Context()=>new(new VisualLayoutOptions(new VisualSize(128,96),6),
        frame:new VisualFrame("Noise","Captured parameters",showLegend:false,transparentBackground:true));
    internal static VisualArtifactInterchangeEnvelope Envelope()=>new VisualTurbulence(.071,.113,2,13)
        .Prepare(Context()).ToArtifact().ToInterchangeEnvelope();
    internal static void Save(string stem,PreparedVisual source,VisualArtifactInterchangeEnvelope envelope) {
        var root=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(root))return;
        Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,stem+".json"),envelope.ToJson());
        File.WriteAllText(Path.Combine(root,stem+".native.svg"),source.ToSvg(new VisualSvgOptions(effectProfile:VisualSvgEffectProfile.NativeFilters)));
        File.WriteAllBytes(Path.Combine(root,stem+".png"),source.ToPng());
    }
}
