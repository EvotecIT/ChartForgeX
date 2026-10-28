using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;

public static partial class PolarViewportRecipe {
    /// <summary>Prepares both responsive native drawings. A host supplies subsequent managed native replies through its existing bridge.</summary>
    public static HtmlChartSection PrepareSection(Chart chart) => new HtmlInteractiveChartRenderer().PreparePolarSection(chart, Window(), options => {
        options.Interaction.ChartId = "directional-response"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 420;
    });
}
