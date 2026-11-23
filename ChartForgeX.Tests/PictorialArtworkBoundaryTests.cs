using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PictorialArtworkBoundaryTests {
    [Fact]
    public void InvalidOrUnsupportedSvgRejectsWithoutInstallingPartialArtwork() {
        var chart = PictorialArtworkTests.Model(.5); var before = chart.ToSvg();
        Assert.Throws<FormatException>(() => chart.Pictorial(p => p.Artwork(ChartPictorialArtwork.FromSvg("<!DOCTYPE svg [<!ENTITY data 'unsafe'>]><svg>&data;</svg>", 8, 4))));
        Assert.Throws<NotSupportedException>(() => chart.Pictorial(p => p.Artwork(ChartPictorialArtwork.FromSvg("<svg xmlns='http://www.w3.org/2000/svg' width='8' height='4'><foreignObject width='8' height='4'><div>Host content</div></foreignObject></svg>", 8, 4))));
        Assert.Throws<NotSupportedException>(() => chart.Pictorial(p => p.Artwork(ChartPictorialArtwork.FromSvg("<svg xmlns='http://www.w3.org/2000/svg' width='8' height='4'><image href='https://example.invalid/private.png' width='8' height='4'/></svg>", 8, 4))));
        Assert.Null(chart.Options.Pictorial.Artwork); Assert.Equal(before, chart.ToSvg());
    }

    [Fact]
    public void ArtworkDimensionsAndDirectionRejectBeforeAllocationOrMutation() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartPictorialArtwork.FromSvg("not parsed", 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartPictorialArtwork.FromSvg("not parsed", 8193, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartPictorialArtwork.FromSvg("not parsed", 5000, 4000));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartPictorialArtwork.FromImage(default));
        var chart = PictorialArtworkTests.Model(.5); var before = chart.ToSvg();
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Pictorial(p => p.FillDirection((ChartPictorialFillDirection)99)));
        Assert.Equal(ChartPictorialFillDirection.LeftToRight, chart.Options.Pictorial.FillDirection); Assert.Equal(before, chart.ToSvg());
    }
}
