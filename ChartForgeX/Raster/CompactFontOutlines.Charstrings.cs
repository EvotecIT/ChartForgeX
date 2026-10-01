using System;
using System.IO;

namespace ChartForgeX.Raster;

internal sealed partial class CompactFontOutlines {
    private const int MaximumStack = 513;
    private const int MaximumSubroutineDepth = 10;
    private const int MaximumOperations = 200000;

    /// <summary>
    /// Runs the Type 2 charstring of <paramref name="glyph"/> and sends its outline to
    /// <paramref name="sink"/> in font units. A malformed charstring ends the outline where it failed.
    /// </summary>
    internal void DrawGlyph(int glyph, IGlyphOutlineSink sink) {
        if (glyph < 0 || glyph >= GlyphCount) return;
        try {
            var fd = FontDictIndex(glyph);
            if (fd < 0 || fd >= _localSubrs.Length) fd = 0;
            var state = new CharstringState(sink, _scales[fd]);
            Run(glyph, fd, state, 0, 0, 0);
        } catch (InvalidDataException) {
            sink.Close();
        } catch (IndexOutOfRangeException) {
            sink.Close();
        }
    }

    private void Run(int glyph, int fd, CharstringState state, double originX, double originY, int seacDepth) {
        var (start, end) = _charStrings.Item(glyph);
        state.X = originX;
        state.Y = originY;
        state.StemCount = 0;
        state.HaveWidth = _cff2;
        state.Count = 0;
        Execute(start, end, fd, state, 0, seacDepth);
        state.ClosePath();
    }

