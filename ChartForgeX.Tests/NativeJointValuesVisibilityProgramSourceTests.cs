using ChartForgeX.Themes;
using Xunit;
namespace ChartForgeX.Tests;
public sealed class NativeJointValuesVisibilityProgramSourceTests {
    [Theory][InlineData(false,390,false)][InlineData(false,760,true)][InlineData(true,390,false)][InlineData(true,760,true)]
    public void PublicJointProgramsUseOneNativeValueVisibilitySourceAndIncumbentPresentationLifetime(bool bar,int width,bool dark){var program=ExampleRecipeSource.EmitNativeJointValuesVisibility(bar,width,dark?VisualThemeMode.Dark:VisualThemeMode.Light);NativeRadialMotionBeforeControls.Save("joint-program-"+bar+"-"+width+".cs",program);Assert.DoesNotContain("{{",program,StringComparison.Ordinal);Assert.Contains("ValuesAndVisibility",program,StringComparison.Ordinal);Assert.Contains("PrepareVisibilitySection",program,StringComparison.Ordinal);Assert.Contains("ChartObservationId",program,StringComparison.Ordinal);Assert.Contains("FontRegistry.Register",program,StringComparison.Ordinal);Assert.DoesNotContain("requestAnimationFrame",program,StringComparison.Ordinal);}
}
