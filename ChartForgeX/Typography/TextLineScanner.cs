using System;
using System.Collections.Generic;

namespace ChartForgeX.Typography;

internal readonly struct TextLineSlice {
    internal TextLineSlice(int start, int length) {
        Start = start;
        Length = length;
    }

    internal int Start { get; }
    internal int Length { get; }

    internal string Read(string text) => text.Substring(Start, Length);
}

internal static class TextLineScanner {
    internal static IEnumerable<TextLineSlice> Enumerate(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var start = 0;
        for (var index = 0; index < text.Length; index++) {
            if (text[index] != '\r' && text[index] != '\n') continue;
            yield return new TextLineSlice(start, index - start);
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
            start = index + 1;
        }
        yield return new TextLineSlice(start, text.Length - start);
    }
}
