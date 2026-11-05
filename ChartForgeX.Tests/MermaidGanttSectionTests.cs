using System.Xml.Linq;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttSectionTests {
    private const string Source = "gantt\ndateFormat YYYY-MM-DD\nsection Engine\nLayout pass :done, l1, 2026-09-01, 12d\n"
        + "Label placement :active, l2, after l1, 10d\nsection Surfaces\nPNG parity : s1, 2026-09-10, 14d\nEmail preview : s2, after s1, 7d\n";

    [Theory]
    [InlineData(960)] [InlineData(390)]
    public void EachSectionIsNamedOnceBesideItsBandAndRowsKeepTheirTitles(int width) {
        var parsed = new MermaidParser().ParseGantt(Source); Assert.False(parsed.HasErrors);
        var svg = XDocument.Parse(parsed.Document!.ToSvg(new MermaidGanttRenderOptions { Width = width }));
        string Text(XElement element) => string.Concat(element.Descendants().Where(child => child.Name.LocalName == "text").Select(child => child.Value));
        XElement[] Role(string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

        Assert.Equal(new[] { "Engine", "Surfaces" }, Role("gantt-section-label").Select(Text));
        // Wrapped titles paint one line per text element, so compare the words.
        Assert.Equal(new[] { "Layoutpass", "Labelplacement", "PNGparity", "Emailpreview" },
            Role("schedule-row-label").Select(label => Text(label).Replace(" ", string.Empty)));
        var bands = Role("gantt-section-band"); Assert.Equal(2, bands.Length);
        foreach (var label in Role("gantt-section-label").Zip(bands, (label, band) => (label, band))) {
            var text = label.label.Descendants().First(child => child.Name.LocalName == "text");
            var y = double.Parse((string)text.Attribute("y")!, System.Globalization.CultureInfo.InvariantCulture);
            var top = double.Parse((string)label.band.Attribute("y")!, System.Globalization.CultureInfo.InvariantCulture);
            var height = double.Parse((string)label.band.Attribute("height")!, System.Globalization.CultureInfo.InvariantCulture);
            Assert.InRange(y, top, top + height);
        }
    }
}
