using System.IO.Compression;
using System.Text;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Guards the enhance runtime's delivery contract: size budgets, CSP-safe code and once-per-document emission.</summary>
public sealed class EnhanceAssetTests {
    private static int Gzip(string text) {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true)) {
            var bytes = Encoding.UTF8.GetBytes(text);
            gzip.Write(bytes, 0, bytes.Length);
        }
        return (int)output.Length;
    }

    [Fact]
    public void Runtime_StaysWithinTheDesignLanguageBudgets() {
        // Design language section 4: the core runtime is at most 12 KB gzip and the stylesheet at most 2 KB.
        var core = Gzip(HtmlEnhanceAssets.Module("cfx-shared.js") + HtmlEnhanceAssets.Module("cfx-enhance.js"));
        Assert.True(core <= 12 * 1024, "Core enhance runtime is " + core + " bytes gzip.");
        Assert.True(Gzip(HtmlEnhanceAssets.Module("cfx-enhance.behaviours.js")) <= 4 * 1024, "Built-in behaviours exceed 4 KB gzip.");
        Assert.True(Gzip(HtmlEnhanceAssets.Module("cfx-link.js")) <= 2 * 1024, "The linked-views helper exceeds 2 KB gzip.");
        Assert.True(Gzip(HtmlEnhanceAssets.Style) <= 2 * 1024, "The enhance stylesheet exceeds 2 KB gzip.");
    }

    [Fact]
    public void Runtime_AvoidsDynamicCodeAndMarkupParsing() {
        var script = HtmlEnhanceAssets.Script;
        foreach (var forbidden in new[] { "eval(", "new Function", "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "setAttribute(\"style\"", "import(" })
            Assert.DoesNotContain(forbidden, script, StringComparison.Ordinal);
        foreach (var name in new[] { "CfxShared", "CfxEnhance", "CfxLink", "behaviour(\"crosshair\"", "behaviour(\"grid\"", "behaviour(\"rows\"", "behaviour(\"radial\"" })
            Assert.Contains(name, script, StringComparison.Ordinal);
    }

    [Fact]
    public void Document_EmitsTheRuntimeOnceWithItsNonce() {
        var document = new HtmlEnhanceDocument("r4nd0m+/=");
        var first = document.Assets();
        Assert.Contains("<script nonce=\"r4nd0m+/=\">", first, StringComparison.Ordinal);
        Assert.Contains("<style nonce=\"r4nd0m+/=\">", first, StringComparison.Ordinal);
        Assert.Equal(1, first.Split("</script>").Length - 1);
        Assert.Equal(string.Empty, document.Assets());
        Assert.True(document.Written);
        Assert.Throws<ArgumentException>(() => HtmlEnhanceAssets.InlineMarkup("bad\" onload=\"x"));
    }

    [Fact]
    public void Files_AreContentAddressedAndReferenced() {
        var assets = new HtmlAssetReferences("assets/") { IncludeIntegrity = true };
        var markup = HtmlEnhanceAssets.ReferenceMarkup(assets);
        Assert.Contains("href=\"assets/" + HtmlEnhanceAssets.StyleFile.FileName + "\"", markup, StringComparison.Ordinal);
        Assert.Contains("src=\"assets/" + HtmlEnhanceAssets.ScriptFile.FileName + "\"", markup, StringComparison.Ordinal);
        Assert.Contains(HtmlEnhanceAssets.ScriptFile.Integrity, markup, StringComparison.Ordinal);
        Assert.Equal(HtmlEnhanceAssets.Script, HtmlEnhanceAssets.ScriptFile.Content);
        Assert.Equal(2, HtmlEnhanceAssets.Files().Count);
        var directory = Path.Combine(Path.GetTempPath(), "cfx-enhance-" + Guid.NewGuid().ToString("N"));
        try {
            var written = HtmlInteractiveAssetFiles.WriteTo(directory, HtmlEnhanceAssets.Files());
            Assert.All(written, path => Assert.True(File.Exists(path)));
        } finally {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        Assert.Throws<ArgumentException>(() => HtmlEnhanceAssets.Module("interactive.js"));
    }
}
