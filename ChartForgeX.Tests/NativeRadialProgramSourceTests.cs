using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeRadialProgramSourceTests {
    [Theory] [InlineData(false, "values", 390, false)] [InlineData(true, "initial", 760, true)] [InlineData(false, "other", 760, false)] [InlineData(true, "other", 390, true)]
    public void ExecutableProgramIncludesItsCompiledCoreRecipeAndAdapterOwnedHost(bool donut, string kind, int width, bool dark) {
        var program = ExampleRecipeSource.EmitNativeRadialMotion(donut, kind, width, dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        NativeRadialMotionBeforeControls.Save("radial-program-" + donut + "-" + kind + "-" + width + ".cs", program);
        Assert.DoesNotContain("{{", program, StringComparison.Ordinal); Assert.Contains("class NativeRadialMotionRecipe", program, StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.Contains("ChartVisibilityTarget.RadialOther", program, StringComparison.Ordinal);
        Assert.Contains("WithObservationIds", program, StringComparison.Ordinal); Assert.Contains("using ChartForgeX.Interactivity.Html;", program, StringComparison.Ordinal);
    }
}
