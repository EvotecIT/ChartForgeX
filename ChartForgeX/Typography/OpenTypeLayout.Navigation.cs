using System.Collections.Generic;

namespace ChartForgeX.Typography;

internal sealed partial class OpenTypeLayout {
    /// <summary>GPOS does not change glyph order: reuse the nearest eligible base instead of rescanning marks.</summary>
    private int PreviousBase(List<LayoutGlyph> glyphs, int index, int flags, int filter, LayoutExecution execution) {
        if (execution.PreviousBases == null || execution.BaseFlags != flags || execution.BaseFilter != filter) {
            var positions = new int[glyphs.Count]; var previous = -1;
            for (var i = 0; i < glyphs.Count; i++) {
                if (--execution.Remaining < 0) throw new FontLayoutException();
                positions[i] = previous;
                var glyph = glyphs[i];
                if (!glyph.Ignorable && GlyphClass(glyph) != 3 && !Skipped(glyph, flags, filter)) previous = i;
            }
            execution.PreviousBases = positions;
            execution.BaseFlags = flags; execution.BaseFilter = filter;
        }
        return execution.PreviousBases[index];
    }
}
