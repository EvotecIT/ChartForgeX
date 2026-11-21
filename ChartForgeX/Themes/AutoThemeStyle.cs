using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Themes;

/// <summary>
/// Lets HTML output of a <see cref="ThemeMode.Auto"/> model follow the viewer's <c>prefers-color-scheme</c>. The SVG is
/// written with <see cref="Variables"/> (the light colours as <c>--cfx-auto-*</c> properties), and <see cref="StyleSheet"/>
/// defines those properties on the element whose <see cref="ScopeAttribute"/> is <see cref="Id"/>: light colours by default and dark colours inside
/// <c>@media (prefers-color-scheme: dark)</c>. Each property reads the matching shared <c>--hfx-*</c> role first, so a
/// CanopyX or HtmlForgeX page keeps the colours it defines. Renderers add their own mode-dependent chrome with
/// <see cref="WithRule"/>, from the same code that writes it for an explicit mode.
/// </summary>
/// <remarks>The scope is an attribute rather than a class, so the root keeps its documented class. Its value is a hash of the
/// complete stylesheet, so fragments with different themes or chrome on one page never share it, and identical fragments share one rule set.</remarks>
internal sealed class AutoThemeStyle {
    internal const string Attribute = "data-cfx-theme-mode";
    internal const string AttributeValue = "auto";
    /// <summary>The attribute the scoped stylesheet selects; the root of Auto output carries it with <see cref="Id"/>.</summary>
    internal const string ScopeAttribute = "data-cfx-auto-theme";
    /// <summary>The page background a self-contained page reads, defined per mode on the scoped body.</summary>
    internal const string PageBackground = "--cfx-auto-page-background";
    private const string AutoPrefix = "--cfx-auto-";
    private const string SharedPrefix = "--hfx-";
    private const string Placeholder = "\u0001";
    private readonly List<(string Selector, string Light, string Dark)> _rules = new();
    private string _lightVariables;
    private string _darkVariables;
    private (string Id, string StyleSheet)? _built;

