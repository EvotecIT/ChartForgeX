using System;
using System.IO;
using System.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeEffectWireBudgetBeforeControls {
    [Fact]
    public void ExistingNativeMaskAndImplicitArtifactDoNotRequireTheMaskToFitJson() {
        var source = NativeEffectWireFixture.Source();
        var mask = NativeEffectWireFixture.Mask(1024,1537);
        var native = source.WithMask(mask);
        var artifact = native.ToArtifact();
        var svg = artifact.ToSvg();var png = artifact.ToPng();
        NativeEffectWireFixture.Save("native-wire-before",svg,png);
        Assert.Contains("<image",svg);Assert.True(png.Length>64);
        Assert.Equal(native.ToPng(),png);
        Assert.Equal(source.Size.Width,artifact.NaturalSize!.Value.Width);
        Assert.Equal(source.Size.Height,artifact.NaturalSize.Value.Height);
    }
}

internal static class NativeEffectWireFixture {
    internal static PreparedVisual Source(bool largeFacts = false, string? factText = null, int factCount = 45) {
        var model = SequenceArtifact.Create("native-wire").WithSize(720,640)
            .AddParticipant("A","Review").AddParticipant("B","Archive").AddMessage("A","B","Save");
        if (largeFacts) {
            var text = factText ?? new string('x',65536);
            for (var index=0;index<factCount;index++) model.Metadata.Add("fact-"+index,text);
        }
        return model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720,640),24)));
    }

    internal static VisualMask Mask(int width,int height) {
        var pixels = new byte[checked(width*height*4)];
        for (var offset=3;offset<pixels.Length;offset+=4) pixels[offset]=255;
        return new VisualMask(new RgbaImage(width,height,pixels),mode:VisualMaskMode.Alpha,orderedDither:false);
    }

    internal static void Save(string stem,string svg,byte[] png) {
        var root=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if(string.IsNullOrWhiteSpace(root))return;
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root,stem+".svg"),svg);
        File.WriteAllBytes(Path.Combine(root,stem+".png"),png);
    }
}
