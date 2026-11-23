using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the range-bar orientation and caption settings shared by static exports.</summary>
    public ChartRangeBarOptions RangeBar {
        get => _rangeBar;
        set => _rangeBar = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartRangeBarOptions _rangeBar = new();
}
