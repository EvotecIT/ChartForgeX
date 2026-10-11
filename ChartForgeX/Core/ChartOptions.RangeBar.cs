namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the range-bar orientation settings shared by static exports.</summary>
    public ChartRangeBarOptions RangeBar { get; } = new();
}
