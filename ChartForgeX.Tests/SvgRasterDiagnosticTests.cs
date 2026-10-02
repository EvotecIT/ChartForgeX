using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterDiagnosticTests {
    [Theory]
    [InlineData("<rect id='shape' width='20' height='20' filter='url(#hide)'/>")]
    [InlineData("<style>.filtered{filter:var(--effect)}</style><rect id='shape' class='filtered' style='--effect:url(#hide)' width='20' height='20'/>")]
    [InlineData("<text x='0' y='15'><tspan id='shape' filter='url(#hide)'>X</tspan></text>")]
    public void FiltersProduceStableElementDiagnosticsAndStrictRejection(string body) {
        var source = Svg(body);
        var result = SvgRasterizer.Rasterize(source);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("SFR002", diagnostic.Code);
        Assert.Equal("shape", diagnostic.ElementId);
        Assert.Contains(diagnostic.ElementName, new[] { "rect", "tspan" });
        Assert.Contains(result.Image.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
        Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true)).Message);
    }

    [Fact]
    public void VisibleForeignContentAndUndecodableImagesAreReported() {
        var result = SvgRasterizer.Rasterize(Svg("<foreignObject id='html' width='20' height='20'><div xmlns='http://www.w3.org/1999/xhtml'>X</div></foreignObject><image id='image' width='20' height='20' href='data:image/png;base64,AA=='/>"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SFR001" && diagnostic.ElementId == "html");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SFR003" && diagnostic.ElementId == "image");
        Assert.DoesNotContain(result.Image.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha != 0);
    }

    [Fact]
    public void NestedSvgAndUseReferencesRetainLossDiagnostics() {
        var nested = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Svg("<rect id='nested' width='20' height='20' filter='blur(2px)'/>")));
        var result = SvgRasterizer.Rasterize(Svg("<image width='20' height='20' href='data:image/svg+xml;base64," + nested + "'/><use id='missing' href='#absent'/>"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SFR002" && diagnostic.ElementId == "nested");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SFR004" && diagnostic.ElementId == "missing");
    }

    [Fact]
    public void UnsupportedChildrenOfTextProduceLossDiagnostics() {
        var result = SvgRasterizer.Rasterize(Svg("<defs><path id='route' d='M0 15 L24 15'/></defs><text><textPath id='label' href='#route'>Label</textPath></text>"));
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("SFR001", diagnostic.Code);
        Assert.Equal("label", diagnostic.ElementId);
        Assert.Equal("textPath", diagnostic.ElementName);
    }

    [Fact]
    public void ReferencedDefinitionRootsRetainFilterDiagnostics() {
        foreach (var body in new[] {
            "<defs><symbol id='effect' filter='blur(2px)'><rect width='20' height='20'/></symbol></defs><use href='#effect'/>",
            "<defs><mask id='effect' filter='blur(2px)'><rect width='20' height='20' fill='white'/></mask></defs><rect width='20' height='20' mask='url(#effect)'/>",
            "<style>defs .filtered{filter:var(--effect)}</style><defs style='--effect:blur(2px)'><pattern id='effect' class='filtered' patternUnits='userSpaceOnUse' width='8' height='8'><rect width='8' height='8'/></pattern></defs><rect width='20' height='20' fill='url(#effect)'/>",
            "<defs><pattern id='base' patternUnits='userSpaceOnUse' width='8' height='8'><rect width='8' height='8'/></pattern><pattern id='effect' href='#base' filter='blur(2px)'/></defs><rect width='20' height='20' fill='url(#effect)'/>"
        }) {
            var source = Svg(body);
            var diagnostic = Assert.Single(SvgRasterizer.Rasterize(source).Diagnostics);
            Assert.Equal("SFR002", diagnostic.Code);
            Assert.Equal("effect", diagnostic.ElementId);
            Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true)).Message);
        }
    }

    [Theory]
    [InlineData("<text id='unsupported' x='0' y='15'>X</text>", "SFR001")]
    [InlineData("<polyline id='unsupported' points='0,0 20,0 20,20'/>", "SFR001")]
    [InlineData("<use id='unsupported' href='#absent'/>", "SFR004")]
    public void AppliedClippingContentReportsUnsupportedGeometryAndReferences(string content, string code) {
        var source = Svg("<defs><clipPath id='clip'>" + content + "</clipPath></defs><rect width='20' height='20' clip-path='url(#clip)'/>");
        var diagnostic = Assert.Single(SvgRasterizer.Rasterize(source).Diagnostics);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal("unsupported", diagnostic.ElementId);
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true));
    }

    [Fact]
    public void ClippingSilhouettesIgnorePaintFiltersWithoutLossWarnings() {
        var source = Svg("<defs><clipPath id='clip' filter='blur(2px)'><rect width='20' height='20' filter='blur(2px)'/></clipPath></defs><rect width='20' height='20' clip-path='url(#clip)'/>");
        Assert.Empty(SvgRasterizer.Rasterize(source, strict: true).Diagnostics);
    }

    [Fact]
    public void SupportedAndUnusedDefinitionContentDoesNotProduceLossWarnings() {
        var result = SvgRasterizer.Rasterize(Svg("<defs><filter id='unused'><feGaussianBlur stdDeviation='3'/></filter></defs><metadata>source</metadata><linearGradient id='unused-gradient'><stop offset='0'/></linearGradient><rect width='20' height='20' filter='none'/><foreignObject display='none'/>"), strict: true);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(SvgRasterizer.ToImage(Svg("<rect width='20' height='20'/>" )).Pixels, result.Image.Pixels);
    }

    private static string Svg(string body) => "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24'>" + body + "</svg>";
}
