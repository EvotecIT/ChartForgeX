using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class WorldMapDots {
    private static ChartPoint[][]? _worldBoundaries;

    public static ChartPoint[][] WorldBoundaries {
        get {
            if (_worldBoundaries != null) return _worldBoundaries;
            var count = NorthAmericaBoundaries.Length +
                SouthAmericaBoundaries.Length +
                AfricaBoundaries.Length +
                EuropeBoundaries.Length +
                AsiaBoundaries.Length +
                OceaniaBoundaries.Length;
            var boundaries = new ChartPoint[count][];
            var index = 0;
            CopyBoundaries(NorthAmericaBoundaries, boundaries, ref index);
            CopyBoundaries(SouthAmericaBoundaries, boundaries, ref index);
            CopyBoundaries(AfricaBoundaries, boundaries, ref index);
            CopyBoundaries(EuropeBoundaries, boundaries, ref index);
            CopyBoundaries(AsiaBoundaries, boundaries, ref index);
            CopyBoundaries(OceaniaBoundaries, boundaries, ref index);
            _worldBoundaries = boundaries;
            return boundaries;
        }
    }

    private static void CopyBoundaries(ChartPoint[][] source, ChartPoint[][] target, ref int index) {
        for (var i = 0; i < source.Length; i++) target[index++] = source[i];
    }
}