    // Returns true when the glyph ended (endchar), so callers stop unwinding subroutines.
    private bool Execute(int p, int end, int fd, CharstringState s, int depth, int seacDepth) {
        if (depth > MaximumSubroutineDepth) throw new InvalidDataException("CFF subroutines nest too deeply.");
        while (p < end) {
            if (++s.Operations > MaximumOperations) throw new InvalidDataException("CFF charstring runs too long.");
            int b0 = Byte(p++);
            if (b0 >= 32 || b0 == 28) {
                double value;
                if (b0 == 28) {
                    value = (short)((Byte(p) << 8) | Byte(p + 1));
                    p += 2;
                } else if (b0 <= 246) {
                    value = b0 - 139;
                } else if (b0 <= 250) {
                    value = (b0 - 247) * 256 + Byte(p++) + 108;
                } else if (b0 <= 254) {
                    value = -(b0 - 251) * 256 - Byte(p++) - 108;
                } else {
                    value = ((Byte(p) << 24) | (Byte(p + 1) << 16) | (Byte(p + 2) << 8) | Byte(p + 3)) / 65536.0;
                    p += 4;
                }

                s.Push(value);
                continue;
            }

            switch (b0) {
                case 1: // hstem
                case 3: // vstem
                case 18: // hstemhm
                case 23: // vstemhm
                    CountStems(s);
                    break;
                case 19: // hintmask
                case 20: // cntrmask
                    CountStems(s);
                    p += (s.StemCount + 7) / 8;
                    break;
                case 21: // rmoveto
                    TakeWidth(s, 2);
                    s.MoveTo(s.Arg(s.Count - 2), s.Arg(s.Count - 1));
                    s.Count = 0;
                    break;
                case 22: // hmoveto
                    TakeWidth(s, 1);
                    s.MoveTo(s.Arg(s.Count - 1), 0);
                    s.Count = 0;
                    break;
                case 4: // vmoveto
                    TakeWidth(s, 1);
                    s.MoveTo(0, s.Arg(s.Count - 1));
                    s.Count = 0;
                    break;
                case 5: // rlineto
                    for (var i = 0; i + 1 < s.Count; i += 2) s.LineTo(s.Arg(i), s.Arg(i + 1));
                    s.Count = 0;
                    break;
                case 6: // hlineto
                case 7: // vlineto
                    for (var i = 0; i < s.Count; i++) {
                        if (((i & 1) == 0) == (b0 == 6)) s.LineTo(s.Arg(i), 0);
                        else s.LineTo(0, s.Arg(i));
                    }

                    s.Count = 0;
                    break;
                case 8: // rrcurveto
                    for (var i = 0; i + 5 < s.Count; i += 6) s.CurveTo(s.Arg(i), s.Arg(i + 1), s.Arg(i + 2), s.Arg(i + 3), s.Arg(i + 4), s.Arg(i + 5));
                    s.Count = 0;
                    break;
                case 24: { // rcurveline
                    var i = 0;
                    for (; s.Count - i >= 8; i += 6) s.CurveTo(s.Arg(i), s.Arg(i + 1), s.Arg(i + 2), s.Arg(i + 3), s.Arg(i + 4), s.Arg(i + 5));
                    if (s.Count - i >= 2) s.LineTo(s.Arg(i), s.Arg(i + 1));
                    s.Count = 0;
                    break;
                }
                case 25: { // rlinecurve
                    var i = 0;
                    for (; s.Count - i >= 8; i += 2) s.LineTo(s.Arg(i), s.Arg(i + 1));
                    if (s.Count - i >= 6) s.CurveTo(s.Arg(i), s.Arg(i + 1), s.Arg(i + 2), s.Arg(i + 3), s.Arg(i + 4), s.Arg(i + 5));
                    s.Count = 0;
                    break;
                }
                case 26: { // vvcurveto
                    var i = 0;
                    var dx1 = 0.0;
                    if ((s.Count & 1) == 1) dx1 = s.Arg(i++);
                    for (; i + 3 < s.Count; i += 4) {
                        s.CurveTo(dx1, s.Arg(i), s.Arg(i + 1), s.Arg(i + 2), 0, s.Arg(i + 3));
                        dx1 = 0;
                    }

                    s.Count = 0;
                    break;
                }
                case 27: { // hhcurveto
                    var i = 0;
                    var dy1 = 0.0;
                    if ((s.Count & 1) == 1) dy1 = s.Arg(i++);
                    for (; i + 3 < s.Count; i += 4) {
                        s.CurveTo(s.Arg(i), dy1, s.Arg(i + 1), s.Arg(i + 2), s.Arg(i + 3), 0);
                        dy1 = 0;
                    }

                    s.Count = 0;
                    break;
                }
                case 30: // vhcurveto
                case 31: { // hvcurveto
                    var horizontal = b0 == 31;
                    for (var i = 0; i + 3 < s.Count; i += 4) {
                        var last = s.Count - i == 5 ? s.Arg(i + 4) : 0;
                        if (horizontal) s.CurveTo(s.Arg(i), 0, s.Arg(i + 1), s.Arg(i + 2), last, s.Arg(i + 3));
                        else s.CurveTo(0, s.Arg(i), s.Arg(i + 1), s.Arg(i + 2), s.Arg(i + 3), last);
                        horizontal = !horizontal;
                    }

                    s.Count = 0;
                    break;
                }
                case 10: // callsubr
                case 29: { // callgsubr
                    var subrs = b0 == 10 ? _localSubrs[fd] : _globalSubrs;
                    var index = (int)s.Pop() + Bias(subrs.Count);
                    var (subrStart, subrEnd) = subrs.Item(index);
                    if (Execute(subrStart, subrEnd, fd, s, depth + 1, seacDepth)) return true;
                    break;
                }
                case 11: // return
                    return false;
                case 14: // endchar
                    if (_cff2) break;
                    if (s.Count >= 4) {
                        // seac: an accented glyph composed from two Standard Encoding glyphs.
                        var count = s.Count;
                        var adx = s.Arg(count - 4);
                        var ady = s.Arg(count - 3);
                        var baseGlyph = GlyphForStandardCode((int)s.Arg(count - 2));
                        var accentGlyph = GlyphForStandardCode((int)s.Arg(count - 1));
                        if (seacDepth == 0 && baseGlyph >= 0 && accentGlyph >= 0) {
                            s.ClosePath();
                            Run(baseGlyph, fd, s, 0, 0, seacDepth + 1);
                            Run(accentGlyph, fd, s, adx, ady, seacDepth + 1);
                        }
                    }

                    s.ClosePath();
                    s.Count = 0;
                    return true;
                case 15 when _cff2: // vsindex
                    s.VariationIndex = (int)s.Pop();
                    break;
                case 16 when _cff2: { // blend: keep each value's default and drop its region deltas.
                    var values = (int)s.Pop();
                    var regions = s.VariationIndex >= 0 && s.VariationIndex < _regionCounts.Length ? _regionCounts[s.VariationIndex] : 0;
                    var deltas = values * regions;
                    if (values < 0 || deltas > s.Count - values) throw new InvalidDataException("CFF2 blend is malformed.");
                    s.Count -= deltas;
                    break;
                }
                case 12:
                    ExecuteEscape(Byte(p++), s);
                    break;
                default:
                    // Reserved operators clear the stack, as a hint or path operator would.
                    s.Count = 0;
                    break;
            }
        }

        return false;
    }

