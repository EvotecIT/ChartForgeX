using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlChartLocalizationTests {
    [Fact]
    public void ChartsInheritNativeLanguageAndDashboardsCopyConfiguredTranslationsToChildren() {
        var chart = Chart.Create().AddLine("Observations", new[] { new ChartPoint(0, 2), new ChartPoint(1, 3) })
            .ConfigureAccessibility(options => options.Language = "pl-PL");
        Assert.Contains("<html lang=\"pl-PL\"", chart.ToInteractiveHtmlPage());
        var html = new HtmlInteractiveDashboardRenderer().RenderPage(new[] { chart, chart }, options => {
            options.Localization.Language = "de-DE";
            options.Localization[HtmlChartText.ResetView] = "Ansicht zurücksetzen";
        });
        Assert.Contains("<html lang=\"de-DE\"", html);
        Assert.Equal(2, html.Split(new[] { ">Ansicht zurücksetzen</button>" }, StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void MessagesAreValidatedAndInstancesKeepIndependentOverrides() {
        var localization = new HtmlChartLocalization();
        Assert.Throws<ArgumentException>(() => localization.Language = "\n");
        Assert.Throws<ArgumentException>(() => localization[HtmlChartText.Clear] = " ");
        Assert.Throws<ArgumentOutOfRangeException>(() => localization[(HtmlChartText)int.MaxValue] = "Invalid");
        localization[HtmlChartText.Compare] = "Porównaj {count}";
        Assert.Equal("Porównaj {count}", localization[HtmlChartText.Compare]);
        Assert.Equal("Clear", new HtmlChartLocalization()[HtmlChartText.Clear]);
    }
}
