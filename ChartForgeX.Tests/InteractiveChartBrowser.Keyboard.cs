using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

internal static partial class InteractiveChartBrowser {
    /// <summary>
    /// Establishes real keyboard input before a host moves focus to a specific native target.
    /// Navigation-order contracts use their own Tab/arrow traversal instead of this modality setup.
    /// </summary>
    internal static async Task FocusWithKeyboardAsync(IPage page, ILocator target) {
        await page.Keyboard.PressAsync("Tab");
        await target.FocusAsync();
        Assert.True(await target.EvaluateAsync<bool>("node => node.getRootNode().activeElement === node"), "The requested native target owns focus.");
        Assert.True(await target.EvaluateAsync<bool>("node => node.matches(':focus-visible')"), "The requested target has keyboard-visible focus.");
    }
}