    private void ExecuteEscape(int op, CharstringState s) {
        switch (op) {
            case 34: // hflex
                s.CurveTo(s.Arg(0), 0, s.Arg(1), s.Arg(2), s.Arg(3), 0);
                s.CurveTo(s.Arg(4), 0, s.Arg(5), -s.Arg(2), s.Arg(6), 0);
                s.Count = 0;
                return;
            case 35: // flex
                s.CurveTo(s.Arg(0), s.Arg(1), s.Arg(2), s.Arg(3), s.Arg(4), s.Arg(5));
                s.CurveTo(s.Arg(6), s.Arg(7), s.Arg(8), s.Arg(9), s.Arg(10), s.Arg(11));
                s.Count = 0;
                return;
            case 36: // hflex1
                s.CurveTo(s.Arg(0), s.Arg(1), s.Arg(2), s.Arg(3), s.Arg(4), 0);
                s.CurveTo(s.Arg(5), 0, s.Arg(6), s.Arg(7), s.Arg(8), -(s.Arg(1) + s.Arg(3) + s.Arg(7)));
                s.Count = 0;
                return;
            case 37: { // flex1
                var dx = s.Arg(0) + s.Arg(2) + s.Arg(4) + s.Arg(6) + s.Arg(8);
                var dy = s.Arg(1) + s.Arg(3) + s.Arg(5) + s.Arg(7) + s.Arg(9);
                var horizontal = Math.Abs(dx) > Math.Abs(dy);
                s.CurveTo(s.Arg(0), s.Arg(1), s.Arg(2), s.Arg(3), s.Arg(4), s.Arg(5));
                s.CurveTo(s.Arg(6), s.Arg(7), s.Arg(8), s.Arg(9), horizontal ? s.Arg(10) : -dx, horizontal ? -dy : s.Arg(10));
                s.Count = 0;
                return;
            }
            case 3: { var b = s.Pop(); var a = s.Pop(); s.Push(a != 0 && b != 0 ? 1 : 0); return; } // and
            case 4: { var b = s.Pop(); var a = s.Pop(); s.Push(a != 0 || b != 0 ? 1 : 0); return; } // or
            case 5: s.Push(s.Pop() == 0 ? 1 : 0); return; // not
            case 9: s.Push(Math.Abs(s.Pop())); return; // abs
            case 10: { var b = s.Pop(); var a = s.Pop(); s.Push(a + b); return; } // add
            case 11: { var b = s.Pop(); var a = s.Pop(); s.Push(a - b); return; } // sub
            case 12: { var b = s.Pop(); var a = s.Pop(); s.Push(b == 0 ? 0 : a / b); return; } // div
            case 14: s.Push(-s.Pop()); return; // neg
            case 15: { var b = s.Pop(); var a = s.Pop(); s.Push(a == b ? 1 : 0); return; } // eq
            case 18: s.Pop(); return; // drop
            case 20: { var index = (int)s.Pop(); var value = s.Pop(); if (index >= 0 && index < s.Transient.Length) s.Transient[index] = value; return; } // put
            case 21: { var index = (int)s.Pop(); s.Push(index >= 0 && index < s.Transient.Length ? s.Transient[index] : 0); return; } // get
            case 22: { var v2 = s.Pop(); var v1 = s.Pop(); var s2 = s.Pop(); var s1 = s.Pop(); s.Push(v1 <= v2 ? s1 : s2); return; } // ifelse
            case 23: s.Push(0.5); return; // random: deterministic output is preferred over noise
            case 24: { var b = s.Pop(); var a = s.Pop(); s.Push(a * b); return; } // mul
            case 26: s.Push(Math.Sqrt(Math.Max(0, s.Pop()))); return; // sqrt
            case 27: { var a = s.Pop(); s.Push(a); s.Push(a); return; } // dup
            case 28: { var b = s.Pop(); var a = s.Pop(); s.Push(b); s.Push(a); return; } // exch
            case 29: { // index
                var i = (int)s.Pop();
                if (i < 0) i = 0;
                s.Push(i < s.Count ? s.Arg(s.Count - 1 - i) : 0);
                return;
            }
            case 30: { // roll
                var shift = (int)s.Pop();
                var n = (int)s.Pop();
                if (n <= 0 || n > s.Count) return;
                var values = new double[n];
                for (var i = 0; i < n; i++) values[i] = s.Arg(s.Count - n + i);
                shift = ((shift % n) + n) % n;
                for (var i = 0; i < n; i++) s.Set(s.Count - n + (i + shift) % n, values[i]);
                return;
            }
            default:
                // dotsection and reserved escapes take no part in the outline.
                s.Count = 0;
                return;
        }
    }

