using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterVariableInheritanceTests {
    [Theory]
    [InlineData("--missing:var(--absent);--ink:var(--missing,#ff0000)")]
    [InlineData("--ink:var(--missing,#ff0000);--missing:var(--absent)")]
    [InlineData("--ink:var(--middle,#ff0000);--middle:var(--missing);--missing:var(--absent)")]
    public void UnavailableAliasesSelectTheDefiningFallbackBeforeChildInheritance(string declarations) {
        Assert.Equal(Render("<rect width='32' height='32' fill='#ff0000'/>"), Render(
            "<g style='" + declarations + "'><rect width='32' height='32' style='--absent:#0000ff;fill:var(--ink,#00ff00)'/></g>"));
    }

    [Fact]
    public void FallbacksCannotMakeCyclicAliasesAvailableButCanRecoverTheirConsumers() {
        var actual = Render("<g style='--self:var(--self,#ff0000);--a:var(--b,#ff0000);--b:var(--a,#0000ff);" +
            "--literal:#ff0000;--unused:var(--literal,var(--unused));--ink:var(--a,#ff0000)'>" +
            "<rect width='8' height='32' fill='var(--self,#00ff00)'/>" +
            "<rect x='8' width='8' height='32' fill='var(--b,#00ff00)'/>" +
            "<rect x='16' width='8' height='32' fill='var(--unused,#00ff00)'/>" +
            "<rect x='24' width='8' height='32' fill='var(--ink,#00ff00)'/></g>");
        Assert.Equal(Render("<rect width='24' height='32' fill='#00ff00'/><rect x='24' width='8' height='32' fill='#ff0000'/>"), actual);
    }

    [Fact]
    public void LongCyclesRemainUnavailableWhenSubstitutionIsBounded() {
        var declarations = string.Join(";", Enumerable.Range(0, 12).Select(i => "--v" + i + ":var(--v" + ((i + 1) % 12) + ",#ff0000)"));
        Assert.Equal(Render("<rect width='32' height='32' fill='#00ff00'/>"), Render(
            "<g style='" + declarations + "'><rect width='32' height='32' fill='var(--v0,#00ff00)'/></g>"));
    }

    [Fact]
    public void AnUnusedLongFallbackCannotInvalidateAnAvailableAlias() {
        var fallback = "#0000ff";
        for (var i = 0; i < 12; i++) fallback = "var(--absent," + fallback + ")";
        Assert.Equal(Render("<rect width='32' height='32' fill='#ff0000'/>"), Render(
            "<g style='--literal:#ff0000;--ink:var(--literal," + fallback + ")'>" +
            "<rect width='32' height='32' fill='var(--ink,#00ff00)'/></g>"));
    }

    [Fact]
    public void OverlappingFallbackCyclesInvalidateEveryParticipatingAlias() {
        Assert.Equal(Render("<rect width='32' height='32' fill='#00ff00'/>"), Render(
            "<g style='--a:var(--b);--b:var(--c,var(--d));--c:var(--a);--d:var(--c,#ff0000)'>" +
            "<rect width='32' height='32' fill='var(--d,#00ff00)'/></g>"));
    }

    [Fact]
    public void GradientStopsInheritTheAvailableFallbackRatherThanRebindingItsMissingDependency() {
        Assert.Equal(Render("<rect width='32' height='32' fill='#ff0000'/>"), Render(
            "<defs style='--missing:var(--absent);--ink:var(--missing,#ff0000)'><linearGradient id='g'>" +
            "<stop offset='0' style='--absent:#0000ff;stop-color:var(--ink,#00ff00)'/></linearGradient></defs>" +
            "<rect width='32' height='32' fill='url(#g)'/>"));
    }

    [Theory]
    [InlineData("", "", "#000000")]
    [InlineData("style='--ink:#00ff00'", "", "#00ff00")]
    [InlineData("style='--ink:#00ff00'", "style='--ink:#0000ff'", "#0000ff")]
    public void ReferencedSymbolsUseHostInheritanceAndTheirOwnDeclarations(string useStyle, string symbolStyle, string expected) {
        Assert.Equal(Render("<rect width='32' height='32' fill='" + expected + "'/>"), Render(
            "<defs><g style='--paint:#ff0000;--ink:var(--paint)!important'><symbol id='s' viewBox='0 0 32 32' " + symbolStyle + ">" +
            "<rect width='32' height='32' style='--paint:#0000ff;fill:var(--ink)'/></symbol></g></defs>" +
            "<use href='#s' width='32' height='32' " + useStyle + "/>"));
    }

    [Fact]
    public void ReferencedLocalStylesheetAliasesComputeFromTheUseHost() {
        Assert.Equal(Render("<rect width='32' height='32' fill='#00ff00'/>"), Render(
            "<style>.themed{--ink:var(--paint)!important}</style><defs style='--paint:#ff0000'>" +
            "<symbol id='s' class='themed' viewBox='0 0 32 32'><rect width='32' height='32' fill='var(--ink)'/></symbol></defs>" +
            "<use href='#s' width='32' height='32' style='--paint:#00ff00'/>"));
    }

    [Fact]
    public void ClipUseInstancesInheritVariablesFromTheClipHost() {
        Assert.Equal(Render("<rect width='32' height='32' fill='#ff0000'/>"), Render(
            "<defs><g style='--visible:hidden'><rect id='s' width='32' height='32' visibility='var(--visible,visible)'/></g>" +
            "<clipPath id='c' style='--visible:visible'><use href='#s'/></clipPath></defs>" +
            "<rect width='32' height='32' fill='#ff0000' clip-path='url(#c)'/>"));
    }

    private static byte[] Render(string body) => SvgRasterizer.ToImage(
        "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'>" + body + "</svg>").Pixels;
}
