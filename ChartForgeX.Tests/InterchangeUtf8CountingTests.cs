using System.Text;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class InterchangeUtf8CountingTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameEscaperCountsExactRawOrEscapedBmpSurrogateAndControlBytes(bool escapeMarkup) {
        const string text="A\u03A9\U0001F600\n<\u0000";
        var output=new VisualArtifactInterchangeJsonWriter(escapeMarkupUnsafeCharacters:escapeMarkup);
        var count=new VisualArtifactInterchangeJsonWriter(writeText:false,escapeMarkupUnsafeCharacters:escapeMarkup);
        output.String(text);count.String(text);var json=output.ToString();
        Assert.Equal(json.Length,count.CharactersWritten);
        Assert.Equal(Encoding.UTF8.GetByteCount(json),count.Utf8BytesWritten);
        Assert.Equal(output.CharactersWritten,count.CharactersWritten);
        Assert.Equal(output.Utf8BytesWritten,count.Utf8BytesWritten);
    }
}
