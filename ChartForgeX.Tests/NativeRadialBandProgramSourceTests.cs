using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeRadialBandProgramSourceTests {
    [Theory]
    [InlineData("pie",true,"values",390,VisualThemeMode.Light)] [InlineData("donut",false,"values",760,VisualThemeMode.Dark)]
    [InlineData("mixed",true,"values",760,VisualThemeMode.Light)] [InlineData("mixed",false,"initial",390,VisualThemeMode.Dark)]
    [InlineData("donut",true,"initial",760,VisualThemeMode.Light)] [InlineData("pie",false,"other",390,VisualThemeMode.Dark)]
    [InlineData("mixed",true,"other",390,VisualThemeMode.Light)] [InlineData("donut",false,"other",760,VisualThemeMode.Dark)]
    public void OrdinaryConsumerProgramCarriesTheCompiledCoreRecipeAndExistingHtmlHost(string family,bool automatic,string intent,int width,VisualThemeMode mode) {
        var program = ExampleRecipeSource.EmitNativeRadialBands(family,automatic,intent,width,mode); NativeRadialMotionBeforeControls.Save("bands-program-" + family + "-" + automatic + "-" + intent + "-" + width + "-" + mode + ".cs",program);
        Assert.DoesNotContain("{{",program,StringComparison.Ordinal); Assert.Contains("class NativeRadialBandMotionRecipe",program,StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson",program,StringComparison.Ordinal); Assert.Contains("ChartVisibilityTarget.RadialOther",program,StringComparison.Ordinal);
        Assert.Contains("WithAutomaticRadialBands",program,StringComparison.Ordinal); Assert.Contains("WithRadialBand",program,StringComparison.Ordinal); Assert.Contains("using ChartForgeX.Interactivity.Html;",program,StringComparison.Ordinal);
    }
}
