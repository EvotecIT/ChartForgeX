using System;
using System.Collections.Generic;

namespace ChartForgeX.Typography;

/// <summary>
/// The Unicode Bidirectional Algorithm (UAX #9) for one paragraph: paragraph level (P2–P3),
/// explicit embeddings, overrides, and isolates (X1–X10, with isolating run sequences), weak types
/// (W1–W7), paired brackets (N0), neutrals (N1–N2), implicit levels (I1–I2), whitespace reset (L1),
/// and reordering (L2). Mirroring (L4) is left to the caller through
/// <see cref="BidiCharacterData.Mirror"/>. Character types come from <see cref="BidiCharacterData"/>.
/// </summary>
internal static class UnicodeBidi {
    internal const int MaximumDepth = 125;
    private const int MaximumBracketStack = 63;

    /// <summary>True when the text holds right-to-left characters, Arabic digits, or explicit formatting, so its levels are not all the paragraph level.</summary>
    internal static bool NeedsResolution(IReadOnlyList<int> codePoints) {
        for (var i = 0; i < codePoints.Count; i++) {
            if (codePoints[i] >= 0x0590 && BidiCharacterData.IsRightToLeftOrExplicit(BidiCharacterData.Classify(codePoints[i]))) return true;
        }

        return false;
    }

    /// <summary>The paragraph level from the first strong character (P2–P3): 1 for R or AL, otherwise 0.</summary>
    internal static int ParagraphLevel(IReadOnlyList<int> codePoints) {
        var types = new BidiClass[codePoints.Count];
        for (var i = 0; i < types.Length; i++) types[i] = BidiCharacterData.Classify(codePoints[i]);
        return FirstStrong(types, MatchIsolates(types, out _), 0, types.Length) ?? 0;
    }

    /// <summary>
    /// Resolves the embedding level of every code point of one paragraph. <paramref name="paragraphLevel"/>
    /// is 0 (left-to-right), 1 (right-to-left), or -1 to take it from the first strong character.
    /// Removed characters (X9: embeddings, overrides, PDF, and boundary neutrals) take the level of
    /// the character before them.
    /// </summary>
    internal static byte[] ResolveLevels(IReadOnlyList<int> codePoints, int paragraphLevel) {
        var count = codePoints.Count;
        var original = new BidiClass[count];
        for (var i = 0; i < count; i++) original[i] = BidiCharacterData.Classify(codePoints[i]);
        var matchingPdi = MatchIsolates(original, out var matchingInitiator);
        if (paragraphLevel < 0) paragraphLevel = FirstStrong(original, matchingPdi, 0, count) ?? 0;
        var types = (BidiClass[])original.Clone();
        var levels = new byte[count];
        var removed = new bool[count];
        ResolveExplicit(original, types, levels, removed, matchingPdi, paragraphLevel);

        foreach (var sequence in IsolatingRunSequences(original, levels, removed, matchingPdi, matchingInitiator)) {
            ResolveSequence(codePoints, original, types, levels, removed, sequence, paragraphLevel);
        }

        ResetWhitespace(original, levels, removed, paragraphLevel);
        for (var i = 0; i < count; i++) {
            if (removed[i]) levels[i] = i > 0 ? levels[i - 1] : (byte)paragraphLevel;
        }

        return levels;
    }

    /// <summary>The logical indices of <paramref name="levels"/> in visual order (L2).</summary>
    internal static int[] VisualOrder(IReadOnlyList<byte> levels) {
        var order = new int[levels.Count];
        for (var i = 0; i < order.Length; i++) order[i] = i;
        if (order.Length == 0) return order;
        var highest = 0;
        var lowestOdd = int.MaxValue;
        foreach (var level in levels) {
            highest = Math.Max(highest, level);
            if ((level & 1) == 1) lowestOdd = Math.Min(lowestOdd, level);
        }

        for (var level = highest; level >= lowestOdd && level > 0; level--) {
            for (var start = 0; start < order.Length;) {
                if (levels[order[start]] < level) {
                    start++;
                    continue;
                }

                var end = start;
                while (end + 1 < order.Length && levels[order[end + 1]] >= level) end++;
                Array.Reverse(order, start, end - start + 1);
                start = end + 1;
            }
        }

        return order;
    }

