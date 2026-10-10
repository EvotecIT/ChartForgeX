using ChartForgeX.Core;
using ChartForgeX.Primitives;

public static partial class V2GalleryModels {
    private static Chart FinancialOptions(ChartSeriesKind kind) {
        var source = new[] {
            new ChartCandlestick(1, 44, 58, 38, 53), new ChartCandlestick(2, 53, 60, 42, 45),
            new ChartCandlestick(3, 46, 55, 41, 46), new ChartCandlestick(4, 47, 47, 47, 47),
            new ChartCandlestick(5, 47, 63, 44, 58)
        };
        var chart = Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri").WithYAxis("Price")
            .WithPointLegend().WithDataLabels(false);
        if (kind == ChartSeriesKind.Candlestick) chart.AddCandlestick("Daily prices", source);
        else chart.AddOhlc("Daily prices", source);
        chart.Series[0].ConfigureFinancial(financial => {
            financial.Rising.Stroke = ChartColor.FromHex("#2A967B");
            financial.Rising.StrokeWidth = 2.4;
            financial.Falling.Stroke = ChartColor.FromHex("#DB825B");
            financial.Falling.StrokeWidth = 2.8;
            financial.Falling.StrokeOpacity = .85;
            if (kind == ChartSeriesKind.Candlestick) {
                financial.Rising.FillOpacity = 0;
                financial.Rising.Wick.Stroke = ChartColor.FromHex("#729BB7");
                financial.Rising.Wick.StrokeWidth = 1.2;
                financial.Falling.Fill = ChartColor.FromHex("#EBA85B");
                financial.Falling.FillOpacity = .65;
                financial.Falling.Wick.Stroke = ChartColor.FromHex("#729BB7");
                financial.Falling.Wick.StrokeWidth = 1.4;
            }
        }).WithPointColor(4, "#729BB7");
        return chart;
    }
}
