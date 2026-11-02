using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>A vertical cone's rail reads as two lines per stage, centred on the stage's divider.</summary>
public sealed class ConeRailCaptionTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)] [InlineData(VisualThemeMode.Dark)]
    public void EachStageShowsNameAndValueThenBothRatiosInTheSecondaryColour(VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(800, 480).WithTheme(VisualTheme.Graphite(), mode).WithDataLabels(true)
            .WithXLabels("Visitors", "Sign-ups", "Trials", "Paid", "Renewed")
            .ConfigureFunnel(options => { options.Form = ChartFunnelForm.Cone; options.Orientation = ChartOrientation.Vertical; options.LabelContent = ChartStageLabelContent.Separate; })
            .AddFunnel("Customers", new[] { 12400d, 5270, 2130, 860, 610 }.Select((value, index) => new ChartPoint(index + 1, value)));
        var colors = chart.Options.Theme.Resolve(mode);
        var svg = XDocument.Parse(chart.ToSvg());
        string[] Texts(string role) => svg.Descendants().Where(item => ((string?)item.Attribute("data-cfx-role"))?.StartsWith(role, StringComparison.Ordinal) == true)
            .Select(item => string.Concat(item.DescendantsAndSelf().Where(node => node.Name.LocalName == "text").Select(node => node.Value))).Where(text => text.Length > 0).ToArray();
        var captions = Texts("funnel-category-label");
        Assert.Equal(new[] { "Visitors  12,400", "Sign-ups  5,270", "Trials  2,130", "Paid  860", "Renewed  610" }, captions);
        var ratios = Texts("funnel-ratio-label");
        Assert.Equal(4, ratios.Length);
        Assert.Equal("42.5% retained · 57.5% drop-off", ratios[0]);
        Assert.DoesNotContain(ratios, text => text.Contains("100.0% retained", StringComparison.Ordinal));
        Assert.Empty(Texts("funnel-value-label")); Assert.Empty(Texts("funnel-retention-label")); Assert.Empty(Texts("funnel-dropoff-label"));
        var ratioText = svg.Descendants().First(item => ((string?)item.Attribute("data-cfx-role"))?.StartsWith("funnel-ratio-label", StringComparison.Ordinal) == true)
            .DescendantsAndSelf().First(node => node.Name.LocalName == "text");
        Assert.True(ChartColor.TryParse((string?)ratioText.Attribute("fill"), out var fill)); Assert.Equal(colors.MutedForeground, fill);
    }
}
