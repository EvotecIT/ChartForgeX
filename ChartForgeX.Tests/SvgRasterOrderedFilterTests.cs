using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterOrderedFilterTests {
    [Fact]
    public void ThreeNamedStagesPreserveSubByteColorUntilTheFinalOutput() {
        var nodes = "<feFlood flood-color='rgb(.2490234375,.2490234375,.2490234375)' result='low'/>"
            + OrderedFilterFixture.Scale("low", "lift", 8) + OrderedFilterFixture.Scale("lift", null, 8);
        var result = OrderedFilterFixture.Import("sub-byte", nodes);
        Assert.Equal(new byte[] { 16, 16, 16, 255 }, OrderedFilterFixture.Pixel(result, 1, 1));
    }

    [Fact]
    public void OrderedScalarClampAndZeroAlphaNormalizationPrecedeLaterCoverageCreation() {
        var nodes = "<feFlood flood-color='rgb(191.25,0,0)' result='color'/>"
            + OrderedFilterFixture.Scale("color", "high", 2) + OrderedFilterFixture.Scale("high", null, .5);
        Assert.Equal(new byte[] { 128, 0, 0, 255 }, OrderedFilterFixture.Pixel(OrderedFilterFixture.Import("stage-clamp", nodes), 1, 1));
        var cleared = "<feComponentTransfer in='SourceGraphic' result='clear'><feFuncA type='linear' slope='0'/></feComponentTransfer>"
            + "<feComponentTransfer in='clear' result='opaque'><feFuncA type='linear' slope='0' intercept='1'/></feComponentTransfer>"
            + "<feComponentTransfer in='opaque' result='red'><feFuncR type='linear' slope='0' intercept='.25'/></feComponentTransfer>"
            + "<feOffset in='red'/>";
        Assert.Equal(new byte[] { 64, 0, 0, 255 }, OrderedFilterFixture.Pixel(OrderedFilterFixture.Import("stage-clear-alpha", cleared), 1, 1));
    }

    [Theory]
    [InlineData(false, 64)] [InlineData(true, 60)]
    public void TwoInputBlendConvertsRgbAtItsConsumerSpaceAndKeepsCoverageLinear(bool linearRgb, byte gray) {
        var nodes = "<feFlood flood-color='rgb(127.5,127.5,127.5)' result='a'/>"
            + "<feFlood flood-color='rgb(127.5,127.5,127.5)' result='b'/>"
            + "<feBlend in='a' in2='b' mode='multiply' color-interpolation-filters='" + (linearRgb ? "linearRGB" : "sRGB") + "'/>";
        Assert.Equal(new byte[] { gray, gray, gray, 255 }, OrderedFilterFixture.Pixel(OrderedFilterFixture.Import("mixed-space-" + linearRgb, nodes), 1, 1));
    }

    [Theory]
    [InlineData("sRGB", 128)] [InlineData("linearRGB", 188)] [InlineData(null, 188)]
    public void NamedGaussianRetainsItsWorkingSpaceUntilTheFinalRgbTransfer(string? space, byte purple) {
        var source = OrderedFilterFixture.Source("<feGaussianBlur in='SourceGraphic' result='mixed' stdDeviation='1'/>")
            .Replace("<rect id='source' width='4' height='3' fill='blue' filter='url(#f)'/>",
                "<g id='source' filter='url(#f)'><rect x='1' y='1' width='1' height='1' fill='red'/><rect x='2' y='1' width='1' height='1' fill='blue'/></g>", StringComparison.Ordinal);
        source = space == null ? source.Replace(" color-interpolation-filters='sRGB'", "", StringComparison.Ordinal)
            : source.Replace("color-interpolation-filters='sRGB'", "color-interpolation-filters='" + space + "'", StringComparison.Ordinal);
        OrderedFilterFixture.SaveSource("gaussian-working-space-" + (space ?? "default"), source);
        var result = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        OrderedFilterFixture.Capture("gaussian-working-space-" + (space ?? "default"), source, result);
        Assert.Empty(result.Diagnostics); Assert.Equal(new byte[] { purple, 0, purple, 57 }, OrderedFilterFixture.Pixel(result, 1, 1));
    }

    [Fact]
    public void FanoutNeverMutatesAnEarlierResultAndArithmeticAddsTheActualPremultipliedInputs() {
        var fanout = "<feFlood flood-color='red' flood-opacity='.5' result='red'/><feOffset in='red' dx='1' result='moved'/>"
            + "<feComposite in='red' in2='moved' operator='xor'/>";
        var result = OrderedFilterFixture.Import("earlier-result-fanout", fanout);
        for (var x = 0; x < 4; x++) Assert.Equal(new byte[] { 255, 0, 0, 128 }, OrderedFilterFixture.Pixel(result, x, 1));
        var sum = "<feFlood flood-color='red' flood-opacity='.5' result='red'/><feFlood flood-color='blue' flood-opacity='.5' result='blue'/>"
            + "<feComposite in='red' in2='blue' operator='arithmetic' k2='1' k3='1'/>";
        Assert.Equal(new byte[] { 128, 0, 128, 255 }, OrderedFilterFixture.Pixel(OrderedFilterFixture.Import("arithmetic-sum", sum), 1, 1));
    }

    [Theory]
    [InlineData(false, 64, 128)] [InlineData(true, 128, 64)]
    public void ThreeMergeInputsPaintInTheirAuthoredBottomToTopOrder(bool reverse, byte red, byte green) {
        var nodes = "<feFlood flood-color='blue' result='blue'/><feFlood flood-color='red' flood-opacity='.5' result='red'/>"
            + "<feFlood flood-color='lime' flood-opacity='.5' result='green'/><feMerge><feMergeNode in='blue'/>"
            + "<feMergeNode in='" + (reverse ? "green" : "red") + "'/><feMergeNode in='" + (reverse ? "red" : "green") + "'/></feMerge>";
        Assert.Equal(new byte[] { red, green, 64, 255 }, OrderedFilterFixture.Pixel(OrderedFilterFixture.Import("merge-order-" + reverse, nodes), 1, 1));
    }

    [Theory]
    [InlineData("duplicate", "<feFlood flood-color='red' result='a'/><feFlood flood-color='blue' result='a'/><feOffset in='a'/>", 0, 0, 255)]
    [InlineData("missing", "<feFlood flood-color='red' result='a'/><feOffset in='missing' result='b'/><feOffset in='b'/>", 255, 0, 0)]
    [InlineData("forward", "<feFlood flood-color='red' result='a'/><feOffset in='later' result='before'/><feFlood flood-color='blue' result='later'/><feOffset in='before'/>", 255, 0, 0)]
    public void DuplicateNamesUseTheNewestPrecedingProducerAndMissingOrForwardNamesUseTheOmittedInput(string key, string nodes, byte red, byte green, byte blue) {
        Assert.Equal(new byte[] { red, green, blue, 255 }, OrderedFilterFixture.Pixel(OrderedFilterFixture.Import("result-binding-" + key, nodes), 1, 1));
    }

    [Theory]
    [InlineData("source-alpha", "<feColorMatrix in='SourceAlpha'/>", 0, 0, 0)]
    [InlineData("region", "<feColorMatrix in='SourceGraphic' x='0'/>", 0, 0, 255)]
    [InlineData("branch", "<feColorMatrix in='SourceGraphic'/><feColorMatrix in='SourceGraphic'/>", 0, 0, 255)]
    [InlineData("reused", "<feColorMatrix in='SourceGraphic' result='a'/><feColorMatrix in='a' result='a'/><feColorMatrix in='a'/>", 0, 0, 255)]
    [InlineData("blur", "<feColorMatrix in='SourceGraphic' result='a'/><feGaussianBlur in='a' stdDeviation='1'/>", 0, 0, 255)]
    [InlineData("default", "<feOffset/>", 0, 0, 255)]
    public void NewlyRepresentedInputAndPrimitiveMembershipHasPositiveNativeControls(string key, string nodes, byte red, byte green, byte blue) {
        var result = OrderedFilterFixture.Import("supported-membership-" + key, nodes);
        Assert.Empty(result.Diagnostics); Assert.Equal(new byte[] { red, green, blue, 255 }, OrderedFilterFixture.Pixel(result, 1, 1));
    }

    [Fact]
    public void DeclaredSubregionUnionPrecedesTheInputAndOutputHardClips() {
        var nodes = "<feFlood flood-color='red' x='-2' width='1' result='left'/><feFlood flood-color='blue' x='5' width='1' result='right'/>"
            + "<feComposite in='left' in2='right' operator='arithmetic' k4='.25'/>";
        var generated = OrderedFilterFixture.Import("subregion-union-generation", nodes);
        for (var y = 0; y < 3; y++) for (var x = 0; x < 4; x++)
            Assert.Equal(new byte[] { 255, 255, 255, 64 }, OrderedFilterFixture.Pixel(generated, x, y));
        var clipped = "<feFlood flood-color='white' x='-10000' width='20000' result='wide'/><feOffset in='wide' x='1' width='2'/>";
        var result = OrderedFilterFixture.Import("primitive-output-clip", clipped);
        Assert.Equal(new byte[4], OrderedFilterFixture.Pixel(result, 0, 1)); Assert.Equal(new byte[4], OrderedFilterFixture.Pixel(result, 3, 1));
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, OrderedFilterFixture.Pixel(result, 1, 1));
        var source = "<svg xmlns='http://www.w3.org/2000/svg' width='4' height='4'><defs><filter id='f' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse'"
            + " x='1' y='1' width='2' height='2' color-interpolation-filters='sRGB'><feGaussianBlur in='SourceGraphic' result='frame' stdDeviation='0'/></filter></defs>"
            + "<rect width='4' height='4' fill='red' filter='url(#f)'/></svg>";
        OrderedFilterFixture.SaveSource("standard-input-hard-clip", source);
        var hardClip = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        OrderedFilterFixture.Capture("standard-input-hard-clip", source, hardClip);
        Assert.Empty(hardClip.Diagnostics); Assert.Equal(new byte[4], OrderedFilterFixture.Pixel(hardClip, 0, 1));
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, OrderedFilterFixture.Pixel(hardClip, 1, 1));
    }

    [Fact]
    public void SeventeenStagesUseOneApplicationAndShareDocumentBytesWorkAndCancellation() {
        var nodes = "<feFlood flood-color='white' result='s0'/>" + string.Concat(Enumerable.Range(1, 16).Select(index =>
            "<feOffset in='s" + (index - 1) + "' result='s" + index + "'/>"));
        var source = OrderedFilterFixture.Source(nodes);
        OrderedFilterFixture.SaveSource("seventeen-stages", source);
        var accepted = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions(maximumApplications: 1));
        OrderedFilterFixture.Capture("seventeen-stages", source, accepted); Assert.Empty(accepted.Diagnostics);
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions(maximumLiveBytes: 1000)));
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions(maximumWorkSamples: 8000)));
        Assert.Empty(SvgRasterizer.Rasterize(OrderedFilterFixture.Source("<feFlood flood-color='white'/>"), strict: true,
            filters: new SvgRasterFilterOptions(maximumWorkSamples: 8000)).Diagnostics);
        var repeated = source.Replace("</svg>", "<use href='#source'/></svg>", StringComparison.Ordinal);
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(repeated, filters: new SvgRasterFilterOptions(maximumApplications: 1)));
        Assert.Throws<OperationCanceledException>(() => SvgRasterizer.Rasterize(source,
            filters: new SvgRasterFilterOptions(cancellationToken: new CancellationToken(true))));
        Assert.Contains(SvgRasterizer.Rasterize(source).Diagnostics, item => item.Code == "SFR002");
    }

    [Theory]
    [InlineData("2 2")] [InlineData("4")]
    public void AuthoredResolutionIsLocatedUntilItsSamplingGridIsRepresented(string resolution) {
        var source = OrderedFilterFixture.Source("<feGaussianBlur in='SourceGraphic' stdDeviation='1' edgeMode='none'/>")
            .Replace("<filter id='f'", "<filter id='f' filterRes='" + resolution + "'", StringComparison.Ordinal);
        OrderedFilterFixture.SaveSource("unsupported-resolution-" + resolution.Replace(' ', '-'), source);
        Assert.Contains(SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions()).Diagnostics, item => item.Code == "SFR002" && item.ElementId == "source");
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions()));
    }

    [Theory]
    [InlineData("BackgroundImage")] [InlineData("BackgroundAlpha")] [InlineData("FillPaint")] [InlineData("StrokePaint")]
    public void UncapturedBuiltInsRemainLocatedInsteadOfBecomingAZeroOrBackdropPromise(string input) {
        var source = OrderedFilterFixture.Source("<feOffset in='" + input + "'/>");
        OrderedFilterFixture.SaveSource("unsupported-input-" + input, source);
        Assert.Contains(SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions()).Diagnostics, item => item.Code == "SFR002" && item.ElementId == "source");
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions()));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrderedOutputStillPrecedesItsOwnClipMaskAndOpacity(bool group) {
        var filter = "<filter id='f' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='4' height='3' color-interpolation-filters='sRGB'>"
            + "<feFlood flood-color='blue' flood-opacity='.5' result='tint'/><feComposite in='SourceGraphic' in2='tint' operator='in' result='masked'/><feOffset in='masked'/></filter>";
        const string own = "filter='url(#f)' clip-path='url(#clip)' mask='url(#mask)' opacity='.5'";
        var rect = "<rect width='4' height='3' fill='red' fill-opacity='.5019607843137255' " + (group ? "" : own) + "/>";
        var source = "<svg xmlns='http://www.w3.org/2000/svg' width='4' height='3'><defs>" + filter
            + "<clipPath id='clip' clipPathUnits='userSpaceOnUse'><rect x='2' width='2' height='3'/></clipPath>"
            + "<mask id='mask' maskUnits='userSpaceOnUse' maskContentUnits='userSpaceOnUse' x='0' y='0' width='4' height='3' mask-type='alpha'>"
            + "<rect width='4' height='3' fill='white' fill-opacity='.5019607843137255'/></mask></defs>"
            + (group ? "<g " + own + ">" + rect + "</g>" : rect) + "</svg>";
        var result = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        OrderedFilterFixture.Capture("outer-clip-mask-opacity-" + group, source, result);
        Assert.Empty(result.Diagnostics); Assert.Equal(new byte[4], OrderedFilterFixture.Pixel(result, 1, 1));
        Assert.Equal(new byte[] { 255, 0, 0, 16 }, OrderedFilterFixture.Pixel(result, 2, 1));
    }

    [Theory]
    [InlineData("outline")] [InlineData("bloom")]
    public void ActualNativeFiltersEmissionAndCapturedEffectsV2ReplayReachTheOrderedOwner(string kind) {
        var source = RetainedAppearanceFixture.Source(white: kind == "bloom");
        var root = kind == "outline" ? source.WithOutline(new VisualOutline(ChartColor.FromRgb(0, 0, 255), 1, false))
            : source.WithBloom(new VisualBloom(1, .1, .7, orderedDither: false));
        var options = new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters); var svg = root.ToSvg(options);
        var replayed = VisualArtifactInterchangeEnvelope.FromJson(root.ToArtifact().ToInterchangeJson()).ApplyCapturedEffects(source);
        Assert.Equal(svg, replayed.ToSvg(options));
        OrderedFilterFixture.SaveSource("emitted-" + kind, svg);
        var result = SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions());
        OrderedFilterFixture.Capture("emitted-" + kind, svg, result); Assert.Empty(result.Diagnostics);
        Assert.Equal(kind == "outline" ? new byte[] { 0, 0, 255, 128 } : new byte[] { 255, 255, 255, 20 }, OrderedFilterFixture.Pixel(result, 1, 1));
        Assert.Equal(kind == "outline" ? new byte[] { 255, 0, 0, 128 } : new byte[] { 255, 255, 255, 255 }, OrderedFilterFixture.Pixel(result, 3, 2));
        Assert.Equal(new byte[4], OrderedFilterFixture.Pixel(result, 0, 0));
    }
}
