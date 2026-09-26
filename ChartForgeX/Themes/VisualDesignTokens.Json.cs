using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

public sealed partial class VisualDesignTokens {
    // A token file holds about 120 values nested four levels deep: the caps bound the value count, array and object sizes,
    // and nesting depth (32), and duplicate keys are rejected.
    private static readonly GeoJsonReadLimits TokenJsonLimits = new GeoJsonReadLimits(4096, 256, 256).LimitDepth(32).RejectDuplicates();

    /// <summary>
    /// Loads tokens from the generated design-token JSON (the HtmlForgeX design tokens 1.x; 1.1.0 adds the optional <c>ramps</c>). The document holds a
    /// <c>light</c> and a <c>dark</c> object, each with <c>surface</c>, <c>text</c>, <c>chrome</c>, <c>accent</c>,
    /// <c>severity</c> (fill and ink), <c>outcome</c>, <c>state</c>, and <c>series</c>, plus optional <c>ramps</c>
    /// (<c>sequential</c>: colours weakest to strongest; <c>diverging</c>: <c>negative</c> and <c>positive</c> arms weakest to
    /// strongest around a <c>neutral</c> colour). Files without ramps still load. Other top-level members, such as
    /// <c>name</c> or <c>notes</c>, are ignored.
    /// </summary>
    /// <remarks>
    /// Mapping: <c>surface.page</c> is the page background, <c>surface.card</c> the card, <c>surface.cardAlt</c> the plot
    /// surface, <c>surface.line</c> borders and grid, <c>text.primary</c> and <c>text.secondary</c> the foregrounds,
    /// <c>accent.base</c> and <c>chrome.accent</c> the accents, and <c>series</c> the categorical palette in its fixed order.
    /// Severity, outcome, and state colours populate <see cref="Status"/>; the theme's positive, warning, and negative
    /// colours come from <c>outcome.pass</c>, <c>severity.medium</c>, and <c>severity.critical</c>. <c>text.muted</c>,
    /// <c>surface.lineStrong</c>, <c>accent.soft</c>, and the chrome bar colours other than <c>chrome.accent</c> have no chart
    /// slot and are not read; the secondary accent is decorative only.
    /// </remarks>
    /// <param name="json">The token document.</param>
    /// <param name="mode">Which variant to load.</param>
    /// <returns>The loaded tokens.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">The JSON is malformed, too large, has duplicate keys, lacks a required member,
    /// or holds a colour that is not <c>#rrggbb</c> or <c>#rrggbbaa</c>. Keys are case-sensitive.</exception>
    public static VisualDesignTokens FromJson(string json, VisualThemeMode mode = VisualThemeMode.Light) {
        if (json == null) throw new ArgumentNullException(nameof(json));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown theme mode.");
        var name = mode == VisualThemeMode.Dark ? "dark" : "light";
        var root = GeoJsonValue.Parse(json, StringComparer.Ordinal, TokenJsonLimits).AsObject("design tokens");
        var set = Member(root, name, name);
        var surface = Member(set, "surface", name + ".surface");
        var text = Member(set, "text", name + ".text");
        var chrome = Member(set, "chrome", name + ".chrome");
        var accent = Member(set, "accent", name + ".accent");
        var severity = Member(set, "severity", name + ".severity");
        var outcome = Member(set, "outcome", name + ".outcome");
        var state = Member(set, "state", name + ".state");
        var status = new VisualStatusTokens {
            Critical = Pair(severity, "critical", name + ".severity"),
            High = Pair(severity, "high", name + ".severity"),
            Medium = Pair(severity, "medium", name + ".severity"),
            Low = Pair(severity, "low", name + ".severity"),
            Info = Pair(severity, "info", name + ".severity"),
            Pass = Pair(outcome, "pass", name + ".outcome"),
            Neutral = Pair(outcome, "neutral", name + ".outcome"),
            Maintenance = Pair(state, "maintenance", name + ".state")
        };
        return new VisualDesignTokens {
            Background = Color(surface, "page", name + ".surface"),
            Surface = Color(surface, "cardAlt", name + ".surface"),
            ElevatedSurface = Color(surface, "card", name + ".surface"),
            Border = Color(surface, "line", name + ".surface"),
            Foreground = Color(text, "primary", name + ".text"),
            MutedForeground = Color(text, "secondary", name + ".text"),
            Accent = Color(accent, "base", name + ".accent"),
            SecondaryAccent = Color(chrome, "accent", name + ".chrome"),
            Positive = status.Pass.Fill,
            Warning = status.Medium.Fill,
            Negative = status.Critical.Fill,
            Disabled = status.Neutral.Fill,
            Palette = Series(set, name + ".series"),
            Status = status,
            SequentialRamp = SequentialRampOrNull(set, name),
            DivergingRamp = DivergingRampOrNull(set, name)
        };
    }

