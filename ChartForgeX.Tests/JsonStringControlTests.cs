using System.Globalization;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class JsonStringControlTests {
    [Fact]
    public void SharedReaderRejectsEveryUnescapedControlInKeysAndValues() {
        for (var code = 0; code < 32; code++) {
            var control = ((char)code).ToString();
            foreach (var json in new[] { "{\"key\":\"before" + control + "after\"}", "{\"before" + control + "after\":1}" }) {
                var error = Assert.Throws<ArgumentException>(() => GeoJsonValue.Parse(json));
                Assert.Contains("Unescaped control character", error.Message, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void SharedReaderKeepsEscapedControlsAndPrintableUnicode() {
        for (var code = 0; code < 32; code++) {
            var escaped = "\\u" + code.ToString("x4", CultureInfo.InvariantCulture);
            var values = GeoJsonValue.Parse("{\"before" + escaped + "after\":\"Ω" + escaped + "漢\"}").AsObject("escaped strings");
            Assert.Equal("Ω" + (char)code + "漢", values["before" + (char)code + "after"].AsString("value"));
        }
        Assert.Equal("\b\f\n\r\t", GeoJsonValue.Parse("\"\\b\\f\\n\\r\\t\"").AsString("escapes"));
    }
}
