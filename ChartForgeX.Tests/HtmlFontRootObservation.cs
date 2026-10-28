using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Independent exact destination observation: validates bounded generated font records and removes only their byte text.</summary>
internal static class HtmlFontRootObservation {
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
    private static readonly Regex Roots = new("<template data-cfx-font-root=\"(?<id>[a-f0-9]{64})\" data-cfx-font-byte-length=\"(?<length>[0-9]+)\">(?<body>[A-Za-z0-9+/=]*)</template>", RegexOptions.CultureInvariant, Timeout);
    private static readonly Regex Faces = new("<template data-cfx-font-face=\"(?<alias>cfx-owned-[a-f0-9]{64})\" data-cfx-font-resource=\"(?<root>[a-f0-9]{64})\" data-cfx-font-weight=\"(?<weight>[0-9]+)\" data-cfx-font-style=\"(?<style>normal|italic)\"></template>", RegexOptions.CultureInvariant, Timeout);
    internal static void AssertDestination(string section, string destination) {
        var original = Read(section, empty: false); var observation = Read(destination, empty: true);
        // These assertions independently retain every original identity, length, alias and descriptor.
        Assert.Equal(original.RootFacts, observation.RootFacts); Assert.Equal(original.FaceFacts, observation.FaceFacts);
        var expected = new StringBuilder(section);
        for (var index = original.Roots.Length - 1; index >= 0; index--) {
            var body = original.Roots[index].Groups["body"]; expected.Remove(body.Index, body.Length);
        }
        Assert.Equal(expected.ToString(), destination);
    }
    private static (Match[] Roots, string[] RootFacts, string[] FaceFacts) Read(string html, bool empty) {
        Assert.InRange(Encoding.UTF8.GetByteCount(html), 1, 32 * 1024 * 1024);
        var roots = Roots.Matches(html).Cast<Match>().ToArray(); var faces = Faces.Matches(html).Cast<Match>().ToArray();
        Assert.Equal(Occurrences(html, "data-cfx-font-root="), roots.Length);
        Assert.Equal(Occurrences(html, "data-cfx-font-face="), faces.Length);
        Assert.InRange(roots.Length, 0, 32); Assert.InRange(faces.Length, 0, 64);
        var ids = new HashSet<string>(StringComparer.Ordinal); long total = 0;
        foreach (var root in roots) {
            var id = root.Groups["id"].Value; Assert.True(ids.Add(id));
            var length = int.Parse(root.Groups["length"].Value, CultureInfo.InvariantCulture);
            Assert.Equal(length.ToString(CultureInfo.InvariantCulture), root.Groups["length"].Value);
            Assert.InRange(length, 1, 2 * 1024 * 1024); total += length; Assert.InRange(total, 1, 4 * 1024 * 1024);
            var body = root.Groups["body"].Value;
            if (empty) Assert.Empty(body);
            else {
                Assert.InRange(body.Length, 4, (2 * 1024 * 1024 + 2) / 3 * 4);
                var bytes = Convert.FromBase64String(body); Assert.Equal(body, Convert.ToBase64String(bytes)); Assert.Equal(length, bytes.Length);
                using var hash = SHA256.Create(); Assert.Equal(id, Convert.ToHexString(hash.ComputeHash(bytes)).ToLowerInvariant());
            }
        }
        var aliases = new HashSet<string>(StringComparer.Ordinal); var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var face in faces) {
            Assert.True(aliases.Add(face.Groups["alias"].Value)); Assert.Contains(face.Groups["root"].Value, ids); referenced.Add(face.Groups["root"].Value);
            var weight = int.Parse(face.Groups["weight"].Value, CultureInfo.InvariantCulture); Assert.InRange(weight, 1, 1000);
            Assert.Equal(weight.ToString(CultureInfo.InvariantCulture), face.Groups["weight"].Value);
        }
        Assert.Equal(ids.OrderBy(id => id, StringComparer.Ordinal), referenced.OrderBy(id => id, StringComparer.Ordinal));
        return (roots, roots.Select(root => root.Value.Substring(0, root.Groups["body"].Index - root.Index)).ToArray(), faces.Select(face => face.Value).ToArray());
    }
    private static int Occurrences(string text, string token) {
        var count = 0;
        for (var start = 0; (start = text.IndexOf(token, start, StringComparison.Ordinal)) >= 0; start += token.Length) count++;
        return count;
    }
}
