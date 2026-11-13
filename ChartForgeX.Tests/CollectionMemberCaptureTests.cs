using ChartForgeX.Raster;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects copying one face out of a font collection for portable owned capture.</summary>
public sealed class CollectionMemberCaptureTests {
    [Fact]
    public void AMemberOverTheLimitOrWithATableOutsideTheCollectionIsNotCopied() {
        var collection = OpenTypeTestFonts.Collection(Archivo(), Archivo());
        var face = TrueTypeFont.TryLoad(collection, 1)!;
        var member = face.CopyCollectionMember(OwnedFontResources.MaximumRootBytes)!;
        Assert.Equal(face.DisplayName, member.DisplayName);
        Assert.Null(face.CopyCollectionMember(member.CapturedByteLength - 1));

        // The loader keeps a face whose unused 'prep' record runs past the file; copying it must decline, not throw.
        var directory = (collection[16] << 24) | (collection[17] << 16) | (collection[18] << 8) | collection[19];
        var tables = (collection[directory + 4] << 8) | collection[directory + 5];
        var prep = directory + 12 + (tables - 1) * 16;
        Assert.Equal("prep", System.Text.Encoding.ASCII.GetString(collection, prep, 4));
        collection[prep + 12] = 0x00; collection[prep + 13] = 0xF0; collection[prep + 14] = 0x00; collection[prep + 15] = 0x00;
        var damaged = TrueTypeFont.TryLoad(collection, 1)!;
        Assert.Null(damaged.CopyCollectionMember(int.MaxValue));
    }

    private static byte[] Archivo() {
        using var stream = typeof(ChartFontAssets).Assembly.GetManifestResourceStream("ChartForgeX.Typography.Fonts.Archivo.ttf")!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray();
    }
}
