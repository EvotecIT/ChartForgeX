namespace ChartForgeX.Terminal;

/// <summary>Unicode 17.0 dual, left, right, and non-control join-causing characters used by contextual raster fallback.</summary>
internal static class TerminalJoiningType {
    private static readonly int[] ContextualRanges = {
        0x0620, 0x0620,
        0x0622, 0x064A,
        0x066E, 0x066F,
        0x0671, 0x0673,
        0x0675, 0x06D3,
        0x06D5, 0x06D5,
        0x06EE, 0x06EF,
        0x06FA, 0x06FC,
        0x06FF, 0x06FF,
        0x0710, 0x0710,
        0x0712, 0x072F,
        0x074D, 0x077F,
        0x07CA, 0x07EA,
        0x07FA, 0x07FA,
        0x0840, 0x0858,
        0x0860, 0x0860,
        0x0862, 0x0865,
        0x0867, 0x086A,
        0x0870, 0x0886,
        0x0889, 0x088F,
        0x08A0, 0x08AC,
        0x08AE, 0x08C8,
        0x1807, 0x1807,
        0x180A, 0x180A,
        0x1820, 0x1878,
        0x1887, 0x18A8,
        0x18AA, 0x18AA,
        0xA840, 0xA872,
        0x10AC0, 0x10AC5,
        0x10AC7, 0x10AC7,
        0x10AC9, 0x10ACA,
        0x10ACD, 0x10AE1,
        0x10AE4, 0x10AE4,
        0x10AEB, 0x10AEF,
        0x10B80, 0x10B91,
        0x10BA9, 0x10BAE,
        0x10D00, 0x10D23,
        0x10EC2, 0x10EC4,
        0x10EC6, 0x10EC7,
        0x10F30, 0x10F44,
        0x10F51, 0x10F54,
        0x10F70, 0x10F81,
        0x10FB0, 0x10FB0,
        0x10FB2, 0x10FB6,
        0x10FB8, 0x10FBF,
        0x10FC1, 0x10FC4,
        0x10FC9, 0x10FCB,
        0x1E900, 0x1E943
    };

    public static bool RequiresContextualShaping(int codePoint) {
        var low = 0;
        var high = ContextualRanges.Length / 2 - 1;
        while (low <= high) {
            var middle = low + (high - low) / 2;
            var start = ContextualRanges[middle * 2];
            var end = ContextualRanges[middle * 2 + 1];
            if (codePoint < start) {
                high = middle - 1;
            } else if (codePoint > end) {
                low = middle + 1;
            } else {
                return true;
            }
        }
        return false;
    }
}
