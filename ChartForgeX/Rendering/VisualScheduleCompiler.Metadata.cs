using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static void DependencyMetadata(Dictionary<string, string> metadata, IReadOnlyDictionary<int, int[]> predecessors, int successor) {
        metadata["data-cfx-dependency"] = (predecessors.TryGetValue(successor, out var incoming) ? incoming[0] : -1).ToString(CultureInfo.InvariantCulture);
        if (incoming != null && incoming.Length > 1)
            metadata["data-cfx-dependencies"] = string.Join(",", incoming.Select(index => index.ToString(CultureInfo.InvariantCulture)));
    }
}
