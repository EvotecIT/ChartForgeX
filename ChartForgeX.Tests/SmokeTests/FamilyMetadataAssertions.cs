using System.Linq;
using System.Xml.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static XElement FamilyMetadata(string svg, string role, params (string Key, string Value)[] values) {
        var matches = XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role
            && values.All(value => (string?)element.Attribute("data-cfx-" + value.Key) == value.Value)).ToArray();
        Assert(matches.Length == 1, "Expected one " + role + " retaining " + string.Join(", ", values.Select(value => value.Key + "=" + value.Value)) + ".");
        return matches[0];
    }
}
