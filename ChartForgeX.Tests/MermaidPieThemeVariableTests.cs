using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects Mermaid pie colour variables mapped onto slice colours.</summary>
public sealed class MermaidPieThemeVariableTests {
    [Fact]
    public void PieVariablesColourTheirSlicesAndTitle() {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables': {'pie1':'#ff0000','pie3':'#0000ff','pieTitleTextColor':'#112233'}}}%%\n"
            + "pie title Pets\n  \"Dogs\" : 40\n  \"Cats\" : 30\n  \"Fish\" : 10");
        Assert.False(result.HasErrors);
        Assert.DoesNotContain(result.Diagnostics, item => item.Message.Contains("themeVariables.pie", System.StringComparison.Ordinal));
        var chart = MermaidPieRendering.ToChart((MermaidPieDocument)result.Document!);
        var colors = chart.Series[0].PointColors;
        Assert.Equal(ChartColor.FromHex("#ff0000"), colors[0]);
        Assert.Null(colors[1]);
        Assert.Equal(ChartColor.FromHex("#0000ff"), colors[2]);
        Assert.Equal(ChartColor.FromHex("#112233"), chart.Options.TitleStyle.Color);
    }
}
