using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AxisLabelReplacementTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingAxisLabelsCanBeUsedAsTheirOwnReplacement(bool deferred) {
        var chart = Create();
        var labels = chart.Options.XAxis.Labels;
        var before = labels.ToArray();

        Assert.Same(chart, chart.WithXLabels(deferred ? labels.Where(label => label.Value > 0) : labels));

        Assert.Same(labels, chart.Options.XAxis.Labels);
        Assert.Equal(before, labels);
    }

    [Fact]
    public void RejectedStringLabelsRetainThePreviousLabels() {
        var chart = Create();
        var before = chart.Options.XAxis.Labels.ToArray();

        Assert.Throws<ArgumentNullException>(() => chart.WithXLabels("Replacement", null!));

        Assert.Equal(before, chart.Options.XAxis.Labels);
    }

    [Fact]
    public void RejectedExplicitLabelsRetainThePreviousLabels() {
        var chart = Create();
        var before = chart.Options.XAxis.Labels.ToArray();

        Assert.Throws<ArgumentException>(() => chart.WithXLabels(new[] { new ChartAxisLabel(1, "Replacement"), default(ChartAxisLabel) }));

        Assert.Equal(before, chart.Options.XAxis.Labels);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedEnumerationRetainsThePreviousLabels(bool dates) {
        var chart = Create();
        var before = chart.Options.XAxis.Labels.ToArray();

        if (dates) Assert.Throws<InvalidOperationException>(() => chart.WithXDateLabels(FailAfter(new DateTime(2026, 1, 1))));
        else Assert.Throws<InvalidOperationException>(() => chart.WithXLabels(FailAfter(new ChartAxisLabel(1, "Replacement"))));

        Assert.Equal(before, chart.Options.XAxis.Labels);
    }

    [Fact]
    public void FailedDateFormattingRetainsThePreviousLabels() {
        var chart = Create();
        var before = chart.Options.XAxis.Labels.ToArray();

        Assert.Throws<FormatException>(() => chart.WithXDateLabels(new[] { new DateTime(2026, 1, 1) }, "%"));

        Assert.Equal(before, chart.Options.XAxis.Labels);
    }

    private static Chart Create() => Chart.Create().WithXLabels("North", "South");

    private static IEnumerable<T> FailAfter<T>(T first) {
        yield return first;
        throw new InvalidOperationException("Source enumeration failed.");
    }
}
