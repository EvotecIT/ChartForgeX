using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GroupedCategoryAxisModelTests {
    [Fact]
    public void ParentPathsCopyCallerStorageAndKeepAuthoredCaptions() {
        var parents = new[] { " 2026 ", "Q1" };
        var label = new ChartAxisLabel(1.5, "Jan", parents);
        parents[0] = "Changed";
        Assert.Equal(new[] { " 2026 ", "Q1" }, label.Groups);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)label.Groups)[0] = "Changed");
        Assert.Equal(1.5, label.Value);
        Assert.Equal("Jan", label.Text);
    }

    [Fact]
    public void LabelsCompareCoordinatesAndOrderedCaptionPathsByValue() {
        var first = new ChartAxisLabel(1.5, "Jan", "2026", "Q1");
        var equal = new ChartAxisLabel(1.5, "Jan", new[] { "2026", "Q1" });
        Assert.Equal(first, equal);
        Assert.True(first == equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.Single(new HashSet<ChartAxisLabel> { first, equal });
        Assert.True(first != new ChartAxisLabel(1.5, "Jan", "2026", "q1"));
        Assert.NotEqual(first, new ChartAxisLabel(1.5, "Jan", "Q1", "2026"));
        Assert.NotEqual(first, new ChartAxisLabel(1.5, "Feb", "2026", "Q1"));
        Assert.NotEqual(first, new ChartAxisLabel(2.5, "Jan", "2026", "Q1"));
    }

    [Fact]
    public void ExistingNumericAndDateConstructorsKeepEmptyPathValueSemantics() {
        var numeric = new ChartAxisLabel(1.5, "Leaf");
        var explicitEmpty = new ChartAxisLabel(1.5, "Leaf", Array.Empty<string>());
        Assert.Empty(numeric.Groups);
        Assert.Equal(numeric, explicitEmpty);
        Assert.Equal(numeric.GetHashCode(), explicitEmpty.GetHashCode());
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dated = new ChartAxisLabel(date, "Jan");
        Assert.Empty(dated.Groups);
        Assert.Equal(new ChartAxisLabel(date.ToOADate(), "Jan"), dated);
        Assert.Empty(default(ChartAxisLabel).Groups);
        Assert.Equal(default(ChartAxisLabel), default(ChartAxisLabel));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void InvalidParentElementsAreRejected(string? parent) {
        Assert.Throws<ArgumentException>(() => new ChartAxisLabel(1, "Jan", new[] { parent! }));
    }

    [Fact]
    public void GroupedConstructorRetainsCoordinateAndNullInputValidation() {
        Assert.Throws<ArgumentNullException>(() => new ChartAxisLabel(1, "Jan", (string[])null!));
        Assert.Throws<ArgumentNullException>(() => new ChartAxisLabel(1, null!, "Q1"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxisLabel(double.NaN, "Jan", "Q1"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxisLabel(double.PositiveInfinity, "Jan", "Q1"));
    }
}
