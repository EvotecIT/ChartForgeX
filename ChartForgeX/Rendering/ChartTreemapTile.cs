using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>A retained node rectangle and its bounded child and header areas.</summary>
internal readonly struct ChartTreemapTile {
    internal ChartTreemapTile(int itemIndex, ChartRect rect, ChartRect content, ChartRect header) {
        ItemIndex = itemIndex;
        Rect = rect; ContentRect = content; HeaderRect = header;
    }

    internal int ItemIndex { get; }
    internal ChartRect Rect { get; }
    internal ChartRect ContentRect { get; }
    internal ChartRect HeaderRect { get; }
}
