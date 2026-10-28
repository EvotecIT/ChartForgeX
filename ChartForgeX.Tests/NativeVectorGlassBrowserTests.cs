using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class NativeVectorGlassBrowserTests {
    [BrowserTheory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public async Task FilteredBackdropDoesNotReplaceOrDuplicateVisibleNativeForegroundWords(string variant, VisualThemeMode mode) {
        var canvas = NativeVectorGlassExample.Create(variant, mode);
        var svg = canvas.ToSvg();
        await using var session = await BrowserProofFonts.OpenAsync("<style>body{margin:0}svg{max-width:100%;height:auto}</style>" + svg,
            variant == "compact" ? 390 : 1100, 430);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
              const svg=document.querySelector('svg'), frame=svg.getBoundingClientRect();
              return {texts:[...svg.querySelectorAll('text')].map(t=>({text:t.textContent,width:t.getBoundingClientRect().width,
                height:t.getBoundingClientRect().height,inside:(()=>{const b=t.getBoundingClientRect();return b.left>=frame.left&&b.right<=frame.right&&b.top>=frame.top&&b.bottom<=frame.bottom})()})),
                images:svg.querySelectorAll('image').length,filters:svg.querySelectorAll('filter,script,foreignObject').length,
                overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, "vector-glass-" + variant + "-" + mode);
            File.WriteAllText(stem + ".svg", svg);
            File.WriteAllBytes(stem + ".native.png", canvas.ToPng());
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        foreach (var label in new[] { "Service window", "Explicit local backdrop", "34 ready", "Foreground remains native text" }) {
            var text = Assert.Single(observed.GetProperty("texts").EnumerateArray(), item => item.GetProperty("text").GetString() == label);
            Assert.True(text.GetProperty("width").GetDouble() > 0);
            Assert.True(text.GetProperty("height").GetDouble() > 0);
            Assert.True(text.GetProperty("inside").GetBoolean());
        }
        Assert.Equal(1, observed.GetProperty("images").GetInt32());
        Assert.Equal(0, observed.GetProperty("filters").GetInt32());
        Assert.False(observed.GetProperty("overflow").GetBoolean());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
