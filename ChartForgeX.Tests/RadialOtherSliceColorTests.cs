using System.Reflection;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>The aggregated "Other" slice can be most of a ring, so it must stay visible on every built-in theme.</summary>
public sealed class RadialOtherSliceColorTests {
    public static IEnumerable<object[]> Themes() => typeof(VisualTheme).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(method => method.ReturnType == typeof(VisualTheme) && method.GetParameters().Length == 0)
        .SelectMany(method => new[] { VisualThemeMode.Light, VisualThemeMode.Dark }.Select(mode => new object[] { method.Name, mode }));

    [Theory]
    [MemberData(nameof(Themes))]
    public void OtherSlice_EveryBuiltInTheme_ContrastsWithThePlot(string theme, VisualThemeMode mode) {
        var colors = ((VisualTheme)typeof(VisualTheme).GetMethod(theme)!.Invoke(null, null)!).Resolve(mode);
        var chart = Chart.Create().WithSize(480, 320)
            .AddDonut("Licences", Enumerable.Range(0, 14).Select(index => new ChartPoint(index, index == 0 ? 8 : 6)));
        chart.Options.MaximumPieSlices = 2;

        var other = VisualRadialCompiler.LegendEntries(chart, colors).Last();

        Assert.Equal("Other", other.Label);
        Assert.True(ChartColorMath.ContrastRatio(other.Color, colors.Surface) >= 3 || ChartColorMath.ContrastRatio(colors.Neutral3, colors.Surface) < 3,
            $"{theme} {mode}: the Other slice ({other.Color}) must contrast at least 3:1 with the plot surface ({colors.Surface}).");
    }

}
