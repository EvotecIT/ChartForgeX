using System.Collections.Generic;

namespace ChartForgeX.Typography;

internal sealed partial class OpenTypeLayout {
    /// <summary>The first matching GSUB/GPOS 1.1 condition set supplies alternate feature tables.</summary>
    private Dictionary<int, int> FeatureVariations(FontTableReader table) {
        var result = new Dictionary<int, int>();
        if (_variation == null || table.U16(2) < 1) return result;
        try {
            var variations = table.Offset(0, 10, optional: true, wide: true);
            if (variations < 0 || table.U16(variations) != 1) return result;
            var count = table.U32(variations + 4);
            if (count > 4096) return result;
            table.Require(variations + 8, (int)count * 8);
            for (var i = 0; i < count; i++) {
                var record = variations + 8 + i * 8;
                var conditions = table.Offset(variations, record, optional: true, wide: true); var matches = true;
                if (conditions >= 0) {
                    var number = table.U16(conditions); if (number > 32) return result;
                    table.Require(conditions + 2, number * 4);
                    for (var j = 0; j < number; j++) {
                        var condition = table.Offset(conditions, conditions + 2 + j * 4, wide: true);
                        if (table.U16(condition) != 1) { matches = false; break; }
                        var axis = table.U16(condition + 2);
                        if (axis >= _variation.Coordinates.Length || _variation.Coordinates[axis] < table.F2Dot14(condition + 4) || _variation.Coordinates[axis] > table.F2Dot14(condition + 6)) { matches = false; break; }
                    }
                }
                if (!matches) continue;
                var substitutions = table.Offset(variations, record + 4, wide: true);
                if (table.U16(substitutions) != 1) return result;
                var replacements = table.U16(substitutions + 4); table.Require(substitutions + 6, replacements * 6);
                for (var j = 0; j < replacements; j++) {
                    var replacement = substitutions + 6 + j * 6;
                    result[table.U16(replacement)] = table.Offset(substitutions, replacement + 2, wide: true);
                }
                return result;
            }
        } catch (FontLayoutException) { result.Clear(); }
        return result;
    }
}
