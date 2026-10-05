using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

public sealed partial class VisualDesignTokens {
    /// <summary>
    /// Creates SVG colour variables for these tokens, one per token colour, named from the token path. Paths use the
    /// member names of the token JSON with 1-based positions: <c>surface.page</c>, <c>surface.card</c>,
    /// <c>surface.cardAlt</c>, <c>surface.line</c>, <c>text.primary</c>, <c>text.secondary</c>, <c>series.1</c> …,
    /// <c>severity.{critical|high|medium|low|info}.{fill|ink}</c>, <c>outcome.{pass|neutral}.{fill|ink}</c>,
    /// <c>state.maintenance.{fill|ink}</c>, independent legacy <c>status.{positive|warning|negative|disabled}</c>,
    /// <c>accent.base</c>, <c>chrome.accent</c>, <c>ramps.sequential.1</c> …,
    /// <c>ramps.diverging.negative.1</c> …, <c>ramps.diverging.neutral</c>, and <c>ramps.diverging.positive.1</c> ….
    /// They are added in that order, so when two tokens share a colour the earlier one names a paint without a role.
    /// Each variable has the role of its token (<see cref="SvgColorRole.Surface"/> for <c>surface.*</c>, <c>Text</c>,
    /// <c>Series</c>, <c>Status</c> for severity, outcome, and state, <c>Ramp</c>; accents <c>Any</c>), so a renderer
    /// that writes a colour for a role names the same token in every theme. Text matched by value never takes a surface
    /// token, so contrast text that happens to have a surface colour stays literal; text on filled heatmap cells and
    /// state marks is written for the surface or text role and takes those tokens.
    /// </summary>
    /// <param name="variableName">
    /// Returns the custom property name for a token path, or null to leave that token literal. Null names every token
    /// <c>--cfx-</c> followed by its path in kebab case, for example <c>--cfx-surface-card-alt</c> or <c>--cfx-series-1</c>.
    /// </param>
    /// <returns>The colour variables, for <c>Chart.WithSvgColorVariables</c>, <c>ChartGrid.WithSvgColorVariables</c>, or <c>TopologyRenderOptions.SvgColorVariables</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="variableName"/> returned a name that is not a valid custom property name.</exception>
    public SvgColorVariables ToSvgColorVariables(Func<string, string?>? variableName = null) {
        var name = variableName ?? DefaultSvgVariableName;
        var variables = new SvgColorVariables();
        void Add(string path, ChartColor color) {
            var variable = name(path);
            if (variable != null) variables.Add(variable, color, RoleOf(path));
        }

        void AddList(string path, ChartColor[] colors) {
            for (var i = 0; i < colors.Length; i++) Add(path + "." + (i + 1).ToString(CultureInfo.InvariantCulture), colors[i]);
        }

        void AddPair(string path, VisualTokenColor pair) {
            Add(path + ".fill", pair.Fill);
            Add(path + ".ink", pair.Ink);
        }

        Add("surface.page", Background);
        Add("surface.card", ElevatedSurface);
        Add("surface.cardAlt", Surface);
        Add("surface.line", Border);
        Add("text.primary", Foreground);
        Add("text.secondary", MutedForeground);
        if (Muted.HasValue) Add("text.muted", Muted.Value);
        if (Grid.HasValue) Add("guide.grid", Grid.Value);
        if (Axis.HasValue) Add("guide.axis", Axis.Value);
        AddList("series", _palette);
        AddPair("severity.critical", Status.Critical);
        AddPair("severity.high", Status.High);
        AddPair("severity.medium", Status.Medium);
        AddPair("severity.low", Status.Low);
        AddPair("severity.info", Status.Info);
        AddPair("outcome.pass", Status.Pass);
        AddPair("outcome.neutral", Status.Neutral);
        AddPair("state.maintenance", Status.Maintenance);
        // Legacy chart/topology status slots are independently customizable from the paired report tones.
        Add("status.positive", Positive);
        Add("status.warning", Warning);
        Add("status.negative", Negative);
        Add("status.disabled", Disabled);
        if (Info.HasValue) Add("status.info", Info.Value);
        if (Quiet.HasValue) Add("status.quiet", Quiet.Value);
        if (QuietLine.HasValue) Add("status.quietLine", QuietLine.Value);
        if (Neutral.HasValue) Add("status.neutral", Neutral.Value);
        if (Neutral2.HasValue) Add("surface.neutral2", Neutral2.Value);
        if (Neutral3.HasValue) Add("surface.neutral3", Neutral3.Value);
        Add("accent.base", Accent);
        Add("chrome.accent", SecondaryAccent);
        if (_sequentialRamp != null) AddList("ramps.sequential", _sequentialRamp);
        if (DivergingRamp != null) {
            AddList("ramps.diverging.negative", ToArray(DivergingRamp.Negative));
            Add("ramps.diverging.neutral", DivergingRamp.Neutral);
            AddList("ramps.diverging.positive", ToArray(DivergingRamp.Positive));
        }

        if (UseGraphiteLayout) {
            void Ink(string path, ChartColor color, SvgColorRole role) {
                var variable = name(path + ".ink");
                if (variable != null) variables.AddInk(variable, color, ChartColorMath.AccessibleTextOnBackground(color), role);
            }
            for (var i = 0; i < _palette.Length; i++) Ink("series." + (i + 1).ToString(CultureInfo.InvariantCulture), _palette[i], SvgColorRole.Series);
            if (_sequentialRamp != null)
                for (var i = 0; i < _sequentialRamp.Length; i++) Ink("ramps.sequential." + (i + 1).ToString(CultureInfo.InvariantCulture), _sequentialRamp[i], SvgColorRole.Ramp);
            if (Neutral3.HasValue) Ink("surface.neutral3", Neutral3.Value, SvgColorRole.Surface);
            Ink("mark.danger", Negative, SvgColorRole.Status);
            Ink("mark.warning", Warning, SvgColorRole.Status);
            Ink("mark.success", Positive, SvgColorRole.Status);
            if (Info.HasValue) Ink("mark.info", Info.Value, SvgColorRole.Status);
            if (Quiet.HasValue) Ink("mark.quiet", Quiet.Value, SvgColorRole.Status);
            if (Neutral.HasValue) Ink("mark.neutral", Neutral.Value, SvgColorRole.Status);
        }

        return variables;
    }