    /// <summary>Loads tokens from a generated design-token JSON file.</summary>
    /// <param name="path">The token file path.</param>
    /// <param name="mode">Which variant to load.</param>
    /// <returns>The loaded tokens.</returns>
    /// <exception cref="ArgumentException">The path is empty or the content is not a valid token document.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static VisualDesignTokens FromJsonFile(string path, VisualThemeMode mode = VisualThemeMode.Light) {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Token file path must not be empty.", nameof(path));
        return FromJson(File.ReadAllText(path), mode);
    }

    private static ChartColor[]? SequentialRampOrNull(Dictionary<string, GeoJsonValue> set, string mode) {
        var ramps = OptionalMember(set, "ramps", mode + ".ramps");
        if (ramps == null || !ramps.TryGetValue("sequential", out var value) || value.IsNull) return null;
        var colors = ColorArray(value, mode + ".ramps.sequential");
        if (colors.Length < 2) throw new ArgumentException("Design tokens require at least two '" + mode + ".ramps.sequential' colours.");
        return colors;
    }

    private static VisualDivergingRamp? DivergingRampOrNull(Dictionary<string, GeoJsonValue> set, string mode) {
        var ramps = OptionalMember(set, "ramps", mode + ".ramps");
        var diverging = ramps == null ? null : OptionalMember(ramps, "diverging", mode + ".ramps.diverging");
        if (diverging == null) return null;
        var path = mode + ".ramps.diverging";
        if (!diverging.TryGetValue("negative", out var negative) || negative.IsNull) throw new ArgumentException("Design tokens require '" + path + ".negative'.");
        if (!diverging.TryGetValue("positive", out var positive) || positive.IsNull) throw new ArgumentException("Design tokens require '" + path + ".positive'.");
        var negativeColors = ColorArray(negative, path + ".negative");
        var positiveColors = ColorArray(positive, path + ".positive");
        if (negativeColors.Length == 0 || positiveColors.Length == 0) throw new ArgumentException("Design tokens require at least one colour in each '" + path + "' arm.");
        return new VisualDivergingRamp(negativeColors, Color(diverging, "neutral", path), positiveColors);
    }

    private static Dictionary<string, GeoJsonValue>? OptionalMember(Dictionary<string, GeoJsonValue> parent, string name, string path) =>
        parent.TryGetValue(name, out var value) && !value.IsNull ? value.AsObject(path) : null;

    private static ChartColor[] ColorArray(GeoJsonValue value, string path) {
        var items = value.AsArray(path);
        var colors = new ChartColor[items.Count];
        for (var i = 0; i < items.Count; i++) {
            var itemPath = path + "[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
            colors[i] = Hex(items[i].AsString(itemPath), itemPath);
        }

        return colors;
    }

    private static Dictionary<string, GeoJsonValue> Member(Dictionary<string, GeoJsonValue> parent, string name, string path) {
        if (!parent.TryGetValue(name, out var value) || value.IsNull) throw new ArgumentException("Design tokens require '" + path + "'.");
        return value.AsObject(path);
    }

    private static VisualTokenColor Pair(Dictionary<string, GeoJsonValue> parent, string name, string path) {
        var pair = Member(parent, name, path + "." + name);
        return new VisualTokenColor(Color(pair, "fill", path + "." + name), Color(pair, "ink", path + "." + name));
    }

    private static ChartColor Color(Dictionary<string, GeoJsonValue> parent, string name, string path) {
        if (!parent.TryGetValue(name, out var value) || value.IsNull) throw new ArgumentException("Design tokens require '" + path + "." + name + "'.");
        return Hex(value.AsString(path + "." + name), path + "." + name);
    }

    private static ChartColor[] Series(Dictionary<string, GeoJsonValue> set, string path) {
        if (!set.TryGetValue("series", out var value) || value.IsNull) throw new ArgumentException("Design tokens require '" + path + "'.");
        var items = value.AsArray(path);
        if (items.Count == 0) throw new ArgumentException("Design tokens require at least one '" + path + "' colour.");
        var colors = new ChartColor[items.Count];
        for (var i = 0; i < items.Count; i++) {
            var itemPath = path + "[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
            colors[i] = Hex(items[i].AsString(itemPath), itemPath);
        }

        return colors;
    }

    private static ChartColor Hex(string value, string path) {
        var text = value.Trim();
        if ((text.Length != 7 && text.Length != 9) || text[0] != '#') throw new ArgumentException("Design token '" + path + "' must be a #rrggbb or #rrggbbaa colour: " + value);
        try {
            return ChartColor.FromHex(text);
        } catch (Exception exception) when (exception is ArgumentException || exception is FormatException) {
            throw new ArgumentException("Design token '" + path + "' must be a #rrggbb or #rrggbbaa colour: " + value, exception);
        }
    }
}
