using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the shared source-size domain and logical radius options for bubble series.</summary>
    public ChartBubbleOptions Bubble {
        get => _bubble;
        set => _bubble = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartBubbleOptions _bubble = new();
}
