using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.VisualBlocks;

/// <summary>The semantic operation represented by a reusable grammar-diagram expression.</summary>
public enum RailroadExpressionKind {
    /// <summary>Consumes no input.</summary>
    Empty,
    /// <summary>Matches literal text.</summary>
    Terminal,
    /// <summary>References a rule without expanding it.</summary>
    Reference,
    /// <summary>Matches each child in order.</summary>
    Sequence,
    /// <summary>Matches an alternative; OrderedChoice preserves priority.</summary>
    Choice,
    /// <summary>Matches its child zero or one time.</summary>
    Optional,
    /// <summary>Matches a bounded or unbounded repetition without expanding its child.</summary>
    Repeat,
    /// <summary>Requires its child to match next without consuming input.</summary>
    Lookahead,
    /// <summary>Requires its child not to match next without consuming input.</summary>
    NegativeLookahead,
    /// <summary>Matches the first child except the language of the second child.</summary>
    Except,
    /// <summary>Describes a special grammar condition.</summary>
    Special,
    /// <summary>Matches any single character.</summary>
    AnyCharacter,
    /// <summary>Matches one code point from an inclusive range.</summary>
    CharacterRange
}

/// <summary>An immutable grammar expression shared by grammar frontends and the native railroad renderer.</summary>
public sealed class RailroadExpression {
    private RailroadExpression(RailroadExpressionKind kind, string value = "", IEnumerable<RailroadExpression>? children = null,
        bool caseSensitive = true, bool ordered = false, int minimum = 0, int? maximum = null, int firstCodePoint = 0, int lastCodePoint = 0) {
        Kind = kind; Value = value; CaseSensitive = caseSensitive; OrderedChoice = ordered;
        MinimumOccurrences = minimum; MaximumOccurrences = maximum; FirstCodePoint = firstCodePoint; LastCodePoint = lastCodePoint;
        var copy = children == null ? new List<RailroadExpression>() : new List<RailroadExpression>(children);
        if (copy.Any(child => child == null)) throw new ArgumentException("Grammar children must not be null.", nameof(children));
        Children = copy.AsReadOnly(); Depth = copy.Count == 0 ? 1 : checked(copy.Max(child => child.Depth) + 1);
    }
    /// <summary>Gets the grammar operation.</summary>
    public RailroadExpressionKind Kind { get; }
    /// <summary>Gets literal text, reference identity or special-condition text, according to Kind.</summary>
    public string Value { get; }
    /// <summary>Gets children in semantic order.</summary>
    public IReadOnlyList<RailroadExpression> Children { get; }
    /// <summary>Gets whether a literal comparison is case-sensitive.</summary>
    public bool CaseSensitive { get; }
    /// <summary>Gets whether alternatives are tried in their authored order.</summary>
    public bool OrderedChoice { get; }
    /// <summary>Gets the minimum repeat count.</summary>
    public int MinimumOccurrences { get; }
    /// <summary>Gets the maximum repeat count; null means unbounded.</summary>
    public int? MaximumOccurrences { get; }
    /// <summary>Gets the inclusive start of a character range.</summary>
    public int FirstCodePoint { get; }
    /// <summary>Gets the inclusive end of a character range.</summary>
    public int LastCodePoint { get; }
    /// <summary>Gets expression nesting depth without expanding references or repeat counts.</summary>
    public int Depth { get; }
    /// <summary>Creates an empty path.</summary>
    public static RailroadExpression Empty() => new(RailroadExpressionKind.Empty);
    /// <summary>Creates a literal, including an empty literal.</summary>
    public static RailroadExpression Terminal(string text, bool caseSensitive = true) => new(RailroadExpressionKind.Terminal, text ?? throw new ArgumentNullException(nameof(text)), caseSensitive: caseSensitive);
    /// <summary>Creates a symbolic rule reference. Recursive references are not expanded.</summary>
    public static RailroadExpression Reference(string ruleId) => new(RailroadExpressionKind.Reference, Required(ruleId, nameof(ruleId)));
    /// <summary>Creates an ordered concatenation; an empty collection consumes no input.</summary>
    public static RailroadExpression Sequence(IEnumerable<RailroadExpression> elements) => new(RailroadExpressionKind.Sequence, children: elements ?? throw new ArgumentNullException(nameof(elements)));
    /// <summary>Creates an ordered concatenation.</summary>
    public static RailroadExpression Sequence(params RailroadExpression[] elements) => Sequence((IEnumerable<RailroadExpression>)elements);
    /// <summary>Creates alternatives, optionally preserving ordered-choice priority.</summary>
    public static RailroadExpression Choice(IEnumerable<RailroadExpression> alternatives, bool ordered = false) {
        var copy = new List<RailroadExpression>(alternatives ?? throw new ArgumentNullException(nameof(alternatives)));
        if (copy.Count == 0) throw new ArgumentException("A choice requires at least one alternative.", nameof(alternatives));
        return new RailroadExpression(RailroadExpressionKind.Choice, children: copy, ordered: ordered);
    }
    /// <summary>Creates unordered alternatives.</summary>
    public static RailroadExpression Choice(params RailroadExpression[] alternatives) => Choice((IEnumerable<RailroadExpression>)alternatives);
    /// <summary>Creates an optional path.</summary>
    public static RailroadExpression Optional(RailroadExpression expression) => new(RailroadExpressionKind.Optional, children: One(expression), minimum: 0, maximum: 1);
    /// <summary>Creates repetition with exact inclusive bounds; null maximum means unbounded.</summary>
    public static RailroadExpression Repeat(RailroadExpression expression, int minimum, int? maximum = null) {
        if (minimum < 0 || maximum.HasValue && maximum.Value < minimum) throw new ArgumentOutOfRangeException(nameof(minimum), "Repeat bounds must be non-negative and ordered.");
        return new RailroadExpression(RailroadExpressionKind.Repeat, children: One(expression), minimum: minimum, maximum: maximum);
    }
    /// <summary>Creates a zero-width positive or negative predicate.</summary>
    public static RailroadExpression Lookahead(RailroadExpression expression, bool positive = true) => new(positive ? RailroadExpressionKind.Lookahead : RailroadExpressionKind.NegativeLookahead, children: One(expression));
    /// <summary>Creates a language exclusion, distinct from a consuming choice.</summary>
    public static RailroadExpression Except(RailroadExpression expression, RailroadExpression excluded) => new(RailroadExpressionKind.Except, children: new[] { expression ?? throw new ArgumentNullException(nameof(expression)), excluded ?? throw new ArgumentNullException(nameof(excluded)) });
    /// <summary>Creates an explicitly described special condition.</summary>
    public static RailroadExpression Special(string description) => new(RailroadExpressionKind.Special, Required(description, nameof(description)));
    /// <summary>Creates a match for any one character.</summary>
    public static RailroadExpression AnyCharacter() => new(RailroadExpressionKind.AnyCharacter);
    /// <summary>Creates an inclusive Unicode code-point range.</summary>
    public static RailroadExpression CharacterRange(int first, int last) {
        if (first < 0 || last < first || last > 0x10ffff) throw new ArgumentOutOfRangeException(nameof(first), "Character ranges must be ordered code points from U+0000 through U+10FFFF.");
        return new RailroadExpression(RailroadExpressionKind.CharacterRange, firstCodePoint: first, lastCodePoint: last);
    }
    private static RailroadExpression[] One(RailroadExpression expression) => new[] { expression ?? throw new ArgumentNullException(nameof(expression)) };
    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A non-empty grammar value is required.", name) : value;
}
