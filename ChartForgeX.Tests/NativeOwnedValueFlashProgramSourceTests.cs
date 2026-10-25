using ChartForgeX.Themes;
using Xunit;
namespace ChartForgeX.Tests;
public sealed class NativeOwnedValueFlashProgramSourceTests {
    [Theory] [InlineData("line",390,false)] [InlineData("bar-gradient",760,true)] [InlineData("bar-capsule",390,true)] [InlineData("pie",760,false)]
    [InlineData("raw-pie",390,false)] [InlineData("raw-donut",760,true)] [InlineData("treemap",390,true)] [InlineData("sunburst",760,false)]
    public void PublicFlashProgramsKeepNativeSourcesCapturedFontsAndExistingValuePresentation(string kind,int width,bool dark){var program=ExampleRecipeSource.EmitNativeOwnedValueFlash(kind,width,dark?VisualThemeMode.Dark:VisualThemeMode.Light);NativeRadialMotionBeforeControls.Save("owned-flash-program-"+kind+"-"+width+".cs",program);Assert.DoesNotContain("{{",program,StringComparison.Ordinal);Assert.Contains("ValueFlash",program,StringComparison.Ordinal);Assert.Contains("FontRegistry.Register",program,StringComparison.Ordinal);Assert.Contains("PrepareNativeTransitionJson",program,StringComparison.Ordinal);Assert.DoesNotContain("requestAnimationFrame",program,StringComparison.Ordinal);Assert.DoesNotContain("OpenTypeTestFonts",program,StringComparison.Ordinal);}
}
