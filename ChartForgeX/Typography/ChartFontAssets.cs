using System;
using System.IO;
using System.Collections.Generic;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Typography;

/// <summary>The OFL-licensed product faces shared by standalone browser output and native rendering.</summary>
public static class ChartFontAssets {
    private static readonly Lazy<TrueTypeFont> Sans = new(() => Load("Archivo"), true);
    private static readonly Lazy<TrueTypeFont> Mono = new(() => Load("JetBrainsMono"), true);
    private static readonly Lazy<string> SansCss = new(() => Face("Archivo", "Archivo", 900), true);
    private static readonly Lazy<string> MonoCss = new(() => Face("JetBrains Mono", "JetBrainsMono", 800), true);
    private static readonly Lazy<string> ProductCss = new(() => SansCss.Value + MonoCss.Value, true);

    /// <summary>Self-contained normal variable faces for unregistered Archivo and JetBrains Mono families.</summary>
    /// <remarks>Explicitly registered families retain precedence. Supply their matching browser CSS in the host document.</remarks>
    public static string StyleSheet => ForFamily("Archivo, 'JetBrains Mono'");

    // Only complete immutable product payloads qualify; registry overrides cannot change their identity.
    internal static bool IsProductStyleSheet(string css) =>
        string.Equals(css, SansCss.Value, StringComparison.Ordinal)
        || string.Equals(css, MonoCss.Value, StringComparison.Ordinal)
        || string.Equals(css, ProductCss.Value, StringComparison.Ordinal);

    internal static bool Owns(TrueTypeFont font, string family) {
        foreach (var part in family.Split(',')) {
            var name = part.Trim().Trim('\'', '"');
            var source = name.Equals("Archivo", StringComparison.OrdinalIgnoreCase) ? Sans.Value
                : name.Equals("JetBrains Mono", StringComparison.OrdinalIgnoreCase) ? Mono.Value : null;
            if (source != null && (ReferenceEquals(font.Root, source.Root)
                || font.CapturedByteIdentity == source.CapturedByteIdentity && font.SameCapturedBytes(source))) return true;
        }
        return false;
    }

    internal static string ForFamily(string family) {
        var css = string.Empty;
        var registered = new HashSet<string>(FontRegistry.Families, StringComparer.OrdinalIgnoreCase);
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in family.Split(',')) {
            var name = part.Trim().Trim('\'', '"');
            if (registered.Contains(name) || !emitted.Add(name)) continue;
            if (name.Equals("Archivo", StringComparison.OrdinalIgnoreCase)) css += SansCss.Value;
            if (name.Equals("JetBrains Mono", StringComparison.OrdinalIgnoreCase)) css += MonoCss.Value;
        }
        return css;
    }

    // A prepared scene owns its actual glyph faces, independently of later registry changes.
    // A same-name non-product face used anywhere in that scene keeps that family host-owned.
    internal static string ForScene(VisualScene scene) {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var overridden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var observed = new HashSet<(TrueTypeFont Font, string Family)>();
        foreach (var node in scene.Nodes) {
            if (node is not VisualSceneText text) continue;
            foreach (var line in text.Text.Lines) foreach (var glyph in line.Glyphs) {
                var family = glyph.Face.SelectedFamily ?? text.Text.Style.Font.Family;
                if (!observed.Add((glyph.Face, family))) continue;
                foreach (var part in family.Split(',')) {
                    var name = part.Trim().Trim('\'', '"');
                    if (!name.Equals("Archivo", StringComparison.OrdinalIgnoreCase)
                        && !name.Equals("JetBrains Mono", StringComparison.OrdinalIgnoreCase)) continue;
                    if (Owns(glyph.Face, name)) used.Add(name); else overridden.Add(name);
                }
            }
        }
        return (used.Contains("Archivo") && !overridden.Contains("Archivo") ? SansCss.Value : string.Empty)
            + (used.Contains("JetBrains Mono") && !overridden.Contains("JetBrains Mono") ? MonoCss.Value : string.Empty);
    }

    internal static InstalledFontFace? Find(string family, int weight, bool italic) {
        TrueTypeFont font;
        if (family.Equals("Archivo", StringComparison.OrdinalIgnoreCase)) font = Sans.Value;
        else if (family.Equals("JetBrains Mono", StringComparison.OrdinalIgnoreCase)) font = Mono.Value;
        else return null;
        // Both shipped faces are upright. Keep the requested weight as a real variable instance;
        // an explicit italic request retains the renderer's established synthetic oblique policy.
        font = TypographyFontResolver.WithRequestedWeight(font, weight);
        return new InstalledFontFace(null, null, family, null, weight, 5, false, font);
    }

    private static TrueTypeFont Load(string name) => TrueTypeFont.TryLoad(Read(name + ".ttf"))
        ?? throw new InvalidOperationException("The bundled product font is unreadable: " + name);

    private static byte[] Read(string name) {
        using var stream = typeof(ChartFontAssets).Assembly.GetManifestResourceStream("ChartForgeX.Typography.Fonts." + name)
            ?? throw new InvalidOperationException("Missing bundled product font: " + name);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static string Face(string family, string file, int maximumWeight) =>
        // The licence keeps LF line ends however the checkout stores it, so the payload stays one immutable identity
        // after an SVG or HTML parser normalizes its text.
        "/* " + System.Text.Encoding.UTF8.GetString(Read("OFL-" + file + ".txt")).Replace("\r\n", "\n").Replace('\r', '\n') + " */"
        + "@font-face{font-family:'" + family + "';font-style:normal;font-weight:100 " + maximumWeight
        + ";font-display:swap;src:url(data:font/woff2;base64," + Convert.ToBase64String(Read(file + ".woff2")) + ") format('woff2')}";
}
