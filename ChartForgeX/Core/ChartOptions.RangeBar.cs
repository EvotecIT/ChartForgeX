namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the range-bar orientation and caption settings shared by static exports.</summary>
    public ChartRangeBarOptions RangeBar { get; internal set; } = new();
}
