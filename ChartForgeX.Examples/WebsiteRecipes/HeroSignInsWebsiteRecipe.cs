#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
#endregion

/// <summary>One executable source for the homepage chart, its data and its displayed program.</summary>
public static class HeroSignInsWebsiteRecipe {
    /// <summary>Creates the homepage chart in the selected output theme.</summary>
    public static Chart Create(VisualThemeMode mode = VisualThemeMode.Light) {
#region chart
        var (series, weeks, values) = SignInData();
        var chart = Chart.Create()
            .WithTitle("Sign-ins by method")
            .WithSubtitle("Weekly sign-ins across the tenant, last 12 weeks")
            .WithYAxis("Sign-ins")
            .WithThemeMode(mode)
            .ConfigureAccessibility(a => a.WithTextAlternative(
                "Sign-ins by method",
                "Passkey sign-ins grow from 800 to 8,900 a week while password-only and SMS sign-ins decline.",
                "en"))
            .WithSize(960, 540)
            .WithXLabels(weeks)
            .AddStackedArea(series[0], Points(values[0]))
            .AddStackedArea(series[1], Points(values[1]))
            .AddStackedArea(series[2], Points(values[2]))
            .AddStackedArea(series[3], Points(values[3]));
#endregion
        return chart;
    }

    /// <summary>Supplies the same observations to the dashboard and Markdown presentation.</summary>
    public static (string[] Series, string[] Weeks, double[][] Values) GetData() => SignInData();

    /// <summary>Writes the ordinary standalone program's static exports.</summary>
    public static void Save(Chart chart) {
#region save
        chart.SaveSvg("sign-ins.svg");
        chart.SavePng("sign-ins.png");
        chart.SaveHtml("sign-ins.html");
#endregion
    }

#region data
    static (string[] Series, string[] Weeks, double[][] Values) SignInData() => (
        new[] { "Passkey", "Authenticator app", "SMS code", "Password only" },
        Enumerable.Range(1, 12).Select(week => $"W{week}").ToArray(),
        new[] {
            new double[] { 800, 1200, 1700, 2300, 3000, 3800, 4700, 5500, 6400, 7200, 8100, 8900 },
            new double[] { 7200, 7400, 7500, 7600, 7500, 7400, 7300, 7100, 6900, 6700, 6400, 6200 },
            new double[] { 4200, 4000, 3800, 3500, 3200, 2900, 2600, 2300, 2000, 1800, 1500, 1300 },
            new double[] { 5600, 5300, 4900, 4500, 4100, 3700, 3300, 2900, 2500, 2100, 1800, 1500 }
        });
#endregion

#region points
    static IEnumerable<ChartPoint> Points(params double[] values) =>
        values.Select((value, index) => new ChartPoint(index + 1, value));
#endregion
}
