using ChartForgeX.Themes;
using Xunit;
namespace ChartForgeX.Tests;
public sealed class NativeJointStackValueProgramSourceTests {
    [Theory][InlineData("stacked-bar",390,false)][InlineData("default-stacked",760,true)][InlineData("normalized-bar",390,false)]
    public void JointStackProgramsRetainTheNativeFullSourceAndExplicitSeriesLabelPolicy(string variant,int width,bool dark){var program=ExampleRecipeSource.EmitNativeJointStackValue(variant,width,dark?VisualThemeMode.Dark:VisualThemeMode.Light);NativeRadialMotionBeforeControls.Save("joint-stack-program-"+variant+"-"+width+".cs",program);Assert.DoesNotContain("{{",program,StringComparison.Ordinal);Assert.Contains("HtmlNativeTransitionEffect.ValuesAndVisibility",program,StringComparison.Ordinal);Assert.Contains("ChartVisibilityLabelLayering.GroupWithSeries",program,StringComparison.Ordinal);Assert.Contains("ShowStackTotals",program,StringComparison.Ordinal);Assert.Contains("WithNormalization",program,StringComparison.Ordinal);Assert.DoesNotContain("requestAnimationFrame",program,StringComparison.Ordinal);}
}
