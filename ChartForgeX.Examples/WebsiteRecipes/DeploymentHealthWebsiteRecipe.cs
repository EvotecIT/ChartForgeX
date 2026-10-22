#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
#endregion

/// <summary>The executable source for the website's first static chart.</summary>
public static class DeploymentHealthWebsiteRecipe {
    /// <summary>Creates the same chart in either website theme.</summary>
    public static Chart Create(VisualThemeMode mode = VisualThemeMode.Light) {
#region chart
        var chart = Chart.Create()
            .WithThemeMode(mode)
            .WithTitle("Deployment health")
            .WithXAxis("Run")
            .WithYAxis("Checks")
            .WithSize(1180, 640)
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
            .AddSmoothArea("Passed", Points(820, 940, 980, 1040, 1120))
            .AddSmoothLine("Failed", Points(22, 30, 28, 21, 18),
                ChartColor.FromRgb(248, 113, 113));
#endregion
        return chart;
    }

    /// <summary>Writes the ordinary standalone example outputs.</summary>
    public static void Save(Chart chart) {
#region save
        chart.SaveSvg("deployment-health.svg");
        chart.SaveHtml("deployment-health.html");
        chart.SavePng("deployment-health.png");
#endregion
    }

#region points
    static IEnumerable<ChartPoint> Points(params double[] values) {
        for (var index = 0; index < values.Length; index++) {
            yield return new ChartPoint(index + 1, values[index]);
        }
    }
#endregion
}
