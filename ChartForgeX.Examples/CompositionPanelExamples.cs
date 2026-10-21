using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

/// <summary>Shows reusable panels and fitted headers over transparent background patterns.</summary>
internal static class CompositionPanelExamples {
    internal static void Write(string output) {
        foreach (var dark in new[] { true, false }) {
            foreach (var width in new[] { 960, 480 }) {
                var height = width == 960 ? 420 : 320;
                var background = ChartColor.FromHex(dark ? "#0F172A" : "#EEF2F7");
                var foreground = ChartColor.FromHex(dark ? "#F1F5F9" : "#0F172A");
                var muted = ChartColor.FromHex(dark ? "#94A3B8" : "#475569");
                var accent = ChartColor.FromHex(dark ? "#38BDF8" : "#0369A1");
                var image = ImageComposition.Create(width, height, background);
                if (dark) image.FillGridPattern(foreground.WithAlpha(14), spacing: 25);
                else image.FillDotPattern(accent.WithAlpha(56), spacing: 25, radius: 1);
                var panel = ImageComposition.CreatePanel(width - 48, height - 48,
                    ChartColor.FromHex(dark ? "#132239E8" : "#FFFFFFE8"), accent.WithAlpha(120), cornerRadius: 20);
                var bodyWidth = panel.Width - 40;
                var titleStyle = TextStyle.Create(22, foreground);
                titleStyle.Font.Weight = 600;
                var noteStyle = TextStyle.Create(13, muted);
                var titleHeight = panel.DrawTextHeader(20, 22, bodyWidth, "Service overview", "Updated 10:42", titleStyle, noteStyle);
                var top = 22 + titleHeight + 22;
                panel.DrawLine(20, top, panel.Width - 20, top, accent.WithAlpha(70))
                    .DrawText(20, top + 20, bodyWidth, "Available capacity", 16, muted)
                    .DrawText(20, top + 50, bodyWidth, "72%", 38, foreground, emphasized: true)
                    .DrawProgressRing(panel.Width - 62, panel.Height - 62, 30, 6, .72,
                        accent.WithAlpha(40), accent);
                image.DrawImage(panel.ToImage(), 24, 24, panel.Width, panel.Height);
                var name = "composition-panels-" + (dark ? "dark-" : "light-") + width;
                image.Save(Path.Combine(output, name + ".png"));
            }
        }
    }
}