    private AutoThemeStyle(VisualTheme theme) {
        Variables = theme.Resolve(ThemeMode.Light).ToTokens().ToSvgColorVariables(AutoName);
        var dark = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variable in theme.Resolve(ThemeMode.Dark).ToTokens().ToSvgColorVariables(AutoName).Variables)
            if (!dark.ContainsKey(variable.Name)) dark.Add(variable.Name, variable.Color.ToCss());
        var light = new StringBuilder();
        var darkText = new StringBuilder();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variable in Variables.Variables) {
            if (!seen.Add(variable.Name)) continue;
            var shared = SharedPrefix + variable.Name.Substring(AutoPrefix.Length);
            var lightColor = variable.Color.ToCss();
            light.Append(variable.Name).Append(":var(").Append(shared).Append(',').Append(lightColor).Append(");");
            darkText.Append(variable.Name).Append(":var(").Append(shared).Append(',')
                .Append(dark.TryGetValue(variable.Name, out var darkColor) ? darkColor : lightColor).Append(");");
        }
        // Token-aware renderers (topology) paint by theme role, so each role is bound to its shared token property too.
        var lightColors = theme.Resolve(ThemeMode.Light); var darkColors = theme.Resolve(ThemeMode.Dark);
        foreach (var (token, path, color) in TokenPaths) {
            var name = AutoName(path)!;
            Variables.AddThemeToken(token, name);
            if (!seen.Add(name)) continue;
            var shared = SharedPrefix + name.Substring(AutoPrefix.Length);
            light.Append(name).Append(":var(").Append(shared).Append(',').Append(color(lightColors).ToCss()).Append(");");
            darkText.Append(name).Append(":var(").Append(shared).Append(',').Append(color(darkColors).ToCss()).Append(");");
        }
        _lightVariables = light.ToString();
        _darkVariables = darkText.ToString();
    }

    /// <summary>Gets the light colours as <c>--cfx-auto-*</c> properties, for rendering the SVG.</summary>
    internal SvgColorVariables Variables { get; }

    /// <summary>Gets the value of <see cref="ScopeAttribute"/> on the root element of the output.</summary>
    internal string Id => (_built ??= Build()).Id;

    /// <summary>Gets the scoped stylesheet: a plain CSS string, emitted in a <c>&lt;style&gt;</c> element.</summary>
    internal string StyleSheet => (_built ??= Build()).StyleSheet;

    /// <summary>Gets the <c>&lt;style&gt;</c> element carrying <see cref="StyleSheet"/>.</summary>
    internal string StyleElement => "<style data-cfx-auto-theme-style=\"" + Id + "\">" + StyleSheet + "</style>";

    /// <summary>Gets the root attributes for renderers that write markup as text, with a leading space.</summary>
    internal string RootAttributes => " " + Attribute + "=\"" + AttributeValue + "\" " + ScopeAttribute + "=\"" + Id + "\"";

    /// <summary>
    /// Returns the style for <paramref name="theme"/> when HTML output of a model in <paramref name="mode"/> should follow the
    /// viewer, which is only for <see cref="ThemeMode.Auto"/> without host colour variables: host colours always win.
    /// </summary>
    internal static AutoThemeStyle? ForHtml(VisualTheme theme, ThemeMode mode, SvgColorVariables? hostVariables) =>
        mode == ThemeMode.Auto && hostVariables == null ? new AutoThemeStyle(theme ?? throw new ArgumentNullException(nameof(theme))) : null;

    /// <summary>
    /// Adds declarations a renderer writes for each mode, such as page backgrounds or chrome custom properties. The renderer
    /// must not also write them inline, because inline declarations win over the stylesheet.
    /// </summary>
    /// <param name="selector">A selector relative to the scoped element: <c>&amp;</c> stands for it, for example <c>&amp;</c> or <c>&amp; .cfx-shell</c>.</param>
    /// <param name="declarations">Returns the declarations for <see cref="ThemeMode.Light"/> or <see cref="ThemeMode.Dark"/>.</param>
    /// <returns>The same instance.</returns>
    internal AutoThemeStyle WithRule(string selector, Func<ThemeMode, string> declarations) {
        if (selector == null || selector.IndexOf('&') < 0) throw new ArgumentException("The selector names the scoped element with '&'.", nameof(selector));
        _rules.Add((selector, declarations(ThemeMode.Light), declarations(ThemeMode.Dark)));
        _built = null;
        return this;
    }

    /// <summary>
    /// Adds a colour the renderer derives outside the theme tokens (for example story syntax colours), so value-mapped
    /// output (<see cref="SvgColorVariables.Apply"/>) follows the viewer for it too. Colours already mapped keep their variable.
    /// </summary>
    internal AutoThemeStyle WithColor(string name, ChartForgeX.Primitives.ChartColor light, ChartForgeX.Primitives.ChartColor dark) {
        var property = AutoPrefix + name;
        Variables.Add(property, light);
        _lightVariables += property + ":" + light.ToCss() + ";";
        _darkVariables += property + ":" + dark.ToCss() + ";";
        _built = null;
        return this;
    }

    /// <summary>Defines <see cref="PageBackground"/> per mode on the scoped element, for a page body that carries the scope.</summary>
    internal AutoThemeStyle WithPageBackground(Func<ThemeMode, string> background) =>
        WithRule("&", mode => PageBackground + ":" + background(mode));

    /// <summary>Writes a reference to a property this style defines, with the light value as the fallback.</summary>
    internal static string Var(string property, string lightFallback) => "var(" + property + "," + lightFallback + ")";

    // Token-aware paints (topology) pass the status fills as their fallbacks, so the tokens name those same fills.
    private static readonly (VisualThemeColorToken Token, string Path, Func<VisualThemeColors, ChartForgeX.Primitives.ChartColor> Color)[] TokenPaths = {
        (VisualThemeColorToken.Background, "surface.page", c => c.Background), (VisualThemeColorToken.Surface, "surface.cardAlt", c => c.Surface),
        (VisualThemeColorToken.ElevatedSurface, "surface.card", c => c.ElevatedSurface), (VisualThemeColorToken.Foreground, "text.primary", c => c.Foreground),
        (VisualThemeColorToken.MutedForeground, "text.secondary", c => c.MutedForeground), (VisualThemeColorToken.Border, "surface.line", c => c.Border),
        (VisualThemeColorToken.Accent, "accent.base", c => c.Accent), (VisualThemeColorToken.Positive, "outcome.pass.fill", c => c.Status.Pass.Fill),
        (VisualThemeColorToken.Warning, "severity.medium.fill", c => c.Status.Medium.Fill), (VisualThemeColorToken.Negative, "severity.critical.fill", c => c.Status.Critical.Fill),
        (VisualThemeColorToken.Neutral, "outcome.neutral.fill", c => c.Status.Neutral.Fill), (VisualThemeColorToken.Maintenance, "state.maintenance.fill", c => c.Status.Maintenance.Fill),
        (VisualThemeColorToken.Info, "status.info", c => c.Info)
    };

    private static string? AutoName(string path) => AutoPrefix + SvgColorVariables.SharedRoleName(path).Substring(SharedPrefix.Length);

    private (string Id, string StyleSheet) Build() {
        var scope = "[" + ScopeAttribute + "=\"" + Placeholder + "\"]";
        var css = new StringBuilder();
        css.Append(scope).Append("{color-scheme:light dark;").Append(_lightVariables).Append('}');
        foreach (var rule in _rules) css.Append(rule.Selector.Replace("&", scope)).Append('{').Append(rule.Light).Append('}');
        css.Append("@media (prefers-color-scheme:dark){").Append(scope).Append('{').Append(_darkVariables).Append('}');
        foreach (var rule in _rules) css.Append(rule.Selector.Replace("&", scope)).Append('{').Append(rule.Dark).Append('}');
        css.Append('}');
        var text = css.ToString();
        var name = "cfx-auto-" + Hash(text);
        return (name, text.Replace(Placeholder, name));
    }

    private static string Hash(string text) {
        unchecked {
            var hash = 14695981039346656037UL;
            foreach (var ch in text) { hash ^= ch; hash *= 1099511628211UL; }
            return hash.ToString("x16", CultureInfo.InvariantCulture).Substring(0, 12);
        }
    }
}
