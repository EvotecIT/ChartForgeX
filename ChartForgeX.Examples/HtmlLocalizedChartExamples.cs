using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

/// <summary>Shows translated HTML controls with independently authored chart text and axes.</summary>
public static class HtmlLocalizedChartExamples {
    /// <summary>Creates a Polish chart whose controls, comparison tray and announcements share one message bundle.</summary>
    /// <param name="dark">Whether to use the dark Graphite palette.</param>
    /// <returns>A standalone interactive page.</returns>
    public static string CreatePage(bool dark = false) {
        var chart = Chart.Create().WithSize(800, 440).WithTitle("Czas przetwarzania")
            .WithSubtitle("Wybierz pomiary, aby porównać wyniki")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .ConfigureAccessibility(options => options.Language = "pl-PL")
            .WithXAxis("Pomiar").WithYAxis("Sekundy")
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("measurements"), "Pomiary",
                new[] { new ChartPoint(1, 12), new ChartPoint(2, 17), new ChartPoint(3, 9), new ChartPoint(4, 14) }));
        return chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Export);
            options.Localization[HtmlChartText.ChartControls] = "Narzędzia wykresu";
            options.Localization[HtmlChartText.ZoomIn] = "Powiększ";
            options.Localization[HtmlChartText.ZoomOut] = "Pomniejsz";
            options.Localization[HtmlChartText.ZoomInButton] = "Powiększ +";
            options.Localization[HtmlChartText.ZoomOutButton] = "Pomniejsz −";
            options.Localization[HtmlChartText.DownloadSvg] = "Pobierz SVG";
            options.Localization[HtmlChartText.DownloadPng] = "Pobierz PNG";
            options.Localization[HtmlChartText.ResetView] = "Przywróć widok";
            options.Localization[HtmlChartText.Compare] = "Porównaj {count}";
            options.Localization[HtmlChartText.Clear] = "Wyczyść";
            options.Localization[HtmlChartText.Pinned] = "Przypięte";
            options.Localization[HtmlChartText.Highlighted] = "Wyróżnione pomiary: {count}";
            options.Localization[HtmlChartText.HighlightCleared] = "Usunięto wyróżnienie";
        });
    }
}
