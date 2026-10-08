using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Memory registrations use the same measurement, rendering, style and fallback paths as file registrations.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class FontRegistryMemoryTests : IDisposable {
    private const string Family = "CFX Memory";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MemoryRegistrationMatchesFileMeasurementAndSvgAndPng(bool fromStream) {
        var bytes = OpenTypeTestFonts.NameKeyed();
        var path = Path.Combine(Path.GetTempPath(), "cfx-memory-" + Guid.NewGuid().ToString("N") + ".otf");
        File.WriteAllBytes(path, bytes);
        try {
            FontRegistry.Register(Family, path);
            var chart = Chart.Create().WithSize(360, 220).WithTitle("ChartForgeX 0123456789")
                .WithTheme(ChartTheme.ReportLight().WithFontFamily(Family))
                .WithXLabels("H", "O").AddBar("H", ChartPoints.FromValues(12, 18));
            var png = chart.ToPng();
            var svg = chart.ToSvg();
            var raster = SvgRasterizer.ToImage(svg).Pixels;
            var measured = VisualCanvasTextFace.Resolve(Family, 400, TextMeasurementMode.PortableEstimate).Measure("HOx", 20);
            FontRegistry.Clear();

            using var stream = new MemoryStream(bytes);
            if (fromStream) FontRegistry.Register(Family, stream);
            else FontRegistry.Register(Family, bytes);
            Assert.Null(TypographyFontResolver.ResolveFace(Family, 400, false).Path);
            Assert.Equal(measured, VisualCanvasTextFace.Resolve(Family, 400, TextMeasurementMode.PortableEstimate).Measure("HOx", 20));
            Assert.Equal(png, chart.ToPng());
            Assert.Equal(svg, chart.ToSvg());
            Assert.Equal(raster, SvgRasterizer.ToImage(chart.ToSvg()).Pixels);
            Assert.True(stream.CanRead);
            if (fromStream) Assert.Equal(stream.Length, stream.Position);
        } finally {
            FontRegistry.Clear();
            File.Delete(path);
        }
    }

    [Fact]
    public void BytesAreOwnedAndReplacingACollectionFaceInvalidatesResolution() {
        var bytes = OpenTypeTestFonts.Collection(OpenTypeTestFonts.NameKeyed(), OpenTypeTestFonts.NameKeyed(weight: 700, italic: true));
        FontRegistry.Register(Family, bytes, weight: 700, italic: true, collectionIndex: 1);
        var first = TypographyFontResolver.ResolveFace(Family, 700, true);
        Assert.Equal(1, first.Font!.CollectionIndex);
        Assert.False(first.SynthesizeBold);
        Assert.False(first.SynthesizeItalic);
        Array.Clear(bytes, 0, bytes.Length);
        Assert.Equal(12, first.Font.Measure("H", 20));

        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(weight: 700, italic: true), weight: 700, italic: true);
        Assert.NotSame(first.Font.Root, TypographyFontResolver.ResolveFace(Family, 700, true).Font!.Root);
        Assert.Single(FontRegistry.Families);
        FontRegistry.Clear();
        Assert.Empty(FontRegistry.Families);
        Assert.NotSame(first.Font.Root, TypographyFontResolver.ResolveFace(Family, 700, true).Font?.Root);
    }

    [Fact]
    public void MemoryFallbackWorksForDistinctFacesWithNoPaths() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(includePrivateUse: false));
        var primary = TypographyFontResolver.ResolveFace(Family, 400, false).Font!;
        Assert.Null(FontFallbackChain.For(primary).FaceFor(OpenTypeTestFonts.PrivateUseCharacter));
        FontRegistry.Register("CFX Memory Fallback", OpenTypeTestFonts.NameKeyed());
        var stack = TypographyFontResolver.ResolveFace(Family + ", CFX Memory Fallback", 400, false).Font!;
        Assert.Equal(new[] { "CFX Memory Fallback" }, stack.FallbackFamilies);
        var fallback = FontFallbackChain.For(stack).FaceFor(OpenTypeTestFonts.PrivateUseCharacter);
        Assert.NotNull(fallback);
        Assert.NotSame(stack.Root, fallback!.Root);
        var shaped = TextShaper.Shape(stack, "H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter));
        Assert.Same(fallback.Root, shaped[1].Face.Root);
        Assert.Equal(OpenTypeTestFonts.Box, shaped[1].Glyph);
        Assert.Equal(22, stack.Measure("H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter), 20));
    }

    [Fact]
    public void StreamStartsAtCurrentPositionAndSupportsNonSeekingReadsWithoutTakingOwnership() {
        var bytes = new byte[] { 1, 2, 3 }.Concat(OpenTypeTestFonts.NameKeyed()).ToArray();
        using var source = new MemoryStream(bytes);
        source.Position = 3;
        using var stream = new ReadOnlyForwardStream(source);
        FontRegistry.Register(Family, stream);
        Assert.True(stream.CanRead);
        Assert.Equal(source.Length, source.Position);
        source.Dispose();
        Assert.Equal(12, TypographyFontResolver.ResolveFace(Family, 400, false).Font!.Measure("H", 20));
    }

    [Fact]
    public void InvalidMemoryRegistrationsDoNotReplaceAValidFace() {
        FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());
        var face = TypographyFontResolver.ResolveFace(Family, 400, false).Font;
        Assert.Throws<ArgumentNullException>(() => FontRegistry.Register(Family, (byte[])null!));
        Assert.Throws<ArgumentNullException>(() => FontRegistry.Register(Family, (Stream)null!));
        Assert.Throws<ArgumentException>(() => FontRegistry.Register(" ", OpenTypeTestFonts.NameKeyed()));
        Assert.Throws<ArgumentOutOfRangeException>(() => FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed(), weight: 1001));
        Assert.Throws<ArgumentException>(() => FontRegistry.Register(Family, new byte[] { 1, 2, 3 }));
        Assert.Throws<ArgumentException>(() => FontRegistry.Register(Family, new MemoryStream(new byte[] { 1, 2, 3 })));
        using var disposed = new MemoryStream();
        disposed.Dispose();
        Assert.Throws<ArgumentException>(() => FontRegistry.Register(Family, disposed));
        Assert.Same(face, TypographyFontResolver.ResolveFace(Family, 400, false).Font);
    }

    public void Dispose() => FontRegistry.Clear();

    private sealed class ReadOnlyForwardStream(Stream source) : Stream {
        public override bool CanRead => source.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => source.Read(buffer, offset, count);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