    /// <summary>Returns the role of a token path: surfaces, text, series, statuses, ramps; accents have no particular role.</summary>
    private static SvgColorRole RoleOf(string path) {
        if (path.StartsWith("surface.", StringComparison.Ordinal)) return SvgColorRole.Surface;
        if (path.StartsWith("text.", StringComparison.Ordinal)) return SvgColorRole.Text;
        if (path.StartsWith("series.", StringComparison.Ordinal)) return SvgColorRole.Series;
        if (path == "guide.grid") return SvgColorRole.Grid;
        if (path == "guide.axis") return SvgColorRole.Axis;
        if (path.StartsWith("ramps.", StringComparison.Ordinal)) return SvgColorRole.Ramp;
        if (path.StartsWith("severity.", StringComparison.Ordinal) || path.StartsWith("outcome.", StringComparison.Ordinal) || path.StartsWith("state.", StringComparison.Ordinal) || path.StartsWith("status.", StringComparison.Ordinal)) return SvgColorRole.Status;
        return SvgColorRole.Any;
    }

    private static string DefaultSvgVariableName(string path) {
        var builder = new StringBuilder("--cfx-", path.Length + 12);
        foreach (var ch in path) {
            if (ch == '.') builder.Append('-');
            else if (char.IsUpper(ch)) builder.Append('-').Append(char.ToLowerInvariant(ch));
            else builder.Append(ch);
        }

        return builder.ToString();
    }

    private static ChartColor[] ToArray(System.Collections.Generic.IReadOnlyList<ChartColor> colors) {
        var result = new ChartColor[colors.Count];
        for (var i = 0; i < result.Length; i++) result[i] = colors[i];
        return result;
    }
}
