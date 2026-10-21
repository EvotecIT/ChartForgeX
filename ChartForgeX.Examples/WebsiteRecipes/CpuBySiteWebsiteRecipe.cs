#region imports
using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Data;
using ChartForgeX.Themes;
#endregion

/// <summary>The executable source for the website's typed-record report grid.</summary>
public static class CpuBySiteWebsiteRecipe {
    /// <summary>Creates all four panels with the selected grid theme.</summary>
    public static ChartGrid Create(VisualThemeMode mode = VisualThemeMode.Light) {
#region records
        var samples = ChartDataset<CpuSample>.From(new CpuSample[] {
            new("Warsaw", 1, 35), new("Warsaw", 2, 42),
            new("Warsaw", 3, 39), new("Warsaw", 4, 51),
            new("London", 1, 48), new("London", 2, 61),
            new("London", 3, 57), new("London", 4, 66),
            new("Berlin", 1, 22), new("Berlin", 2, 28),
            new("Berlin", 3, 31), new("Berlin", 4, 36),
            new("Madrid", 1, 51), new("Madrid", 2, 47),
            new("Madrid", 3, 44), new("Madrid", 4, 39)
        });
#endregion
#region grid
        var report = ChartGrid.FromFacets(
            samples,
            sample => sample.Site,
            (site, rows) => Chart.Create()
                .WithTitle(site)
                .WithYAxis("CPU (%)")
                .AddLine("CPU", rows,
                    sample => sample.Minute, sample => sample.Cpu),
            columns: 2).WithThemeMode(mode);
#endregion
        return report;
    }

    /// <summary>Writes the ordinary standalone example outputs.</summary>
    public static void Save(ChartGrid report) {
#region save
        report.SavePng("cpu-by-site.png");
        report.SaveSvg("cpu-by-site.svg");
#endregion
    }

#region record
    /// <summary>One CPU observation from a named site.</summary>
    public sealed record CpuSample(string Site, double Minute, double Cpu);
#endregion
}