    private static void CountStems(CharstringState s) {
        // An odd operand count means the width comes first (CFF only); halving ignores it either way.
        s.HaveWidth = true;
        s.StemCount += s.Count / 2;
        s.Count = 0;
    }

    private static void TakeWidth(CharstringState s, int arguments) {
        s.HaveWidth = true;
        if (s.Count > arguments) {
            // The first operand of the first stack-clearing operator may be the advance width.
            var extra = s.Count - arguments;
            for (var i = 0; i < arguments; i++) s.Set(i, s.Arg(extra + i));
            s.Count = arguments;
        }
    }

    private static int Bias(int count) => count < 1240 ? 107 : count < 33900 ? 1131 : 32768;

    private sealed class CharstringState {
        private readonly IGlyphOutlineSink _sink;
        private readonly double _scale;
        private readonly double[] _stack = new double[MaximumStack];
        private bool _open;

        public CharstringState(IGlyphOutlineSink sink, double scale) {
            _sink = sink;
            _scale = scale;
        }

        public double X;
        public double Y;
        public int Count;
        public int StemCount;
        public bool HaveWidth;
        public int Operations;
        public int VariationIndex;
        public readonly double[] Transient = new double[32];

        public void Push(double value) {
            if (Count >= _stack.Length) throw new InvalidDataException("CFF charstring stack overflow.");
            _stack[Count++] = value;
        }

        public double Pop() {
            if (Count == 0) throw new InvalidDataException("CFF charstring stack underflow.");
            return _stack[--Count];
        }

        public double Arg(int index) {
            if (index < 0 || index >= Count) throw new InvalidDataException("CFF charstring operand is missing.");
            return _stack[index];
        }

        public void Set(int index, double value) => _stack[index] = value;

        public void MoveTo(double dx, double dy) {
            ClosePath();
            X += dx;
            Y += dy;
            _sink.MoveTo(X * _scale, Y * _scale);
            _open = true;
        }

        public void LineTo(double dx, double dy) {
            if (!_open) MoveTo(0, 0);
            X += dx;
            Y += dy;
            _sink.LineTo(X * _scale, Y * _scale);
        }

        public void CurveTo(double dx1, double dy1, double dx2, double dy2, double dx3, double dy3) {
            if (!_open) MoveTo(0, 0);
            var x1 = X + dx1;
            var y1 = Y + dy1;
            var x2 = x1 + dx2;
            var y2 = y1 + dy2;
            X = x2 + dx3;
            Y = y2 + dy3;
            _sink.CubicTo(x1 * _scale, y1 * _scale, x2 * _scale, y2 * _scale, X * _scale, Y * _scale);
        }

        public void ClosePath() {
            if (!_open) return;
            _sink.Close();
            _open = false;
        }
    }
}
