using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeRicherNumberGrammarTests {
    [Theory]
    [InlineData("0.00 'ms'", 123.4, "", "123.40 ms")]
    [InlineData("0.0;[negative]0.0", -123.4, "", "[negative]123.4")]
    [InlineData("0.0‰", .125, "", "125.0‰")]
    [InlineData("#,##0,, 'M'", 1234567, "", "1 M")]
    [InlineData("0.00E+00", 12345, "", "1.23E+04")]
    [InlineData("0.0;[debit](0.0);'zero'", 0, "", "zero")]
    [InlineData(@"0.00\m\s", 123.4, "", "123.40ms")]
    [InlineData("0.00 'ms'", 123.4, "fr-FR", "123,40 ms")]
    public void BoundedCustomGrammarUsesTheActualFormatterWitnessesAndImmutableNumericProvider(string format, double input, string cultureName, string expected) {
        var policy = ChartValueFormat.Number(format, CultureInfo.GetCultureInfo(cultureName));
        var text = policy.Format(input); var admitted = policy.TryGetNativeTextRepertoire(out var owner, out var observations);
        var changed = (CultureInfo)CultureInfo.GetCultureInfo(cultureName).Clone(); changed.NumberFormat.PercentSymbol = "changed-percent";
        var modified = ChartValueFormat.Number(format, changed); var modifiedAdmission = modified.TryGetNativeTextRepertoire(out _, out _);
        NativeRadialMotionBeforeControls.Save("richer-number-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(format)) + "-" + cultureName + ".facts.json",
            JsonSerializer.Serialize(new { format, input, text, expected, admitted, owner, observations, modifiedAdmission }));
        Assert.Equal(expected, text); Assert.True(admitted); Assert.StartsWith("value-format-native-Number:", owner, StringComparison.Ordinal);
        Assert.Contains(policy.Format(1234.5), observations); Assert.Contains(policy.Format(-1234.5), observations);
        Assert.Contains(policy.Format(0), observations); Assert.Contains(policy.Format(double.MaxValue), observations);
        Assert.False(modifiedAdmission); Assert.Throws<NotSupportedException>(() => modified.PortablePolicy());
    }
    [Theory] [InlineData("0;0;0;0")] [InlineData("0.0E+0000")] [InlineData("N18")] [InlineData("000000000000000000")]
    public void BoundedAdmissionRejectsUnboundedWorkWithoutChangingOrdinaryFormatting(string format) {
        var policy = ChartValueFormat.Number(format); var admitted = policy.TryGetNativeTextRepertoire(out var owner, out var observations); var ordinary = policy.Format(123.45);
        NativeRadialMotionBeforeControls.Save("richer-number-limit-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(format)) + ".facts.json", JsonSerializer.Serialize(new { format, admitted, owner, observations, ordinary }));
        Assert.False(admitted); Assert.Empty(owner); Assert.Empty(observations); Assert.NotEmpty(ordinary);
    }
    [Fact]
    public void TheIncumbentFormatterRetainsItsOwnInvalidStandardPrecisionBoundary() {
        var error = Record.Exception(() => ChartValueFormat.Number("F9999999999999999999999999999"));
        NativeRadialMotionBeforeControls.Save("richer-number-invalid-standard.facts.json", JsonSerializer.Serialize(new { error = error?.GetType().Name, message = error?.Message }));
        Assert.IsType<FormatException>(error);
    }
}
