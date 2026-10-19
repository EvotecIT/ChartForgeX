using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterComputedPaintTests {
    [Theory]
    [InlineData("color='#0000ff' fill='currentColor' stroke='currentColor' style='color:#ff0000!important'")]
    [InlineData("style='fill:currentColor;stroke:currentColor;color:#ff0000'")]
    [InlineData("style='--ink:currentColor;fill:var(--ink);stroke:var(--ink);color:#ff0000!important'")]
    public void FillAndStrokeUseTheFinalColorWithoutChangingTheirIndependentOpacity(string paint) {
        var actual = Render("<rect x='4' y='4' width='24' height='24' stroke-width='4' fill-opacity='.5' stroke-opacity='.25' " + paint + "/>");
        Assert.Equal(Render("<rect x='4' y='4' width='24' height='24' stroke-width='4' fill-opacity='.5' stroke-opacity='.25' fill='#ff0000' stroke='#ff0000'/>"), actual);
    }

    [Fact]
    public void StylesheetCurrentColorUsesTheImportantColorWinnerBeforePaint() {
        Assert.Equal(Render("<rect width='32' height='32' fill='#ff0000'/>"),
            Render("<style>rect{fill:currentColor!important;color:#ff0000!important}</style><rect width='32' height='32' style='color:#0000ff'/>") );
    }

    [Fact]
    public void InheritedCurrentColorFollowsTheChildColorWhileLiteralPaintRetainsItsComputedValue() {
        var actual = Render("<g color='#ff0000' fill='currentColor' stroke='currentColor' stroke-width='2'>" +
            "<rect x='2' y='2' width='12' height='28' color='#0000ff'/>" +
            "<rect x='18' y='2' width='12' height='28' color='#0000ff' fill='#ff0000' stroke='#ff0000'/></g>");
        Assert.Equal(Render("<rect x='2' y='2' width='12' height='28' fill='#0000ff' stroke='#0000ff' stroke-width='2'/>" +
            "<rect x='18' y='2' width='12' height='28' fill='#ff0000' stroke='#ff0000' stroke-width='2'/>"), actual);
    }

    [Fact]
    public void CurrentColorOnColorUsesTheInheritedColorRatherThanTheLosingLocalDeclaration() {
        Assert.Equal(Render("<rect width='32' height='32' fill='#00ff00'/>"),
            Render("<g color='#00ff00'><rect width='32' height='32' fill='currentColor' style='color:#ff0000;color:currentColor!important'/></g>"));
    }

    [Theory]
    [InlineData("<g style='--paint:#ff0000;--ink:var(--paint)!important'>")]
    [InlineData("<style>g{--paint:#ff0000;--ink:var(--paint)!important}</style><g>")]
    public void InheritedAliasesFreezeBeforeChildOverridesAndChildLocalAliasesStillWin(string parent) {
        var actual = Render(parent + "<rect width='16' height='32' style='--paint:#0000ff;fill:var(--ink)'/>" +
            "<rect x='16' width='16' height='32' style='--paint:#0000ff;--ink:var(--paint);fill:var(--ink)'/></g>");
        Assert.Equal(Render("<rect width='16' height='32' fill='#ff0000'/><rect x='16' width='16' height='32' fill='#0000ff'/>"), actual);
    }

    [Fact]
    public void GradientStopsInheritComputedAliasesButTheirOwnNormalAliasesCanOverrideThem() {
        var actual = Render("<style>linearGradient{--paint:#ff0000;--ink:var(--paint)!important}stop{--paint:#0000ff}</style>" +
            "<defs><linearGradient id='red'><stop offset='0' style='stop-color:var(--ink)'/></linearGradient>" +
            "<linearGradient id='blue'><stop offset='0' style='--ink:var(--paint);stop-color:var(--ink)'/></linearGradient></defs>" +
            "<rect width='16' height='32' fill='url(#red)'/><rect x='16' width='16' height='32' fill='url(#blue)'/>");
        Assert.Equal(Render("<rect width='16' height='32' fill='#ff0000'/><rect x='16' width='16' height='32' fill='#0000ff'/>"), actual);
    }

    [Fact]
    public void FrozenFallbacksAndUnavailableCyclicOrMissingAliasesCannotRebindInChildren() {
        var actual = Render("<g style='--ink:var(--absent,#ff0000);--missing:var(--absent);--a:var(--b);--b:var(--a)'>" +
            "<rect width='8' height='32' style='--absent:#0000ff;fill:var(--ink)'/>" +
            "<rect x='8' width='8' height='32' style='--absent:#0000ff;fill:var(--missing,#00ff00)'/>" +
            "<rect x='16' width='8' height='32' style='--b:#0000ff;fill:var(--a,#00ff00)'/>" +
            "<rect x='24' width='8' height='32' style='fill:var(--unknown,var(--ink))'/></g>");
        Assert.Equal(Render("<rect width='8' height='32' fill='#ff0000'/><rect x='8' width='16' height='32' fill='#00ff00'/><rect x='24' width='8' height='32' fill='#ff0000'/>"), actual);
    }

    private static byte[] Render(string body) => SvgRasterizer.ToImage(
        "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'>" + body + "</svg>").Pixels;
}
