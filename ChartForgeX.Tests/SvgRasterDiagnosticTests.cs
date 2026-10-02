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
    public void SupportedAndUnusedDefinitionContentDoesNotProduceLossWarnings() {
        var result = SvgRasterizer.Rasterize(Svg("<defs><filter id='unused'><feGaussianBlur stdDeviation='3'/></filter></defs><metadata>source</metadata><linearGradient id='unused-gradient'><stop offset='0'/></linearGradient><rect width='20' height='20' filter='none'/><foreignObject display='none'/>"), strict: true);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(SvgRasterizer.ToImage(Svg("<rect width='20' height='20'/>" )).Pixels, result.Image.Pixels);
    }

    private static string Svg(string body) => "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24'>" + body + "</svg>";
}
