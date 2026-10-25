using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRicherNumberBeforeControls {
    [Theory]
    [InlineData("0.00 'ms'",123.4,"","123.40 ms")]
    [InlineData("0.0;[negative]0.0",-123.4,"","[negative]123.4")]
    [InlineData("0.0‰",.125,"","125.0‰")]
    [InlineData("#,##0,, 'M'",1234567,"","1 M")]
    [InlineData("0.00E+00",12345,"","1.23E+04")]
    [InlineData("0.0;[debit](0.0);'zero'",0,"","zero")]
    [InlineData("0.00\\m\\s",123.4,"","123.40ms")]
    [InlineData("0.00 'ms'",123.4,"fr-FR","123,40 ms")]
    public void ExistingNumberOwnerFormatsActualCustomGrammarAndOrdinaryNativeCaptions(string format,double reading,string cultureName,string expected) {
        var culture=cultureName.Length==0?CultureInfo.InvariantCulture:CultureInfo.GetCultureInfo(cultureName);var policy=ChartValueFormat.Number(format,culture);var text=policy.Format(reading);
        // Pie source remains nonnegative; the negative-section formatter observation is an actual formatter contract, not negative Pie admission.
        var chart=NativeRadialMotionBeforeControls.Model(false,false).WithValueFormat(policy);var renderer=new HtmlInteractiveChartRenderer();var section=renderer.PrepareSection(chart);var repeat=renderer.PrepareSection(chart);
        var formatKey=string.Concat(format.Select(character=>((int)character).ToString("x4",CultureInfo.InvariantCulture)));var name="number-custom-before-"+formatKey+"-"+cultureName;
        NativeRadialMotionBeforeControls.Capture(section,name);NativeRadialMotionBeforeControls.Save(name+".facts.json",JsonSerializer.Serialize(new{format,reading,cultureName,text,expected}));
        Assert.Equal(expected,text);Assert.Equal(section.Html,repeat.Html);Assert.Equal(section.PreparedVisual.ToSvg(),repeat.PreparedVisual.ToSvg());Assert.Equal(section.PreparedVisual.ToPng(),repeat.PreparedVisual.ToPng());
    }
}
