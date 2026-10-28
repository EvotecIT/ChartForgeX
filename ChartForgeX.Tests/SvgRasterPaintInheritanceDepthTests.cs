using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Small public-source controls for active uncached href ancestry; no unbounded stack fixture.</summary>
public sealed class SvgRasterPaintInheritanceDepthTests {
    [Theory]
    [InlineData("pattern")] [InlineData("linearGradient")] [InlineData("radialGradient")]
    public void EightUncachedReferencesResolveButTheNinthIsLocatedBeforeFurtherRecursion(string kind) {
        var accepted = Source(kind, 8);
        var before = SvgRasterizer.Rasterize(accepted, strict: true);
        Assert.Empty(before.Diagnostics);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, before.Image.Pixels.Take(4));
        var limited = Source(kind, 9);
        PatternRecursionFixture.SaveSource("paint-inheritance-limit-" + kind, limited);
        var result = SvgRasterizer.Rasterize(limited);
        Assert.Contains(result.Diagnostics, item => item.Code == "SFR001" && item.ElementId == "p8"
            && item.Message.Contains("href ancestry", StringComparison.Ordinal));
        Assert.Contains("SFR001", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(limited, strict: true)).Message);
    }

    [Theory]
    [InlineData("pattern")] [InlineData("linearGradient")] [InlineData("radialGradient")]
    public void IndependentReuseDoesNotAccumulateDefinitionDepth(string kind) {
        var source = Source(kind, 8).Replace("</svg>", "<rect width='2' height='2' fill='url(#p0)'/></svg>", StringComparison.Ordinal);
        var result = SvgRasterizer.Rasterize(source, strict: true);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, result.Image.Pixels.Take(4));
    }

    private static string Source(string kind, int count) {
        var text = new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' width='2' height='2'><defs>");
        for (var index = 0; index < count; index++) {
            text.Append('<').Append(kind).Append(" id='p").Append(index.ToString(CultureInfo.InvariantCulture)).Append("'");
            if (kind == "pattern") text.Append(" patternUnits='userSpaceOnUse' width='1' height='1'");
            if (index + 1 < count) text.Append(" href='#p").Append((index + 1).ToString(CultureInfo.InvariantCulture)).Append("'/>");
            else {
                text.Append('>').Append(kind == "pattern" ? "<rect width='1' height='1' fill='red'/>"
                    : "<stop offset='0' stop-color='red'/><stop offset='1' stop-color='red'/>");
                text.Append("</").Append(kind).Append('>');
            }
        }
        return text.Append("</defs><rect width='2' height='2' fill='url(#p0)'/></svg>").ToString();
    }
}
