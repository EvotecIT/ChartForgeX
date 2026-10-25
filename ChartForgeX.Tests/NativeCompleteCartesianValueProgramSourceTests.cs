using ChartForgeX.Themes;
using Xunit;
namespace ChartForgeX.Tests;
public sealed class NativeCompleteCartesianValueProgramSourceTests {
    [Theory][InlineData("area",390,false)][InlineData("stacked-bar",760,true)][InlineData("stacked-area-gap",390,false)][InlineData("stacked-area-zero",760,true)]
    public void PublicCompleteValueProgramsUseRealSourceAndTheExistingHtmlLifetime(string variant,int width,bool dark) {
        var program=ExampleRecipeSource.EmitNativeCompleteCartesianValue(variant,width,dark?VisualThemeMode.Dark:VisualThemeMode.Light);
        NativeRadialMotionBeforeControls.Save("complete-cartesian-program-"+variant+"-"+width+".cs",program);
        Assert.DoesNotContain("{{",program,StringComparison.Ordinal);Assert.Contains("PrepareNativeTransitionJson",program,StringComparison.Ordinal);Assert.Contains("FontRegistry.Register",program,StringComparison.Ordinal);Assert.Contains("ChartSeriesKind.StackedArea",program,StringComparison.Ordinal);Assert.Contains("WithStackedAreaMissingData",program,StringComparison.Ordinal);Assert.DoesNotContain("requestAnimationFrame",program,StringComparison.Ordinal);
    }
}
