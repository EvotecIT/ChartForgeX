using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AxisPaintBoundsStorageTests {
    [Fact]
    public void RepeatedMarkerSizesFitWithoutRetainingEveryObservation() {
        const int count = 1_000_000;
        var bounds = new VisualAxisPaintBounds();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < count; index++) bounds.Include(index / (double)(count - 1), 12);
        var changed = bounds.TryExpand(0, 1, 640, false, false, out var minimum, out var maximum);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(changed);
        Assert.Equal(-12d / 616, minimum, 8);
        Assert.Equal(1 + 12d / 616, maximum, 8);
        Assert.True(allocated < 64 * 1024, "Axis fitting retained " + allocated + " bytes for repeated marker sizes.");
    }
}
