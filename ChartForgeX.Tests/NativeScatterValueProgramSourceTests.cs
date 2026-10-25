using ChartForgeX.Themes;
using Xunit;
namespace ChartForgeX.Tests;
public sealed class NativeScatterValueProgramSourceTests {
    [Theory][InlineData(false,390,false)][InlineData(false,760,true)][InlineData(true,390,false)][InlineData(true,760,true)]
    public void PublicScatterProgramsKeepActualSourceValuesAxesAndExplicitFontOwnership(bool initial,int width,bool dark){var program=ExampleRecipeSource.EmitNativeScatterValue(initial,width,dark?VisualThemeMode.Dark:VisualThemeMode.Light);NativeRadialMotionBeforeControls.Save("scatter-program-"+initial+"-"+width+".cs",program);Assert.DoesNotContain("{{",program,StringComparison.Ordinal);Assert.Contains("ChartSeriesKind.Scatter",program,StringComparison.Ordinal);Assert.Contains("PrepareNativeTransitionJson",program,StringComparison.Ordinal);Assert.Contains("FontRegistry.Register",program,StringComparison.Ordinal);Assert.Contains(initial?"initial=true":"initial=false",program,StringComparison.Ordinal);Assert.DoesNotContain("requestAnimationFrame",program,StringComparison.Ordinal);}
}
