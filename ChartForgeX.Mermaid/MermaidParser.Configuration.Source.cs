using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private readonly struct ConfigurationJsonLine {
        public ConfigurationJsonLine(int offset, int line, int column) { Offset = offset; Line = line; Column = column; }
        public int Offset { get; }
        public int Line { get; }
        public int Column { get; }
    }

    private static MermaidSourceSpan ConfigurationJsonPropertySpan(GeoJsonValue value, IReadOnlyList<int> normalizedOffsets,
        List<ConfigurationJsonLine> lines, IReadOnlyList<int> sourceLineStarts, MermaidSourceSpan fallback) {
        if (value.PropertyOffset < 0 || value.PropertyLength <= 0 || lines.Count == 0) return fallback;
        var start = Locate(normalizedOffsets[value.PropertyOffset]);
        var end = Locate(normalizedOffsets[value.PropertyOffset + value.PropertyLength - 1]);
        var length = sourceLineStarts[end.Line - 1] + end.Column - sourceLineStarts[start.Line - 1] - start.Column + 1;
        return new MermaidSourceSpan(start.Line, start.Column, length);

        ConfigurationJsonLine Locate(int offset) {
            var low = 0;
            var high = lines.Count - 1;
            while (low < high) {
                var middle = low + (high - low + 1) / 2;
                if (lines[middle].Offset <= offset) low = middle;
                else high = middle - 1;
            }
            var line = lines[low];
            return new ConfigurationJsonLine(offset, line.Line, line.Column + offset - line.Offset);
        }
    }
}
