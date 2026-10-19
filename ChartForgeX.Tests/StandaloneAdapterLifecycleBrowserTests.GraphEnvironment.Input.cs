using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    private static async Task AssertGraphEnvironmentRapidInputAsync(IPage page, string name) {
        var clear = page.GetByRole(AriaRole.Button, new() { Name = "Clear selection", Exact = true });
        var selectionModifier = OperatingSystem.IsMacOS() ? "Meta" : "Control";
        var observations = new List<JsonElement>();
        async Task ObserveAsync(string action, string expected, int before) {
            var facts = await page.EvaluateAsync<JsonElement>("""
                action=>({action,time:performance.now(),primary:ChartForgeXGraphExplorer.get(retainedGraph).selection.primary||'',
                    selection:ChartForgeXGraphExplorer.captureState(retainedGraph).selection,events:graphEnvironmentSelections,
                    actual:retainedGraph.dataset.cfxGraphRendererActive,pointerHit:retainedGraph.dataset.cfxGraphLastPointerHit})
                """, action);
            observations.Add(facts);
            Assert.Equal(expected, facts.GetProperty("primary").GetString());
            Assert.Equal(before + 1, facts.GetProperty("events").GetInt32());
        }
        async Task ActivateAsync(string id, bool modified = false) {
            var before = await page.EvaluateAsync<int>("()=>graphEnvironmentSelections");
            if (modified) await page.Keyboard.DownAsync(selectionModifier);
            try { await ClickGraphEnvironmentPointAsync(page, id); }
            finally { if (modified) await page.Keyboard.UpAsync(selectionModifier); }
            await ObserveAsync((modified ? selectionModifier + "+" : "") + "click " + id, modified ? "" : id, before);
        }
        try {
            await clear.ClickAsync();
            await ActivateAsync("a");
            await ActivateAsync("b");
            await ActivateAsync("b", modified: true);
            await ActivateAsync("a");
            var before = await page.EvaluateAsync<int>("()=>graphEnvironmentSelections");
            await clear.ClickAsync();
            await ObserveAsync("Clear selection", "", before);
            await ActivateAsync("a");
        } finally {
            await WriteReceiptAsync(name + "-rapid-input", JsonSerializer.SerializeToElement(observations));
        }
    }
}
