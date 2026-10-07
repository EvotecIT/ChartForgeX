using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2PreparedLineStyleTests {
    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.StepLine)]
    [InlineData(ChartSeriesKind.Area)]
    [InlineData(ChartSeriesKind.StepArea)]
    [InlineData(ChartSeriesKind.StackedArea)]
    public void AuthoredLineTreatmentsFailExplicitlyAtEveryPreparedBoundaryKind(ChartSeriesKind kind) {
        var chart = Create(kind);
        foreach (var style in new[] { ChartLineVisualStyle.Plain(), ChartLineVisualStyle.Luminous(), ChartLineVisualStyle.Premium() }) {
            chart.WithLineVisualStyle(style);
            var error = Assert.Throws<NotSupportedException>(() => chart.Prepare(new VisualRenderContext()));
            Assert.Contains("explicit line visual styles", error.Message);
            Assert.Contains("legacy export", error.Message);
            Assert.True(chart.ToPng().Length > 64);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetterMutationsRemainAuthoredWhenTheLegacyThemeChanges(bool graphite) {
        var chart = Create(ChartSeriesKind.Line).WithTheme(graphite ? ChartTheme.GraphiteLight() : ChartTheme.Light());
        chart.Options.LineVisualStyle.HaloOpacity = .5;
        Assert.Contains("explicit line visual styles", Assert.Throws<NotSupportedException>(() => chart.Prepare(new VisualRenderContext())).Message);
        chart.WithTheme(graphite ? ChartTheme.Light() : ChartTheme.GraphiteLight());
        Assert.Contains("explicit line visual styles", Assert.Throws<NotSupportedException>(() => chart.Prepare(new VisualRenderContext())).Message);
    }

    [Fact]
    public void UntouchedLegacyDefaultsDoNotChangePreparedThemeAndUnrelatedMarksDoNotRejectLineSettings() {
        var chart = Create(ChartSeriesKind.Line).WithTheme(ChartTheme.Light());
        var classic = chart.Prepare(new VisualRenderContext()).ToSvg();
        chart.WithTheme(ChartTheme.GraphiteLight());
        Assert.Equal(classic, chart.Prepare(new VisualRenderContext()).ToSvg());
        foreach (var kind in new[] { ChartSeriesKind.Bar, ChartSeriesKind.Scatter }) {
            var marks = Create(kind).WithLuminousLineStyle().Prepare(new VisualRenderContext());
            Assert.NotEmpty(marks.Regions);
            Assert.True(marks.ToPng().Length > 64);
        }
    }

    private static Chart Create(ChartSeriesKind kind) {
        var chart = Chart.Create();
        var points = new[] { new ChartPoint(1, 2), new ChartPoint(2, 4) };
        return kind switch {
            ChartSeriesKind.Line => chart.AddLine("Observed", points),
            ChartSeriesKind.StepLine => chart.AddStepLine("Observed", points),
            ChartSeriesKind.Area => chart.AddArea("Observed", points),
            ChartSeriesKind.StepArea => chart.AddStepArea("Observed", points),
            ChartSeriesKind.StackedArea => chart.AddStackedArea("Observed", points),
            ChartSeriesKind.Bar => chart.AddBar("Observed", points),
            ChartSeriesKind.Scatter => chart.AddScatter("Observed", points),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
