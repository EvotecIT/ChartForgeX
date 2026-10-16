using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects complete gauge readings and detached, transactional anatomy configuration.</summary>
public sealed class GaugeConfigurationDataTests {
    [Fact]
    public void ReferencesRemainDetachedAndReadOnlyInTheirAuthoredOrder() {
        var references = new[] { new ChartGaugeTarget(125, "Stretch"), new ChartGaugeTarget(-10, "Baseline") };
        var reading = new ChartGaugeValue(140, 0, 100, references);
        var series = ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity", reading);
        references[0] = new ChartGaugeTarget(1, "Changed");

        Assert.Equal(140, reading.Value);
        Assert.Equal(new[] { 125d, -10d }, reading.Targets.Select(target => target.Value));
        Assert.Equal(new[] { "Stretch", "Baseline" }, reading.Targets.Select(target => target.Label));
        Assert.Equal(new[] { 125d, -10d }, Assert.Single(series.GaugeValues).Targets.Select(target => target.Value));
        Assert.Throws<NotSupportedException>(() => ((IList<ChartGaugeTarget>)reading.Targets)[0] = new ChartGaugeTarget(2));
        Assert.Empty(default(ChartGaugeValue).Targets);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ReferenceValuesRejectNonFiniteNumbers(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartGaugeTarget(value, "Reference"));
    }

    [Fact]
    public void CompleteReadingUpdatesReplaceReferencesAndPreserveObservationOverrides() {
        var original = new ChartGaugeValue(40, targets: new[] { new ChartGaugeTarget(60, "Budget") });
        var color = ChartColor.FromHex("#123456");
        var series = ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity", original)
            .WithObservationId(0, new ChartObservationId("reading"))
            .WithPointLabel(0, "Measured capacity").WithPointColor(0, color);
        Chart.Create().AddSeries(series);

        series.UpdateGaugeValue(0, new ChartGaugeValue(70, 10, 110,
            new[] { new ChartGaugeTarget(80, "Budget"), new ChartGaugeTarget(90, "Stretch") }));

        var current = Assert.Single(series.GaugeValues);
        Assert.Equal(70, current.Value); Assert.Equal(10, current.Minimum); Assert.Equal(110, current.Maximum);
        Assert.Equal(new[] { 80d, 90d }, current.Targets.Select(target => target.Value));
        Assert.Equal(new[] { "Budget", "Stretch" }, current.Targets.Select(target => target.Label));
        Assert.Equal(60, Assert.Single(original.Targets).Value);
        Assert.Equal(1, series.ObservationCount); Assert.Equal(1, series.SourcePointCount); Assert.Empty(series.Points);
        Assert.Equal("reading", series.ObservationIds[0].Value);
        Assert.Equal("Measured capacity", series.PointLabels[0]); Assert.Equal(color, series.PointColors[0]);

        series.UpdateGaugeValue(0, new ChartGaugeValue(75));
        Assert.Empty(Assert.Single(series.GaugeValues).Targets);
        Assert.Equal("reading", series.ObservationIds[0].Value);
    }

    [Fact]
    public void ClearingAndReplacingReadingsRemoveStaleReferencesAndResetObservationOverrides() {
        var series = ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity",
                new ChartGaugeValue(40, targets: new[] { new ChartGaugeTarget(60, "Budget") }))
            .WithObservationId(0, new ChartObservationId("reading")).WithPointLabel(0, "Old label");

        series.ReplaceGaugeValues(Array.Empty<ChartGaugeValue>());
        Assert.Empty(series.GaugeValues); Assert.Equal(0, series.ObservationCount); Assert.Equal(0, series.SourcePointCount);
        Assert.Empty(series.ObservationIds); Assert.Empty(series.PointLabels);

