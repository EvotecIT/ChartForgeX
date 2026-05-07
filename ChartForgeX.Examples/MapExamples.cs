using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class MapExamples {
    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        Save(CreateCalendarHeatmap(), output, "developer-consistency-calendar-light", pngOutputScale);
        Save(CreateDottedMap(), output, "travel-dotted-map-dark", pngOutputScale);
        Save(CreateEuropeRevenueDottedMap(), output, "revenue-europe-country-map-light", pngOutputScale);
        foreach (var example in MapViewportExamples()) {
            Save(CreateViewportMap(example.Title + " Route Map", example.Viewport, example.Points, example.Route, 760, 460, example.Viewport.Name + " dotted viewport with route connectors"), output, example.FileName, pngOutputScale);
        }

        SaveGrid(CreateMapViewportShowcaseGrid(), output, "map-viewport-showcase-grid", pngOutputScale);
        Save(CreateRegionMap(), output, "revenue-region-map-us-states-light", pngOutputScale);
        Save(CreateTileMap(), output, "revenue-tile-map-us-states-light", pngOutputScale);
    }

    private static Chart CreateCalendarHeatmap() {
        return Chart.Create()
            .WithTitle("Developer Consistency Calendar")
            .WithSubtitle("Contribution-style day grid with focusable SVG regions and native hover titles")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(980, 420)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
            .AddCalendarHeatmap("Commits", CalendarActivity(), ChartColor.FromRgb(34, 197, 94));
    }

    private static IEnumerable<ChartCalendarHeatmapItem> CalendarActivity() {
        var start = new DateTime(2026, 1, 1);
        for (var i = 0; i < 365; i++) {
            var date = start.AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            if ((i + date.Month) % 6 == 0) continue;
            var value = 1 + (i * 7 + date.Month * 3) % 11;
            yield return new ChartCalendarHeatmapItem(date, value);
        }

        yield return new ChartCalendarHeatmapItem(new DateTime(2026, 3, 16), 4);
        yield return new ChartCalendarHeatmapItem(new DateTime(2026, 9, 21), 6);
    }

    private static Chart CreateDottedMap() {
        return Chart.Create()
            .WithTitle("Travel Map")
            .WithSubtitle("Dotted world layer with highlighted longitude and latitude points")
            .WithTheme(ChartTheme.ReportDark())
            .WithSize(980, 560)
            .WithLegend(false)
            .WithDataLabels()
            .AddDottedMap("Visited", new[] {
                new ChartMapPoint("Indonesia", 113.9213, -0.7893, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("Spain", -3.7038, 40.4168, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("United States", -98.5795, 39.8283, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("Norway", 8.4689, 60.4720, ChartColor.FromRgb(59, 130, 246))
            })
            .AddMapRouteBetweenPoints("United States to Spain", "United States", "Spain", ChartColor.FromRgb(34, 197, 94))
            .AddMapRouteBetweenPoints("Spain to Indonesia", "Spain", "Indonesia", ChartColor.FromRgb(59, 130, 246));
    }

    private static ChartGrid CreateMapViewportShowcaseGrid() {
        var grid = ChartGrid.Create()
            .WithTitle("Map Viewport Showcase")
            .WithSubtitle("The same dotted map layer can focus on continents, Europe, Poland, or custom longitude/latitude windows")
            .WithBrandKit(ChartBrandKit.Executive())
            .WithColumns(2)
            .WithPadding(30)
            .WithGap(18)
            .WithPanelSize(520, 340);

        foreach (var example in MapViewportExamples()) {
            grid.Add(CreateViewportMap(example.Title, example.Viewport, example.Points, example.Route, 520, 340, example.Viewport.Name + " viewport"));
        }

        return grid;
    }

    private static MapViewportExample[] MapViewportExamples() {
        return new[] {
            new MapViewportExample(
                "World",
                "map-world-route-light",
                ChartMapViewport.World(),
                new[] {
                new ChartMapPoint("United States", -98.5795, 39.8283, ChartColor.FromRgb(37, 99, 235)),
                new ChartMapPoint("Spain", -3.7038, 40.4168, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("Indonesia", 113.9213, -0.7893, ChartColor.FromRgb(34, 197, 94))
                },
                new MapRouteSpec("US to Spain", "United States", "Spain")),
            new MapViewportExample(
                "Europe",
                "map-europe-route-light",
                ChartMapViewport.Europe(),
                new[] {
                new ChartMapPoint("Madrid", -3.7038, 40.4168, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("Warsaw", 21.0122, 52.2297, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("Oslo", 10.7522, 59.9139, ChartColor.FromRgb(59, 130, 246))
                },
                new MapRouteSpec("Madrid to Warsaw", "Madrid", "Warsaw")),
            new MapViewportExample(
                "North America",
                "map-north-america-route-light",
                ChartMapViewport.NorthAmerica(),
                new[] {
                new ChartMapPoint("San Francisco", -122.4194, 37.7749, ChartColor.FromRgb(37, 99, 235)),
                new ChartMapPoint("New York", -74.0060, 40.7128, ChartColor.FromRgb(37, 99, 235)),
                new ChartMapPoint("Toronto", -79.3832, 43.6532, ChartColor.FromRgb(14, 165, 233))
                },
                new MapRouteSpec("SF to New York", "San Francisco", "New York")),
            new MapViewportExample(
                "South America",
                "map-south-america-route-light",
                ChartMapViewport.SouthAmerica(),
                new[] {
                new ChartMapPoint("Lima", -77.0428, -12.0464, ChartColor.FromRgb(245, 158, 11)),
                new ChartMapPoint("Sao Paulo", -46.6333, -23.5505, ChartColor.FromRgb(245, 158, 11)),
                new ChartMapPoint("Buenos Aires", -58.3816, -34.6037, ChartColor.FromRgb(239, 68, 68))
                },
                new MapRouteSpec("Lima to Sao Paulo", "Lima", "Sao Paulo")),
            new MapViewportExample(
                "Africa",
                "map-africa-route-light",
                ChartMapViewport.Africa(),
                new[] {
                new ChartMapPoint("Cairo", 31.2357, 30.0444, ChartColor.FromRgb(20, 184, 166)),
                new ChartMapPoint("Lagos", 3.3792, 6.5244, ChartColor.FromRgb(20, 184, 166)),
                new ChartMapPoint("Cape Town", 18.4241, -33.9249, ChartColor.FromRgb(6, 182, 212))
                },
                new MapRouteSpec("Cairo to Cape Town", "Cairo", "Cape Town")),
            new MapViewportExample(
                "Asia",
                "map-asia-route-light",
                ChartMapViewport.Asia(),
                new[] {
                new ChartMapPoint("Singapore", 103.8198, 1.3521, ChartColor.FromRgb(124, 58, 237)),
                new ChartMapPoint("Tokyo", 139.6503, 35.6762, ChartColor.FromRgb(124, 58, 237)),
                new ChartMapPoint("Seoul", 126.9780, 37.5665, ChartColor.FromRgb(168, 85, 247))
                },
                new MapRouteSpec("Singapore to Tokyo", "Singapore", "Tokyo")),
            new MapViewportExample(
                "Oceania",
                "map-oceania-route-light",
                ChartMapViewport.Oceania(),
                new[] {
                new ChartMapPoint("Sydney", 151.2093, -33.8688, ChartColor.FromRgb(14, 165, 233)),
                new ChartMapPoint("Auckland", 174.7633, -36.8485, ChartColor.FromRgb(14, 165, 233)),
                new ChartMapPoint("Jakarta", 106.8456, -6.2088, ChartColor.FromRgb(34, 197, 94))
                },
                new MapRouteSpec("Sydney to Auckland", "Sydney", "Auckland")),
            new MapViewportExample(
                "Poland",
                "map-poland-route-light",
                ChartMapViewport.Poland(),
                new[] {
                new ChartMapPoint("Gdansk", 18.6466, 54.3520, ChartColor.FromRgb(220, 38, 38)),
                new ChartMapPoint("Warsaw", 21.0122, 52.2297, ChartColor.FromRgb(220, 38, 38)),
                new ChartMapPoint("Krakow", 19.9450, 50.0647, ChartColor.FromRgb(239, 68, 68))
                },
                new MapRouteSpec("Gdansk to Krakow", "Gdansk", "Krakow"))
        };
    }

    private static Chart CreateViewportMap(string title, ChartMapViewport viewport, IEnumerable<ChartMapPoint> points, MapRouteSpec route, int width, int height, string subtitle) {
        return Chart.Create()
            .WithTitle(title)
            .WithSubtitle(subtitle)
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(width, height)
            .WithLegend(false)
            .WithMapViewport(viewport)
            .WithDataLabels()
            .AddDottedMap("Cities", points, ChartColor.FromRgb(37, 99, 235))
            .AddMapRouteBetweenPoints(route.Label, route.FromPointLabel, route.ToPointLabel, ChartColor.FromRgb(37, 99, 235));
    }

    private static Chart CreateEuropeRevenueDottedMap() {
        return Chart.Create()
            .WithTitle("Revenue by European Market")
            .WithSubtitle("Weighted country markers with route overlays for regional revenue")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(980, 560)
            .WithLegend(false)
            .WithMapViewport(ChartMapViewport.Europe())
            .WithDataLabels()
            .WithDataLabelStyle(style => style.WithFontSize(11.5))
            .WithValueFormatter(value => "$" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "k")
            .AddDottedMap("Revenue", new[] {
                new ChartMapPoint("United Kingdom", -1.1743, 52.3555, 188, ChartColor.FromRgb(37, 99, 235)),
                new ChartMapPoint("Poland", 19.1451, 51.9194, 142, ChartColor.FromRgb(220, 38, 38)),
                new ChartMapPoint("Spain", -3.7038, 40.4168, 96, ChartColor.FromRgb(245, 158, 11)),
                new ChartMapPoint("Germany", 10.4515, 51.1657, 214, ChartColor.FromRgb(34, 197, 94)),
                new ChartMapPoint("Norway", 8.4689, 60.4720, 74, ChartColor.FromRgb(14, 165, 233))
            }, ChartColor.FromRgb(37, 99, 235))
            .AddMapRouteBetweenPoints("United Kingdom to Poland", "United Kingdom", "Poland", ChartColor.FromRgb(37, 99, 235))
            .AddMapRouteBetweenPoints("Spain to Germany", "Spain", "Germany", ChartColor.FromRgb(34, 197, 94));
    }

    private static Chart CreateTileMap() {
        return Chart.Create()
            .WithTitle("Revenue Tile Map")
            .WithSubtitle("Catalog-backed tile geography for compact regional comparison")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(980, 500)
            .WithLegend(false)
            .WithValueFormatter(value => "$" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "k")
            .AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), StateRevenue(), ChartColor.FromRgb(37, 99, 235));
    }

    private static Chart CreateRegionMap() {
        return Chart.Create()
            .WithTitle("Revenue Region Map")
            .WithSubtitle("Catalog-backed SVG geometry with keyboard-focusable regions")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(980, 560)
            .WithLegend(false)
            .WithMapLabels(false)
            .WithValueFormatter(value => "$" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "k")
            .AddRegionMap("Revenue", ChartMapCatalog.Get("us-states"), StateRevenue(), ChartColor.FromRgb(37, 99, 235));
    }

    private static IEnumerable<ChartRegionMapItem> StateRevenue() {
        var states = new[] {
            "AK", "ME", "VT", "NH", "WA", "MT", "ND", "MN", "WI", "MI", "NY", "MA", "RI",
            "OR", "ID", "SD", "IA", "IL", "IN", "OH", "PA", "NJ", "CT",
            "CA", "NV", "WY", "NE", "MO", "KY", "WV", "VA", "MD", "DE",
            "AZ", "UT", "CO", "KS", "AR", "TN", "NC", "SC", "DC",
            "HI", "NM", "OK", "LA", "MS", "AL", "GA", "TX", "FL"
        };

        for (var i = 0; i < states.Length; i++) {
            var code = states[i];
            var value = 28 + ((code[0] * 7 + code[1] * 11 + i * 5) % 68);
            yield return new ChartRegionMapItem(code, value);
        }
    }

    private static void Save(Chart chart, string output, string name, ChartPngOutputScale pngOutputScale) {
        chart.WithPngOutputScale(pngOutputScale);
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SaveHtml(Path.Combine(output, name + ".html"));
        chart.SavePng(Path.Combine(output, name + ".png"));
    }

    private static void SaveGrid(ChartGrid grid, string output, string name, ChartPngOutputScale pngOutputScale) {
        grid.WithPngOutputScale(pngOutputScale);
        grid.SaveSvg(Path.Combine(output, name + ".svg"));
        grid.SaveHtml(Path.Combine(output, name + ".html"));
        grid.SavePng(Path.Combine(output, name + ".png"));
    }

    private readonly struct MapViewportExample {
        public readonly string Title;
        public readonly string FileName;
        public readonly ChartMapViewport Viewport;
        public readonly ChartMapPoint[] Points;
        public readonly MapRouteSpec Route;

        public MapViewportExample(string title, string fileName, ChartMapViewport viewport, ChartMapPoint[] points, MapRouteSpec route) {
            Title = title;
            FileName = fileName;
            Viewport = viewport;
            Points = points;
            Route = route;
        }
    }

    private readonly struct MapRouteSpec {
        public readonly string Label;
        public readonly string FromPointLabel;
        public readonly string ToPointLabel;

        public MapRouteSpec(string label, string fromPointLabel, string toPointLabel) {
            Label = label;
            FromPointLabel = fromPointLabel;
            ToPointLabel = toPointLabel;
        }
    }
}