    // BD9: each isolate initiator's matching PDI (or -1), and each PDI's initiator.
    private static int[] MatchIsolates(BidiClass[] types, out int[] matchingInitiator) {
        var matchingPdi = new int[types.Length];
        matchingInitiator = new int[types.Length];
        var open = new Stack<int>();
        for (var i = 0; i < types.Length; i++) {
            matchingPdi[i] = -1;
            matchingInitiator[i] = -1;
            if (IsIsolateInitiator(types[i])) open.Push(i);
            else if (types[i] == BidiClass.PDI && open.Count > 0) {
                var initiator = open.Pop();
                matchingPdi[initiator] = i;
                matchingInitiator[i] = initiator;
            } else if (types[i] == BidiClass.B) open.Clear();
        }

        return matchingPdi;
    }

    // P2: the first L, R, or AL, skipping isolated content.
    private static int? FirstStrong(BidiClass[] types, int[] matchingPdi, int start, int end) {
        for (var i = start; i < end; i++) {
            var type = types[i];
            if (type == BidiClass.L) return 0;
            if (type == BidiClass.R || type == BidiClass.AL) return 1;
            if (IsIsolateInitiator(type)) {
                if (matchingPdi[i] < 0) return null;
                i = matchingPdi[i];
            }
        }

        return null;
    }

    // X1–X9.
    private static void ResolveExplicit(BidiClass[] original, BidiClass[] types, byte[] levels, bool[] removed, int[] matchingPdi, int paragraphLevel) {
        var stackLevel = new byte[MaximumDepth + 2];
        var stackOverride = new BidiClass[MaximumDepth + 2];
        var stackIsolate = new bool[MaximumDepth + 2];
        var depth = 0;
        stackLevel[0] = (byte)paragraphLevel;
        stackOverride[0] = BidiClass.ON;
        var overflowIsolates = 0;
        var overflowEmbeddings = 0;
        var validIsolates = 0;

        for (var i = 0; i < original.Length; i++) {
            var type = original[i];
            switch (type) {
                case BidiClass.RLE:
                case BidiClass.LRE:
                case BidiClass.RLO:
                case BidiClass.LRO: {
                    var rtl = type == BidiClass.RLE || type == BidiClass.RLO;
                    var level = NextLevel(stackLevel[depth], rtl);
                    if (level <= MaximumDepth && overflowIsolates == 0 && overflowEmbeddings == 0) {
                        depth++;
                        stackLevel[depth] = (byte)level;
                        stackOverride[depth] = type == BidiClass.RLO ? BidiClass.R : type == BidiClass.LRO ? BidiClass.L : BidiClass.ON;
                        stackIsolate[depth] = false;
                    } else if (overflowIsolates == 0) {
                        overflowEmbeddings++;
                    }

                    levels[i] = stackLevel[depth];
                    removed[i] = true;
                    break;
                }
                case BidiClass.RLI:
                case BidiClass.LRI:
                case BidiClass.FSI: {
                    levels[i] = stackLevel[depth];
                    if (stackOverride[depth] != BidiClass.ON) types[i] = stackOverride[depth];
                    var rtl = type == BidiClass.RLI || (type == BidiClass.FSI && FirstStrong(original, matchingPdi, i + 1, matchingPdi[i] >= 0 ? matchingPdi[i] : original.Length) == 1);
                    var level = NextLevel(stackLevel[depth], rtl);
                    if (level <= MaximumDepth && overflowIsolates == 0 && overflowEmbeddings == 0) {
                        validIsolates++;
                        depth++;
                        stackLevel[depth] = (byte)level;
                        stackOverride[depth] = BidiClass.ON;
                        stackIsolate[depth] = true;
                    } else {
                        overflowIsolates++;
                    }

                    break;
                }
                case BidiClass.PDI:
                    if (overflowIsolates > 0) overflowIsolates--;
                    else if (validIsolates > 0) {
                        overflowEmbeddings = 0;
                        while (!stackIsolate[depth] && depth > 0) depth--;
                        if (depth > 0) depth--;
                        validIsolates--;
                    }

                    levels[i] = stackLevel[depth];
                    if (stackOverride[depth] != BidiClass.ON) types[i] = stackOverride[depth];
                    break;
                case BidiClass.PDF:
                    if (overflowIsolates == 0) {
                        if (overflowEmbeddings > 0) overflowEmbeddings--;
                        else if (!stackIsolate[depth] && depth > 0) depth--;
                    }

                    levels[i] = stackLevel[depth];
                    removed[i] = true;
                    break;
                case BidiClass.B:
                    levels[i] = (byte)paragraphLevel;
                    break;
                case BidiClass.BN:
                    levels[i] = stackLevel[depth];
                    removed[i] = true;
                    break;
                default:
                    levels[i] = stackLevel[depth];
                    if (stackOverride[depth] != BidiClass.ON) types[i] = stackOverride[depth];
                    break;
            }
        }
    }