        series.ReplaceGaugeValues(new[] { new ChartGaugeValue(20, targets: new[] { new ChartGaugeTarget(25, "New reference") }) });
        var reference = Assert.Single(Assert.Single(series.GaugeValues).Targets);
        Assert.Equal(25, reference.Value); Assert.Equal("New reference", reference.Label);
        Assert.Equal(1, series.ObservationCount); Assert.Empty(series.ObservationIds); Assert.Empty(series.PointLabels);
    }

    [Fact]
    public void InvalidGaugeReplacementKeepsTheCompleteReadingAndIdentity() {
        var series = ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity",
                new ChartGaugeValue(40, targets: new[] { new ChartGaugeTarget(60, "Budget") }))
            .WithObservationId(0, new ChartObservationId("reading")).WithPointLabel(0, "Measured capacity");

        Assert.Throws<ArgumentOutOfRangeException>(() => series.UpdateGaugeValue(0, default));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ReplaceGaugeValues(new[] { default(ChartGaugeValue) }));
        Assert.Throws<ArgumentException>(() => series.ReplaceGaugeValues(new[] { new ChartGaugeValue(1), new ChartGaugeValue(2) }));

        var current = Assert.Single(series.GaugeValues);
        Assert.Equal(40, current.Value);
        Assert.Equal(60, Assert.Single(current.Targets).Value); Assert.Equal("Budget", current.Targets[0].Label);
        Assert.Equal("reading", series.ObservationIds[0].Value); Assert.Equal("Measured capacity", series.PointLabels[0]);
    }

    [Fact]
    public void CircleFactoriesRejectTargetedReadings() {
        var reading = new ChartGaugeValue(40, targets: new[] { new ChartGaugeTarget(60, "Budget") });
        Assert.Throws<InvalidOperationException>(() => ChartSeries.CreateCircle(new ChartSeriesId("progress"), "Progress", reading));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CircleMutationsRejectTargetsWithoutChangingTheReadingOrIdentity(bool replace) {
        var series = ChartSeries.CreateCircle(new ChartSeriesId("progress"), "Progress", 40)
            .WithObservationId(0, new ChartObservationId("reading")).WithPointLabel(0, "Measured progress");
        var targeted = new ChartGaugeValue(70, targets: new[] { new ChartGaugeTarget(80) });

        Assert.Throws<InvalidOperationException>(() => {
            if (replace) series.ReplaceGaugeValues(new[] { targeted });
            else series.UpdateGaugeValue(0, targeted);
        });

        Assert.Equal(40, Assert.Single(series.GaugeValues).Value); Assert.Empty(series.GaugeValues[0].Targets);
        Assert.Equal(1, series.ObservationCount); Assert.Equal("reading", series.ObservationIds[0].Value);
        Assert.Equal("Measured progress", series.PointLabels[0]);
    }

    [Fact]
    public void DefaultAnatomyAndExplicitTickBoundariesRemainUsable() {
        var chart = Chart.Create(); var options = chart.Options.Gauge;
        Assert.Equal(ChartGaugeForm.Arc, options.Form); Assert.Equal(ChartOrientation.Horizontal, options.Orientation);
        Assert.Equal(150, options.StartAngleDegrees); Assert.Equal(240, options.SweepAngleDegrees);
        Assert.Null(options.TickCount); Assert.Empty(options.TickLabels);

        chart.ConfigureGauge(candidate => {
            candidate.Form = ChartGaugeForm.Needle; candidate.Orientation = ChartOrientation.Vertical;
            candidate.StartAngleDegrees = -720; candidate.SweepAngleDegrees = 360; candidate.TickCount = 2;
        });
        Assert.Equal(ChartGaugeForm.Needle, chart.Options.Gauge.Form); Assert.Equal(ChartOrientation.Vertical, chart.Options.Gauge.Orientation);
        Assert.Equal(-720, chart.Options.Gauge.StartAngleDegrees); Assert.Equal(360, chart.Options.Gauge.SweepAngleDegrees);
        Assert.Equal(2, chart.Options.Gauge.TickCount);
        chart.Options.Gauge.TickCount = 100; Assert.Equal(100, chart.Options.Gauge.TickCount);
        chart.Options.Gauge.TickCount = null; Assert.Null(chart.Options.Gauge.TickCount);
    }

    [Fact]
    public void ConfigurationCopiesTickLabelInputsAndDetachesRetainedCallbacks() {
        var labels = new[] { new ChartAxisLabel(10, "Low"), new ChartAxisLabel(90, "High") };
        var chart = Chart.Create(); var previous = chart.Options.Gauge;
        ChartGaugeOptions? retained = null;
        chart.ConfigureGauge(options => {
            retained = options;
            options.Form = ChartGaugeForm.Linear; options.Orientation = ChartOrientation.Vertical;
            options.StartAngleDegrees = 450; options.SweepAngleDegrees = 180; options.TickCount = 3;
            options.TickLabels = labels;
        });
        labels[0] = new ChartAxisLabel(1, "Changed input");
        previous.TickLabels = new[] { new ChartAxisLabel(2, "Previous options") };
        retained!.Form = ChartGaugeForm.Arc; retained.Orientation = ChartOrientation.Horizontal;
        retained.StartAngleDegrees = 0; retained.SweepAngleDegrees = 30; retained.TickCount = 5;
        retained.TickLabels = Array.Empty<ChartAxisLabel>();

        var installed = chart.Options.Gauge;
        Assert.NotSame(previous, installed); Assert.NotSame(retained, installed);
        Assert.Equal(ChartGaugeForm.Linear, installed.Form); Assert.Equal(ChartOrientation.Vertical, installed.Orientation);
        Assert.Equal(450, installed.StartAngleDegrees); Assert.Equal(180, installed.SweepAngleDegrees); Assert.Equal(3, installed.TickCount);
        Assert.Equal(new[] { 10d, 90d }, installed.TickLabels.Select(label => label.Value));
        Assert.Equal(new[] { "Low", "High" }, installed.TickLabels.Select(label => label.Text));
        Assert.Throws<NotSupportedException>(() => ((IList<ChartAxisLabel>)installed.TickLabels)[0] = new ChartAxisLabel(3, "Changed view"));
    }

    [Theory]
    [InlineData("form")]
    [InlineData("orientation")]
    [InlineData("start-nonfinite")]
    [InlineData("sweep-zero")]
    [InlineData("sweep-overturn")]
    [InlineData("sweep-nonfinite")]
    [InlineData("ticks-too-few")]
    [InlineData("ticks-too-many")]
    public void InvalidConfigurationRollsBackAnatomyAndTickLabelChanges(string setting) {
        var chart = Chart.Create().ConfigureGauge(options => {
            options.Orientation = ChartOrientation.Vertical; options.StartAngleDegrees = 45;
            options.SweepAngleDegrees = 180; options.TickCount = 3;
            options.TickLabels = new[] { new ChartAxisLabel(50, "Middle") };
        });
        var previous = chart.Options.Gauge;

        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ConfigureGauge(options => {
            options.Orientation = ChartOrientation.Horizontal; options.StartAngleDegrees = 90;
            options.SweepAngleDegrees = 270; options.TickCount = 5;
            options.TickLabels = new[] { new ChartAxisLabel(25, "Changed") };
            switch (setting) {
                case "form": options.Form = (ChartGaugeForm)99; break;
                case "orientation": options.Orientation = (ChartOrientation)99; break;
                case "start-nonfinite": options.StartAngleDegrees = double.NaN; break;
                case "sweep-zero": options.SweepAngleDegrees = 0; break;
                case "sweep-overturn": options.SweepAngleDegrees = 361; break;
                case "sweep-nonfinite": options.SweepAngleDegrees = double.PositiveInfinity; break;
                case "ticks-too-few": options.TickCount = 1; break;
                case "ticks-too-many": options.TickCount = 101; break;
                default: throw new InvalidOperationException("Unknown test setting.");
            }
        }));

        Assert.Same(previous, chart.Options.Gauge);
        Assert.Equal(ChartGaugeForm.Arc, previous.Form); Assert.Equal(ChartOrientation.Vertical, previous.Orientation);
        Assert.Equal(45, previous.StartAngleDegrees); Assert.Equal(180, previous.SweepAngleDegrees); Assert.Equal(3, previous.TickCount);
        Assert.Equal(50, Assert.Single(previous.TickLabels).Value); Assert.Equal("Middle", previous.TickLabels[0].Text);
    }

    [Fact]
    public void InvalidExplicitLabelsLeaveTheInstalledCaptionSnapshotIntact() {
        var options = new ChartGaugeOptions { TickLabels = new[] { new ChartAxisLabel(50, "Middle") } };
        var previous = options.TickLabels;

        Assert.Throws<ArgumentNullException>(() => options.TickLabels = null!);
        Assert.Throws<ArgumentNullException>(() => options.TickLabels = new[] { new ChartAxisLabel(25, "Valid first"), default });

        Assert.Same(previous, options.TickLabels);
        Assert.Equal(50, Assert.Single(options.TickLabels).Value); Assert.Equal("Middle", options.TickLabels[0].Text);
    }
}
