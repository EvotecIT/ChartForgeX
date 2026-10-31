using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ChartForgeX.Interactivity.Html;

// Compacts responsive paint while retaining the ordinary static SVG contract.
internal static class HtmlCompactSvg {
    internal static string Compact(string svg, int printWidth) {
        var root = XElement.Parse(svg, LoadOptions.PreserveWhitespace);
        ShortenNamespaces(root);
        root.SetAttributeValue("style", "display:block;width:100%;max-width:" + printWidth.ToString(CultureInfo.InvariantCulture) + "px;height:auto");
        var cells = root.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "heatmap-cell").ToArray();
        foreach (var cell in cells) {
            var link = cell.Elements().FirstOrDefault(e => e.Name.LocalName == "a" && (string?)e.Attribute("data-cfx-role") == "heatmap-cell-link");
            if (link == null || cell.Elements().Count() != 1) continue;
            // The link's title supplies both its accessible name and its native hover tooltip.
            foreach (var name in new[] { "id", "aria-label", "role", "data-cfx-source-id", "data-cfx-meta-state", "data-cfx-state-label", "data-cfx-series-key", "data-cfx-series-scope", "data-cfx-point", "data-cfx-source-point", "data-cfx-series", "data-cfx-value", "data-cfx-x", "data-cfx-y" }) cell.Attribute(name)?.Remove();
            link.SetAttributeValue("tabindex", null);
            link.SetAttributeValue("data-cfx-role", "heatmap-cell");
            foreach (var attribute in cell.Attributes().ToArray()) link.SetAttributeValue(attribute.Name, attribute.Value);
            foreach (var shape in link.Descendants().Where(e => e.Name.LocalName == "rect" && (string?)e.Attribute("data-cfx-role") == "heatmap-cell-shape" && (string?)e.Attribute("stroke") == "none"))
                shape.Attribute("stroke-width")?.Remove();
            cell.ReplaceWith(link);
        }
        // An image role makes its descendants presentational; linked marks must remain exposed as links.
        if (root.Descendants().Any(e => e.Name.LocalName == "a" && e.Attribute("href") != null) && (string?)root.Attribute("role") == "img")
            root.SetAttributeValue("role", "group");
        DeduplicatePatterns(root);
        RemoveUnneededRectangleClips(root);
        return root.ToString(SaveOptions.DisableFormatting);
    }
    // Keep 96 bits of generated identity; replace only references to IDs in this drawing, never user text.
    private static void ShortenNamespaces(XElement root) {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var attribute in root.DescendantsAndSelf().Attributes("id")) {
            var match = Regex.Match(attribute.Value, @"^cfx-(?:v2|scope)-([0-9a-f]{24})[0-9a-f]{40}(?=-)");
            if (!match.Success) continue;
            var hex = match.Groups[1].Value;
            var bytes = new byte[12];
            for (var i = 0; i < bytes.Length; i++) bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var compact = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');
            aliases[attribute.Value] = "cfx-" + compact + attribute.Value.Substring(match.Length);
        }
        RewriteReferences(root, aliases, true);
    }
    private static void RewriteReferences(XElement root, Dictionary<string, string> aliases, bool renameIds) {
        if (aliases.Count == 0) return;
        foreach (var attribute in root.DescendantsAndSelf().Attributes()) {
            var value = attribute.Value;
            if (renameIds && attribute.Name.LocalName == "id" && aliases.TryGetValue(value, out var id)) value = id;
            else if (attribute.Name.LocalName == "href" && value.StartsWith("#", StringComparison.Ordinal) && aliases.TryGetValue(value.Substring(1), out var target)) value = "#" + target;
            else if (attribute.Name.LocalName == "aria-labelledby" || attribute.Name.LocalName == "aria-describedby")
                value = string.Join(" ", value.Split(' ').Select(token => aliases.TryGetValue(token, out var mapped) ? mapped : token));
            else if ((attribute.Name.LocalName == "fill" || attribute.Name.LocalName == "stroke" || attribute.Name.LocalName == "clip-path" || attribute.Name.LocalName == "mask" || attribute.Name.LocalName == "filter" || attribute.Name.LocalName == "style") && value.IndexOf("url(#", StringComparison.Ordinal) >= 0)
                value = Regex.Replace(value, @"url\(#([^)]*)\)", match => aliases.TryGetValue(match.Groups[1].Value, out var mapped) ? "url(#" + mapped + ")" : match.Value);
            if (value != attribute.Value) attribute.Value = value;
        }
    }
    private static void DeduplicatePatterns(XElement root) {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pattern in root.Descendants().Where(e => e.Name.LocalName == "pattern").ToArray()) {
            var id = (string?)pattern.Attribute("id");
            if (id == null || pattern.Descendants().Any(e => e.Attribute("id") != null)) continue;
            var copy = new XElement(pattern); copy.Attribute("id")?.Remove();
            var key = copy.ToString(SaveOptions.DisableFormatting);
            if (!seen.TryGetValue(key, out var existing)) { seen.Add(key, id); continue; }
            aliases.Add(id, existing);
            var parent = pattern.Parent; pattern.Remove();
            if (parent != null && parent.Name.LocalName == "defs" && !parent.HasElements) parent.Remove();
        }
        RewriteReferences(root, aliases, false);
    }
    private static void RemoveUnneededRectangleClips(XElement root) {
        var clips = root.Descendants().Where(e => e.Name.LocalName == "clipPath" && e.Attribute("id") != null)
            .ToDictionary(e => (string)e.Attribute("id")!, e => e, StringComparer.Ordinal);
        foreach (var group in root.Descendants().Where(e => e.Name.LocalName == "g" && e.Attributes().Count() == 1 && e.Attribute("clip-path") != null).ToArray()) {
            var clipRef = (string)group.Attribute("clip-path")!;
            if (!clipRef.StartsWith("url(#", StringComparison.Ordinal) || !clipRef.EndsWith(")", StringComparison.Ordinal)) continue;
            if (!clips.TryGetValue(clipRef.Substring(5, clipRef.Length - 6), out var clip)) continue;
            if (clip.Elements().Count() != 1 || group.Elements().Count() != 1) continue;
            var bounds = clip.Elements().First(); var rect = group.Elements().First();
            if ((string?)clip.Attribute("clipPathUnits") != "userSpaceOnUse" || bounds.Name.LocalName != "rect" || rect.Name.LocalName != "rect" || (string?)rect.Attribute("stroke") != "none") continue;
            if (Contains(bounds, rect)) group.ReplaceWith(rect);
        }
    }
    private static bool Contains(XElement outer, XElement inner) {
        double Number(XElement e, string name) => double.TryParse((string?)e.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : double.NaN;
        var x = Number(outer, "x"); var y = Number(outer, "y"); var w = Number(outer, "width"); var h = Number(outer, "height");
        var ix = Number(inner, "x"); var iy = Number(inner, "y"); var iw = Number(inner, "width"); var ih = Number(inner, "height");
        return ix >= x + 1 && iy >= y + 1 && ix + iw <= x + w - 1 && iy + ih <= y + h - 1;
    }
}