    // X10: level runs chained through matching isolate initiators and PDIs.
    private static List<List<int>> IsolatingRunSequences(BidiClass[] original, byte[] levels, bool[] removed, int[] matchingPdi, int[] matchingInitiator) {
        var runs = new List<List<int>>();
        var runOf = new int[levels.Length];
        List<int>? current = null;
        var currentLevel = -1;
        for (var i = 0; i < levels.Length; i++) {
            if (removed[i]) continue;
            if (current == null || levels[i] != currentLevel) {
                current = new List<int>();
                runs.Add(current);
                currentLevel = levels[i];
            }

            current.Add(i);
            runOf[i] = runs.Count - 1;
        }

        var sequences = new List<List<int>>();
        foreach (var run in runs) {
            var first = run[0];
            if (original[first] == BidiClass.PDI && matchingInitiator[first] >= 0) continue;
            var sequence = new List<int>(run);
            while (true) {
                var last = sequence[sequence.Count - 1];
                if (!IsIsolateInitiator(original[last]) || matchingPdi[last] < 0) break;
                sequence.AddRange(runs[runOf[matchingPdi[last]]]);
            }

            sequences.Add(sequence);
        }

        return sequences;
    }

    private static void ResolveSequence(IReadOnlyList<int> codePoints, BidiClass[] original, BidiClass[] allTypes, byte[] levels, bool[] removed, List<int> sequence, int paragraphLevel) {
        var length = sequence.Count;
        var level = levels[sequence[0]];
        var before = sequence[0] - 1;
        while (before >= 0 && removed[before]) before--;
        var after = sequence[length - 1] + 1;
        while (after < levels.Length && removed[after]) after++;
        var levelBefore = before >= 0 ? levels[before] : paragraphLevel;
        var levelAfter = IsIsolateInitiator(original[sequence[length - 1]]) || after >= levels.Length ? paragraphLevel : levels[after];
        var sos = (Math.Max(level, levelBefore) & 1) == 1 ? BidiClass.R : BidiClass.L;
        var eos = (Math.Max(levels[sequence[length - 1]], levelAfter) & 1) == 1 ? BidiClass.R : BidiClass.L;
        var t = new BidiClass[length];
        for (var k = 0; k < length; k++) t[k] = allTypes[sequence[k]];

        // W1: marks take the type of what they follow (ON after an isolate initiator or PDI).
        for (var k = 0; k < length; k++) {
            if (t[k] != BidiClass.NSM) continue;
            var previous = k == 0 ? BidiClass.ON : original[sequence[k - 1]];
            t[k] = k == 0 ? sos : IsIsolateInitiator(previous) || previous == BidiClass.PDI ? BidiClass.ON : t[k - 1];
        }

        // W2: European digits after Arabic letters are Arabic digits. W3: AL is R.
        var lastStrong = sos;
        for (var k = 0; k < length; k++) {
            if (t[k] == BidiClass.L || t[k] == BidiClass.R || t[k] == BidiClass.AL) lastStrong = t[k];
            else if (t[k] == BidiClass.EN && lastStrong == BidiClass.AL) t[k] = BidiClass.AN;
        }

        for (var k = 0; k < length; k++) if (t[k] == BidiClass.AL) t[k] = BidiClass.R;

        // W4: a single separator between two numbers of the same kind joins them.
        for (var k = 1; k + 1 < length; k++) {
            if (t[k] == BidiClass.ES && t[k - 1] == BidiClass.EN && t[k + 1] == BidiClass.EN) t[k] = BidiClass.EN;
            else if (t[k] == BidiClass.CS && t[k - 1] == BidiClass.EN && t[k + 1] == BidiClass.EN) t[k] = BidiClass.EN;
            else if (t[k] == BidiClass.CS && t[k - 1] == BidiClass.AN && t[k + 1] == BidiClass.AN) t[k] = BidiClass.AN;
        }

        // W5: terminators next to European digits become digits.
        for (var k = 0; k < length; k++) {
            if (t[k] != BidiClass.ET) continue;
            var end = k;
            while (end < length && t[end] == BidiClass.ET) end++;
            var adjacent = (k > 0 && t[k - 1] == BidiClass.EN) || (end < length && t[end] == BidiClass.EN);
            if (adjacent) for (var j = k; j < end; j++) t[j] = BidiClass.EN;
            k = end - 1;
        }

        // W6: remaining separators and terminators are neutral.
        for (var k = 0; k < length; k++) {
            if (t[k] == BidiClass.ES || t[k] == BidiClass.ET || t[k] == BidiClass.CS) t[k] = BidiClass.ON;
        }

        // W7: European digits after left-to-right text are L.
        lastStrong = sos;
        for (var k = 0; k < length; k++) {
            if (t[k] == BidiClass.L || t[k] == BidiClass.R) lastStrong = t[k];
            else if (t[k] == BidiClass.EN && lastStrong == BidiClass.L) t[k] = BidiClass.L;
        }

        var embedding = (level & 1) == 1 ? BidiClass.R : BidiClass.L;
        ResolveBrackets(codePoints, original, sequence, t, sos, embedding);

        // N1–N2: neutrals between two runs of the same direction take it; others take the embedding direction.
        for (var k = 0; k < length; k++) {
            if (!IsNeutral(t[k])) continue;
            var end = k;
            while (end < length && IsNeutral(t[end])) end++;
            var leading = k == 0 ? sos : StrongDirection(t[k - 1]);
            var trailing = end >= length ? eos : StrongDirection(t[end]);
            var resolved = leading == trailing && leading != BidiClass.ON ? leading : embedding;
            for (var j = k; j < end; j++) t[j] = resolved;
            k = end - 1;
        }

        // I1–I2.
        for (var k = 0; k < length; k++) {
            var index = sequence[k];
            var current = levels[index];
            if ((current & 1) == 0) {
                if (t[k] == BidiClass.R) levels[index] = (byte)(current + 1);
                else if (t[k] == BidiClass.AN || t[k] == BidiClass.EN) levels[index] = (byte)(current + 2);
            } else if (t[k] == BidiClass.L || t[k] == BidiClass.EN || t[k] == BidiClass.AN) {
                levels[index] = (byte)(current + 1);
            }
        }
    }

