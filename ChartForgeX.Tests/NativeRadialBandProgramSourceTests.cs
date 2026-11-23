using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeRadialBandProgramSourceTests {
    [Theory]
    [InlineData("pie",true,"values",390,ThemeMode.Light)] [InlineData("donut",false,"values",760,ThemeMode.Dark)]
    [InlineData("mixed",true,"values",760,ThemeMode.Light)] [InlineData("mixed",false,"initial",390,ThemeMode.Dark)]
    [InlineData("donut",true,"initial",760,ThemeMode.Light)] [InlineData("pie",false,"other",390,ThemeMode.Dark)]
    [InlineData("mixed",true,"other",390,ThemeMode.Light)] [InlineData("donut",false,"other",760,ThemeMode.Dark)]
    public void OrdinaryConsumerProgramCarriesTheCompiledCoreRecipeAndExistingHtmlHost(string family,bool automatic,string intent,int width,ThemeMode mode) {
        var program = ExampleRecipeSource.EmitNativeRadialBands(family,automatic,intent,width,mode); NativeRadialMotionBeforeControls.Save("bands-program-" + family + "-" + automatic + "-" + intent + "-" + width + "-" + mode + ".cs",program);
        Assert.DoesNotContain("{{",program,StringComparison.Ordinal); Assert.Contains("class NativeRadialBandMotionRecipe",program,StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson",program,StringComparison.Ordinal); Assert.Contains("ChartVisibilityTarget.RadialOther",program,StringComparison.Ordinal);
        Assert.Contains("RadialBandAllocation",program,StringComparison.Ordinal); Assert.Contains("RadialBand",program,StringComparison.Ordinal); Assert.Contains("using ChartForgeX.Interactivity.Html;",program,StringComparison.Ordinal);
    }
}
