using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GroupedCategoryAxisPlanTests {
    [Fact]
    public void FlatNumericAliasesKeepTheirExistingUnboundFastPath() {
        var chart = Chart.Create().AddLine("Observed", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(0, "Origin"));
        chart.Options.XAxis.LabelFormatter = _ => throw new InvalidOperationException("A plan must not evaluate a formatter.");
        Assert.Null(ChartCategoryGroupPlan.TryCreate(chart));
        ChartGuards.RenderCompatibility(chart, preparing: true);
        Assert.Equal("Origin", ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxis.Labels, 0));
    }

    [Fact]
    public void RunsUseSortedContiguousPrefixesIncludingUngroupedAndMissingLeaves() {
        var values = new[] { 1.5, 8.5, 10.5, 12.5, 14.5 };
        var series = ChartSeries.CreateBar(new ChartSeriesId("observations"), "Observed", values.Select(value => new ChartPoint(value, value + 1)));
        series.WithObservationId(0, new ChartObservationId("jan"));
        var chart = Chart.Create().AddSeries(series).WithXLabels(new[] {
            new ChartAxisLabel(14.5, "Jan", "2027", "Q1"), new ChartAxisLabel(3.5, "Feb", "2026", "Q1"),
            new ChartAxisLabel(1.5, "Jan", "2026", "Q1"), new ChartAxisLabel(8.5, "Mar", "2026", "Q2"),
            new ChartAxisLabel(10.5, "Gap"), new ChartAxisLabel(12.5, "Apr", "2026", "Q1")
        });
        var before = series.RenderPoints.ToArray();
        var plan = Assert.IsType<ChartCategoryGroupPlan>(ChartCategoryGroupPlan.TryCreate(chart));
        Assert.False(plan.Horizontal);
        Assert.Equal(new[] { 1.5, 3.5, 8.5, 10.5, 12.5, 14.5 }, plan.Catalog.Select(label => label.Value));
        Assert.Equal(values, plan.Bindings.Select(label => label.Value));
        Assert.Equal(new[] {
            (0, "2026", 0, 2), (0, "2026", 4, 4), (0, "2027", 5, 5),
            (1, "2026/Q1", 0, 1), (1, "2026/Q2", 2, 2), (1, "2026/Q1", 4, 4), (1, "2027/Q1", 5, 5)
        }, plan.Runs.Select(run => (run.Depth, string.Join("/", run.Path), run.FirstLeafIndex, run.LastLeafIndex)));
        Assert.Equal(new[] { (0d, 9.5), (11.5, 13.5), (13.5, 16d), (0d, 6d), (6d, 9.5), (11.5, 13.5), (13.5, 16d) },
            plan.CreateSpans(0, 16).Select(span => (span.Start, span.End)));
        Assert.Equal((1.5, 8.5), (plan.Runs[0].Minimum, plan.Runs[0].Maximum));
        Assert.Equal(before, series.RenderPoints);
        Assert.Equal("observations", series.Id.Value);
        Assert.Equal("jan", series.ObservationIds[0].Value);
        Assert.Equal(values.Length, series.SourcePointCount);
    }

    [Fact]
    public void PlansAndObservedBindingPathsDetachWithoutReplacingCapturedFormatterText() {
        var source = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(1.5) + 3);
        var parents = new[] { "North" };
        var chart = Chart.Create().AddBar("Observed", new[] { new ChartPoint(source, 7) })
            .WithXLabels(new[] { new ChartAxisLabel(1.5, "Central", parents) });
        var calls = 0;
        var plan = Assert.IsType<ChartCategoryGroupPlan>(ChartCategoryGroupPlan.TryCreate(chart));
        var bindings = Assert.IsAssignableFrom<IReadOnlyList<ChartAxisLabel>>(ChartCartesianCategoryLabels.CaptureBindings(
            chart, chart.Options.XAxis, _ => { calls++; return "Captured category"; }));
        Assert.Equal(1, calls);
        Assert.Equal(source, Assert.Single(plan.Bindings).Value);
        Assert.Equal("Central", plan.Bindings[0].Text);
        Assert.Equal("Captured category", Assert.Single(bindings).Text);
        Assert.Equal(source, bindings[0].Value);
        parents[0] = "Changed";
        chart.Options.XAxis.Labels.Clear(); chart.Options.XAxis.Reversed = true;
        var axisCopy = plan.Axis;
        axisCopy.Labels.Clear(); axisCopy.Reversed = true;
        Assert.False(plan.Axis.Reversed);
        Assert.Single(plan.Axis.Labels);
        Assert.Equal("North", Assert.Single(plan.Catalog[0].Groups));
        Assert.Equal("North", Assert.Single(bindings[0].Groups));
        Assert.Equal("North", Assert.Single(plan.Runs[0].Path));
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("bar-y")]
    [InlineData("bar-legacy")]
    [InlineData("line-y")]
    [InlineData("step-y")]
    public void HorizontalBindingsUseTheExistingPhysicalCategoryRouting(string route) {
        var points = new[] { new ChartPoint(1.5, 2), new ChartPoint(3.5, 3) };
        var chart = Chart.Create();
        if (route.StartsWith("bar", StringComparison.Ordinal)) chart.AddHorizontalBar("Observed", points);
        else {
            if (route == "line-y") chart.AddLine("Observed", points); else chart.AddStepLine("Observed", points);
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
        }
        var catalog = new[] { new ChartAxisLabel(1.5, "Central", "North"), new ChartAxisLabel(3.5, "Central", "South") };
        if (route == "bar-legacy") {
            chart.WithXLabels(catalog);
            chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic;
        } else {
            chart.Options.YAxis.Labels.AddRange(catalog);
            chart.Options.XAxis.Labels.Add(new ChartAxisLabel(2, "Value alias"));
        }
        var plan = Assert.IsType<ChartCategoryGroupPlan>(ChartCategoryGroupPlan.TryCreate(chart));
        Assert.True(plan.Horizontal);
        Assert.Equal(catalog, plan.Catalog);
        Assert.Equal(new[] { "North", "South" }, plan.Bindings.Select(label => Assert.Single(label.Groups)));
        Assert.Equal(ChartScaleKind.Linear, plan.Axis.Scale);
    }

    [Theory]
    [InlineData("value")]
    [InlineData("secondary")]
    [InlineData("time")]
    [InlineData("logarithmic")]
    [InlineData("radial")]
    [InlineData("sparkline")]
    [InlineData("histogram")]
    [InlineData("ignored-legacy")]
    public void UnsupportedGroupedOwnershipFailsAtCanonicalPreparationValidation(string configuration) {
        var chart = Single();
        switch (configuration) {
            case "value": chart.Options.YAxis.Labels.Add(new ChartAxisLabel(7, "Value", "Group")); break;
            case "secondary": chart.Options.SecondaryYAxis.Labels.Add(new ChartAxisLabel(7, "Value", "Group")); break;
            case "time": chart.Options.XAxis.Scale = ChartScaleKind.Time; break;
            case "logarithmic": chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic; break;
            case "radial": chart = Chart.Create().AddPie("Share", new[] { new ChartPoint(1, 7) }).WithXLabels(chart.Options.XAxis.Labels); break;
            case "sparkline": chart.WithSparkline(); break;
            case "histogram": chart = Chart.Create().AddHistogram("Counts", new[] { 1d, 2, 3 }, 2).WithXLabels(chart.Options.XAxis.Labels); break;
            case "ignored-legacy":
                chart = Chart.Create().AddHorizontalBar("Observed", new[] { new ChartPoint(1, 7) }).WithXLabels(chart.Options.XAxis.Labels);
                chart.Options.YAxis.Labels.Add(new ChartAxisLabel(1, "Physical label")); break;
        }
        Assert.Throws<NotSupportedException>(() => ChartGuards.RenderCompatibility(chart, preparing: true));
    }

    [Theory]
    [InlineData("ragged")]
    [InlineData("missing")]
    [InlineData("conflicting")]
    [InlineData("ambiguous")]
    public void CatalogValidationRejectsUnresolvedOrConflictingMembership(string configuration) {
        var chart = Single();
        switch (configuration) {
            case "ragged": chart.Options.XAxis.Labels.Add(new ChartAxisLabel(2, "Other", "Outer", "Inner")); break;
            case "missing": chart.Series[0].Points[0] = new ChartPoint(2, 7); break;
            case "conflicting": chart.Options.XAxis.Labels.Add(new ChartAxisLabel(1, "Leaf", "Other group")); break;
            case "ambiguous":
                chart.Series[0].Points[0] = new ChartPoint(1 + 0.00000025, 7);
                chart.Options.XAxis.Labels.Add(new ChartAxisLabel(1 + 0.0000005, "Other leaf", "Other group")); break;
        }
        Assert.Throws<InvalidOperationException>(() => ChartCategoryGroupPlan.TryCreate(chart));
    }

    [Fact]
    public void ExactMappingsWinBeforeGroupedAmbiguityWithoutChangingFlatFallback() {
        var labels = new[] { new ChartAxisLabel(1, "First", "A"), new ChartAxisLabel(1 + 0.0000005, "Second", "B") };
        Assert.Equal(labels[1], ChartAxisValueFormatter.FindExplicitMapping(labels, labels[1].Value, rejectAmbiguous: true));
        Assert.Equal("First", ChartAxisValueFormatter.FindExplicitLabel(labels, 1 + 0.00000025));
        Assert.Throws<InvalidOperationException>(() => ChartAxisValueFormatter.FindExplicitMapping(labels, 1 + 0.00000025, rejectAmbiguous: true));
        Assert.Null(ChartAxisValueFormatter.FindExplicitLabel(labels, double.NaN));
        var chart = Single(); chart.Options.XAxis.Labels.Add(chart.Options.XAxis.Labels[0]);
        Assert.Single(Assert.IsType<ChartCategoryGroupPlan>(ChartCategoryGroupPlan.TryCreate(chart)).Catalog);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceCellMidpointsRemainFiniteAcrossExtremeNumericCatalogs(bool oppositeSigns) {
        var left = oppositeSigns ? -double.MaxValue : double.MaxValue / 2;
        var right = double.MaxValue;
        var chart = Chart.Create().AddBar("Observed", new[] { new ChartPoint(left, 1), new ChartPoint(right, 2) })
            .WithXLabels(new[] { new ChartAxisLabel(left, "First", "A"), new ChartAxisLabel(right, "Second", "B") });
        var plan = Assert.IsType<ChartCategoryGroupPlan>(ChartCategoryGroupPlan.TryCreate(chart));
        var spans = plan.CreateSpans(left, right);
        var midpoint = oppositeSigns ? 0 : left + (right - left) / 2;
        Assert.True(double.IsFinite(spans[0].End));
        Assert.Equal(midpoint, spans[0].End);
        Assert.Equal(spans[0].End, spans[1].Start);
        Assert.Equal(left, spans[0].Start);
        Assert.Equal(right, spans[1].End);
    }

    [Fact]
    public void SingletonAndUnobservedEdgeCellsKeepFullSourceBoundsWithoutAddingObservations() {
        var chart = Single();
        var singleton = Assert.IsType<ChartCategoryGroupPlan>(ChartCategoryGroupPlan.TryCreate(chart));
        var span = Assert.Single(singleton.CreateSpans(0, 2));
        Assert.Equal((0d, 2d), (span.Start, span.End));
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(-5, "Earlier", "Group"));
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(20, "Later", "Group"));
        var plan = Assert.IsType<ChartCategoryGroupPlan>(ChartCategoryGroupPlan.TryCreate(chart));
        Assert.Single(plan.Bindings);
        Assert.Equal(1, chart.Series[0].SourcePointCount);
        var full = Assert.Single(plan.CreateSpans(0, 2));
        Assert.Equal((-5d, 20d), (full.Start, full.End));
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.CreateSpans(2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.CreateSpans(double.NaN, 2));
    }

    private static Chart Single() => Chart.Create().AddBar("Observed", new[] { new ChartPoint(1, 7) })
        .WithXLabels(new[] { new ChartAxisLabel(1, "Leaf", "Group") });
}