    // N0: a bracket pair takes the embedding direction when its content has that direction, or the
    // opposite direction when only that is inside and the context before it agrees.
    private static void ResolveBrackets(IReadOnlyList<int> codePoints, BidiClass[] original, List<int> sequence, BidiClass[] t, BidiClass sos, BidiClass embedding) {
        var pairs = new List<(int Open, int Close)>();
        var openers = new List<(int Closing, int Position)>();
        for (var k = 0; k < sequence.Count; k++) {
            if (t[k] != BidiClass.ON) continue;
            var cp = codePoints[sequence[k]];
            var closing = BidiCharacterData.ClosingBracketFor(cp);
            if (closing >= 0) {
                if (openers.Count == MaximumBracketStack) break;
                openers.Add((BidiCharacterData.Canonical(closing), k));
            } else if (BidiCharacterData.IsClosingBracket(cp)) {
                var canonical = BidiCharacterData.Canonical(cp);
                for (var s = openers.Count - 1; s >= 0; s--) {
                    if (openers[s].Closing != canonical) continue;
                    pairs.Add((openers[s].Position, k));
                    openers.RemoveRange(s, openers.Count - s);
                    break;
                }
            }
        }

        pairs.Sort((a, b) => a.Open.CompareTo(b.Open));
        var opposite = embedding == BidiClass.L ? BidiClass.R : BidiClass.L;
        foreach (var (open, close) in pairs) {
            var foundEmbedding = false;
            var foundOpposite = false;
            for (var k = open + 1; k < close; k++) {
                var direction = StrongDirection(t[k]);
                if (direction == embedding) foundEmbedding = true;
                else if (direction == opposite) foundOpposite = true;
            }

            BidiClass resolved;
            if (foundEmbedding) resolved = embedding;
            else if (foundOpposite) {
                var context = sos;
                for (var k = open - 1; k >= 0; k--) {
                    var direction = StrongDirection(t[k]);
                    if (direction == BidiClass.ON) continue;
                    context = direction;
                    break;
                }

                resolved = context == opposite ? opposite : embedding;
            } else {
                continue;
            }

            SetBracket(original, sequence, t, open, resolved);
            SetBracket(original, sequence, t, close, resolved);
        }
    }

