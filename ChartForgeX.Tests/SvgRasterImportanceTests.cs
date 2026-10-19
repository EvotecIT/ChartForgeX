using ChartForgeX.Svg;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterImportanceTests {
    [Fact]
    public void StylesheetImportancePrecedesInlineNormalButRetainsSpecificityAndIndependentPaint() {
        var actual = Render("<style>#n{fill:#0000ff!important}.mark{fill:#00ff00!important;stroke:#000000}rect{fill:#ff0000!important}</style>" +
            "<rect id='n' class='mark' x='4' y='4' width='24' height='24' fill='#ff0000' style='fill:#00ff00;stroke:#ffffff;stroke-width:2'/>");
        Assert.Equal(Render("<rect x='4' y='4' width='24' height='24' fill='#0000ff' stroke='#ffffff' stroke-width='2'/>"), actual);
    }

    [Fact]
    public void InlineImportanceAndLaterImportantDeclarationsWinWithoutLosingDuplicatePriority() {
        var actual = Render("<style>#n{fill:#0000ff!important}#n{fill:#ff0000!important}</style>" +
            "<rect id='n' width='32' height='32' style='fill:#ffffff ! ImPoRtAnT;fill:#000000;fill:#00ff00!important;fill:#0000ff'/>");
        Assert.Equal(Render("<rect width='32' height='32' fill='#00ff00'/>"), actual);
        Assert.Equal(Render("<rect width='32' height='32' fill='#ff0000'/>"),
            Render("<style>rect{fill:#0000ff!important}rect{fill:#ff0000!important}</style><rect width='32' height='32'/>"));
    }

    [Fact]
    public void InheritedComputedPaintAndVariablesDoNotInheritDeclarationPriority() {
        var actual = Render("<g style='fill:#ff0000!important;--paint:#ff0000!important'><rect width='16' height='32'/>" +
            "<rect x='16' width='16' height='32' style='--paint:#0000ff;fill:var(--paint)'/></g>");
        Assert.Equal(Render("<rect width='16' height='32' fill='#ff0000'/><rect x='16' width='16' height='32' fill='#0000ff'/>"), actual);
    }

    [Fact]
    public void GradientStopsAndCustomPropertiesUseTheSameAuthorPriority() {
        var actual = Render("<style>stop{--paint:#ff0000!important;stop-color:var(--paint)!important;stop-opacity:.5!important}</style>" +
            "<defs><linearGradient id='g'><stop offset='0' style='--paint:#0000ff;stop-color:#00ff00;stop-opacity:1'/></linearGradient></defs>" +
            "<rect width='32' height='32' fill='url(#g)'/>");
        Assert.Equal(Render("<rect width='32' height='32' fill='#ff0000' fill-opacity='.5'/>"), actual);
    }

    [Fact]
    public void DeclarationRoundTripKeepsOrdinaryValuesAndRecognizesOnlyTerminalUnquotedPriority() {
        const string ordinary = "fill:var(--paint,#123456);font-family:'important !important';--payload:url('x!important')";
        Assert.Equal(ordinary, SvgStyleDeclarationList.Parse(ordinary).ToMarkup());
        var declarations = SvgStyleDeclarationList.Parse("fill:red!important;fill:blue;stroke:green ! IMPORTANT");
        Assert.Equal("red", declarations.Get("fill"));
        Assert.All(declarations.Declarations, declaration => Assert.True(declaration.Important));
        Assert.Equal(declarations.ToMarkup(), new SvgStyleDeclarationList(declarations.Declarations).ToMarkup());
        Assert.Equal("blue", declarations.Set("fill", "blue").Get("fill"));
    }

    private static byte[] Render(string content) {
        Assert.True(SvgRasterRenderer.TryRenderFragment(content, "0 0 32 32", "none", 32, 32, out var pixels));
        return pixels;
    }
}
