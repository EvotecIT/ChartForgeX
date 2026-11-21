using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class HtmlOwnedFontRecipeTests {
    [Theory] [InlineData("wide",ThemeMode.Light)] [InlineData("wide",ThemeMode.Dark)]
    [InlineData("compact",ThemeMode.Light)] [InlineData("compact",ThemeMode.Dark)]
    public void CompiledRecipeAndOrdinaryConsumerUseTheSameNativeModelAndHtmlTextPolicy(string variant,ThemeMode mode) {
        var chart=HtmlOwnedFontRecipe.Create(variant,mode,BrowserProofFonts.Family);var prepared=chart.Prepare(VisualExportRequest.ForChart(chart).Context);var png=prepared.ToPng();
        var html=chart.ToInteractiveHtmlPage(options=>options.TextPresentation=HtmlChartTextPresentation.Automatic);
        var source=ExampleRecipeSource.EmitHtmlOwnedFonts(variant,mode,galleryTypography:true);
        HtmlOwnedFontDeliveryTests.Save("recipe-"+variant+"-"+mode+".html",html);HtmlOwnedFontDeliveryTests.Save("recipe-"+variant+"-"+mode+".cs",source);
        Assert.Contains("data-cfx-font-root=",html,StringComparison.Ordinal);Assert.Contains("HtmlChartTextPresentation.Automatic",source,StringComparison.Ordinal);
        Assert.Contains("FontRegistry.Register",source,StringComparison.Ordinal);Assert.DoesNotContain("FilePath",source,StringComparison.Ordinal);
        Assert.Equal(png,prepared.ToPng());Assert.DoesNotContain("<script",prepared.ToSvg(),StringComparison.Ordinal);
    }
}
