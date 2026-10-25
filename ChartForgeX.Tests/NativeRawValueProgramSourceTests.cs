using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;
public sealed class NativeRawValueProgramSourceTests {
    [Theory] [InlineData(false,"zero",390,false)] [InlineData(true,"zero",760,true)] [InlineData(false,"empty",760,true)] [InlineData(true,"empty",390,false)]
    [InlineData(false,"other",390,true)] [InlineData(true,"initial",760,false)] [InlineData(false,"source-reflow",760,false)] [InlineData(true,"source-reflow",390,true)]
    public void PublicRawValueProgramsUseActualNativeSourcesAndExistingAcceptanceLifetime(bool donut,string kind,int width,bool dark) {
        var program=ExampleRecipeSource.EmitNativeRawValue(donut,kind,width,dark?VisualThemeMode.Dark:VisualThemeMode.Light);NativeRadialMotionBeforeControls.Save("raw-program-"+donut+"-"+kind+"-"+width+".cs",program);
        Assert.DoesNotContain("{{",program,StringComparison.Ordinal);Assert.Contains("CaptureNativeValueSource=true",program,StringComparison.Ordinal);Assert.Contains("PrepareNativeTransitionJson",program,StringComparison.Ordinal);
        Assert.Contains("ChartObservationId",program,StringComparison.Ordinal);Assert.Contains("SourceReflow",program,StringComparison.Ordinal);Assert.DoesNotContain("OpenTypeTestFonts",program,StringComparison.Ordinal);Assert.DoesNotContain("requestAnimationFrame",program,StringComparison.Ordinal);
    }
}
