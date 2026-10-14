using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

internal static partial class InteractiveChartBrowser {
    private static readonly object ColorEvidenceLock = new();

    /// <summary>Compares rendered sRGB RGBA paint, independent of Chromium's CSS serialization spelling.</summary>
    internal static Task AssertCssColorsEqualAsync(IPage page, string expected, string actual) =>
        AssertCssColorsAsync(page, expected, actual, equal: true);

    /// <summary>Requires distinct rendered RGBA paint rather than different CSS spelling.</summary>
    internal static Task AssertCssColorsDifferentAsync(IPage page, string first, string second) =>
        AssertCssColorsAsync(page, first, second, equal: false);

    private static async Task AssertCssColorsAsync(IPage page, string expected, string actual, bool equal) {
        var channels = await page.EvaluateAsync<int[][]>("""
            values => {
                const canvas = document.createElement('canvas');
                canvas.width = canvas.height = 1;
                const context = canvas.getContext('2d', { willReadFrequently: true, colorSpace: 'srgb' });
                return values.map(value => {
                    if (!value || !CSS.supports('color', value)) throw new Error('Invalid CSS color: ' + value);
                    // Canvas silently ignores unsupported colors. Two different sentinels distinguish
                    // an ignored assignment from a valid paint without implementing a CSS parser.
                    context.fillStyle = '#010203'; context.fillStyle = value;
                    const first = context.fillStyle;
                    context.fillStyle = '#040506'; context.fillStyle = value;
                    if (context.fillStyle !== first) throw new Error('Canvas cannot resolve CSS color: ' + value);
                    context.clearRect(0, 0, 1, 1);
                    context.fillRect(0, 0, 1, 1);
                    return [...context.getImageData(0, 0, 1, 1).data];
                });
            }
            """, new[] { expected, actual });
        var captures = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(captures)) {
            if (!Path.IsPathRooted(captures)) throw new InvalidOperationException("CFX_V2_CAPTURE_ROOT must be an explicit absolute output path.");
            Directory.CreateDirectory(captures);
            var evidence = JsonSerializer.Serialize(new { expected, actual, equal, expectedRgba = channels[0], actualRgba = channels[1] });
            lock (ColorEvidenceLock) File.AppendAllText(Path.Combine(captures, "browser-color-comparisons.jsonl"), evidence + "\n");
        }
        Assert.Equal(equal, channels[0].SequenceEqual(channels[1]));
    }
}