    private static void SetBracket(BidiClass[] original, List<int> sequence, BidiClass[] t, int position, BidiClass direction) {
        t[position] = direction;
        // Marks that followed the bracket (W1 made them ON) follow its new direction.
        for (var k = position + 1; k < sequence.Count && original[sequence[k]] == BidiClass.NSM; k++) t[k] = direction;
    }

    // L1: separators, and whitespace before them or at the end of the line, return to the paragraph level.
    private static void ResetWhitespace(BidiClass[] original, byte[] levels, bool[] removed, int paragraphLevel) {
        var trailing = true;
        for (var i = original.Length - 1; i >= 0; i--) {
            var type = original[i];
            if (type == BidiClass.S || type == BidiClass.B) {
                levels[i] = (byte)paragraphLevel;
                trailing = true;
            } else if (trailing && (type == BidiClass.WS || IsIsolateInitiator(type) || type == BidiClass.PDI || removed[i])) {
                levels[i] = (byte)paragraphLevel;
            } else {
                trailing = false;
            }
        }
    }

    private static int NextLevel(int level, bool rtl) => rtl ? (level + 1) | 1 : (level + 2) & ~1;

    private static bool IsIsolateInitiator(BidiClass type) => type == BidiClass.LRI || type == BidiClass.RLI || type == BidiClass.FSI;

    private static bool IsNeutral(BidiClass type) =>
        type == BidiClass.B || type == BidiClass.S || type == BidiClass.WS || type == BidiClass.ON || IsIsolateInitiator(type) || type == BidiClass.PDI;

    // L, or R for R and numbers; ON for anything else.
    private static BidiClass StrongDirection(BidiClass type) =>
        type == BidiClass.L ? BidiClass.L : type == BidiClass.R || type == BidiClass.EN || type == BidiClass.AN ? BidiClass.R : BidiClass.ON;
}
