using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class MapExamples {
    public static void Write(string output, ChartPngOutputScale pngOutputScale, bool includeExternalCatalogMaps = false) {
        Save(CreateCalendarHeatmap(), output, "developer-consistency-calendar-light", pngOutputScale);
        Save(CreateDottedMap(), output, "travel-dotted-map-dark", pngOutputScale);
        Save(CreateEuropeRevenueDottedMap(), output, "revenue-europe-country-map-light", pngOutputScale);
        foreach (var example in MapViewportExamples()) {
            Save(CreateViewportMap(example.Title + " Route Map", example.Viewport, example.Points, example.Route, 760, 460, example.Viewport.Name + " dotted viewport with route connectors"), output, example.FileName, pngOutputScale);
        }

        SaveGrid(CreateMapViewportShowcaseGrid(), output, "map-viewport-showcase-grid", pngOutputScale);
        Save(CreateRegionMap(), output, "revenue-region-map-us-states-light", pngOutputScale);
        Save(CreateIndustrialBirthsRegionMap(), output, "industrial-births-region-map-us-states-light", pngOutputScale);
        Save(CreateTileMap(), output, "revenue-tile-map-us-states-light", pngOutputScale);
        if (includeExternalCatalogMaps) SaveOptionalCatalogRegionMapExamples(output, pngOutputScale);
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

    private static Chart CreateIndustrialBirthsRegionMap() {
        var colorScale = ChartMapColorScale
            .Diverging(ChartColor.FromHex("#F97316"), ChartColor.FromHex("#FFF7ED"), ChartColor.FromHex("#065F46"), 3.2)
            .WithValueRange(0, 10)
            .WithLabels("0", "3.2 median", ">10")
            .WithNoDataColor(ChartColor.FromHex("#E5E7EB"));

        return Chart.Create()
            .WithTitle("Where Industrial Firms Are Being Born")
            .WithSubtitle("Births per 10,000 residents with a diverging choropleth scale")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(980, 560)
            .WithLegend(false)
            .WithMapLabels(false)
            .WithMapColorScale(colorScale)
            .WithValueFormatter(value => value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
            .AddRegionHeatmap("Births", ChartMapCatalog.Get("us-states"), StateIndustrialBirths());
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

    private static IEnumerable<ChartRegionMapItem> StateIndustrialBirths() {
        var states = new[] {
            "AK", "ME", "VT", "NH", "WA", "MT", "ND", "MN", "WI", "MI", "NY", "MA", "RI",
            "OR", "ID", "SD", "IA", "IL", "IN", "OH", "PA", "NJ", "CT",
            "CA", "NV", "WY", "NE", "MO", "KY", "WV", "VA", "MD", "DE",
            "AZ", "UT", "CO", "KS", "AR", "TN", "NC", "SC", "DC",
            "HI", "NM", "OK", "LA", "MS", "AL", "GA", "TX", "FL"
        };

        var suppressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AK", "HI", "WY", "WV", "MS" };
        for (var i = 0; i < states.Length; i++) {
            var code = states[i];
            if (suppressed.Contains(code)) continue;
            var baseValue = ((code[0] * 13 + code[1] * 17 + i * 9) % 125) / 10.0;
            var regionalLift = code is "CA" or "TX" or "NC" or "GA" or "UT" ? 2.4 : code is "MI" or "OH" or "PA" or "IL" ? -2.1 : 0;
            yield return new ChartRegionMapItem(code, Math.Max(0, baseValue + regionalLift));
        }
    }

    private static void SaveOptionalCatalogRegionMapExamples(string output, ChartPngOutputScale pngOutputScale) {
        var assets = FindCatalogAssetDirectory();
        if (assets == null) return;
        Save(CreateNuts3EuropeIndustrialBirthsMap(assets), output, "eu-industrial-births-nuts3-light", pngOutputScale);
        foreach (var example in new[] {
            new CatalogCountryMapExample("Poland", "PL", "industrial-births-poland-nuts3-light", 14, 25, 49, 55),
            new CatalogCountryMapExample("France", "FR", "industrial-births-france-nuts3-light", -6, 10, 41, 52),
            new CatalogCountryMapExample("Germany", "DE", "industrial-births-germany-nuts3-light", 5, 16, 47, 56)
        }) {
            Save(CreateNuts3CountryIndustrialBirthsMap(assets, example), output, example.FileName, pngOutputScale);
        }
    }

    private static Chart CreateNuts3EuropeIndustrialBirthsMap(string assets) {
        var euCodes = new[] { "AT", "BE", "BG", "CY", "CZ", "DE", "DK", "EE", "EL", "ES", "FI", "FR", "HR", "HU", "IE", "IT", "LT", "LU", "LV", "NL", "PL", "PT", "RO", "SE", "SI", "SK" };
        var nuts3 = ChartMapCatalog.Load("eu-nuts3-2021", assets, options => {
            options.WithCoordinateBounds(-12, 42, 34, 72);
            options.IncludeFeaturePropertyValues("CNTR_CODE", euCodes);
        });
        var countries = ChartMapCatalog.Load("eu-nuts0-2021", assets, options => options.WithCoordinateBounds(-12, 42, 34, 72, includeIntersections: false));
        var scale = IndustrialBirthsScale();

        return Chart.Create()
            .WithTitle("Where new industrial firms are being born across EU regions")
            .WithSubtitle("Catalog-loaded EU NUTS3 geometry with per-region heatmap values")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(1120, 768)
            .WithCard(false)
            .WithPlotBackground(false)
            .WithMapSurface(false)
            .WithLegend(false)
            .WithMapLabels(false)
            .WithMapRegionStroke(ChartColor.FromRgba(255, 255, 255, 165), 0.75)
            .WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right)
            .WithRegionMapCoordinateBounds(-12, 42, 34, 72)
            .AddMapBaseLayer(countries, ChartColor.FromHex("#E8E8E8"), ChartColor.FromHex("#D3D3D3"), 0.75)
            .AddRegionHeatmap("Births per 10,000 residents", nuts3, EuNuts3IndustrialBirthsValues(nuts3), scale)
            .AddMapBoundaryLayer(countries, ChartColor.FromRgba(17, 24, 39, 230), 0.78);
    }

    private static Chart CreateNuts3CountryIndustrialBirthsMap(string assets, CatalogCountryMapExample example) {
        var definition = ChartMapCatalog.Load("eu-nuts3-2021", assets, options => {
            options.WithCoordinateBounds(example.MinimumLongitude, example.MaximumLongitude, example.MinimumLatitude, example.MaximumLatitude);
            options.IncludeFeaturePropertyValues("CNTR_CODE", example.CountryCode);
        });
        var scale = IndustrialBirthsScale();

        return Chart.Create()
            .WithTitle(example.Country + " NUTS3 Industrial Births")
            .WithSubtitle("Catalog-loaded EU NUTS3 geometry filtered by CNTR_CODE=" + example.CountryCode)
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(860, 680)
            .WithCard(false)
            .WithPlotBackground(false)
            .WithMapSurface(false)
            .WithLegend(false)
            .WithMapLabels(false)
            .WithMapRegionStroke(ChartColor.FromRgba(255, 255, 255, 170), 0.75)
            .WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right)
            .WithRegionMapCoordinateBounds(example.MinimumLongitude, example.MaximumLongitude, example.MinimumLatitude, example.MaximumLatitude)
            .AddRegionHeatmap("Births per 10,000 residents", definition, CatalogCountryValues(definition, example.CountryCode), scale);
    }

    private static ChartMapColorScale IndustrialBirthsScale() {
        return ChartMapColorScale
            .Diverging(ChartColor.FromHex("#F97316"), ChartColor.FromHex("#FFF7ED"), ChartColor.FromHex("#065F46"), 3.2)
            .WithValueRange(0, 10)
            .WithLabels("0", "3.2 median", ">10")
            .WithNoDataColor(ChartColor.FromHex("#E5E7EB"));
    }

    private static IEnumerable<ChartRegionMapItem> EuNuts3IndustrialBirthsValues(ChartMapDefinition definition) {
        foreach (var region in definition.Regions) {
            var country = region.Code.Length >= 2 ? region.Code.Substring(0, 2) : region.Code;
            if (country == "IT" || country == "CY") continue;
            var noise = ((region.Code[region.Code.Length - 1] * 13 + region.Code.Length * 11) % 71) / 10.0 - 3.0;
            var baseline = country switch {
                "FR" => 6.2,
                "PL" => 5.1,
                "CZ" or "SK" or "HU" => 6.4,
                "EE" or "LV" or "LT" => 7.2,
                "RO" or "BG" or "EL" or "HR" or "SI" => 4.8,
                "DE" or "NL" or "BE" or "AT" => 2.1,
                "ES" => 1.4,
                _ => 3.4
            };
            if ((country == "DE" || country == "NL" || country == "BE") && noise < -1.6) continue;
            yield return new ChartRegionMapItem(region.Code, Math.Min(10, Math.Max(0, baseline + noise)));
        }
    }

    private static IEnumerable<ChartRegionMapItem> CatalogCountryValues(ChartMapDefinition definition, string countryCode) {
        foreach (var region in definition.Regions) {
            var noise = ((region.Code[region.Code.Length - 1] * 19 + region.Code.Length * 7) % 61) / 10.0;
            var baseline = countryCode switch {
                "PL" => 4.7,
                "FR" => 5.6,
                "DE" => 2.2,
                _ => 3.2
            };
            if (countryCode == "DE" && noise < 1.0) continue;
            yield return new ChartRegionMapItem(region.Code, Math.Min(10, Math.Max(0, baseline + noise - 2.4)));
        }
    }

    private static string? FindCatalogAssetDirectory() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null) {
            var candidate = Path.Combine(directory.FullName, "artifacts");
            if (File.Exists(Path.Combine(candidate, ChartMapCatalog.GetEntry("eu-nuts3-2021").FileName))) return candidate;
            directory = directory.Parent;
        }

        return null;
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

    private readonly struct CatalogCountryMapExample {
        public readonly string Country;
        public readonly string CountryCode;
        public readonly string FileName;
        public readonly double MinimumLongitude;
        public readonly double MaximumLongitude;
        public readonly double MinimumLatitude;
        public readonly double MaximumLatitude;

        public CatalogCountryMapExample(string country, string countryCode, string fileName, double minimumLongitude, double maximumLongitude, double minimumLatitude, double maximumLatitude) {
            Country = country;
            CountryCode = countryCode;
            FileName = fileName;
            MinimumLongitude = minimumLongitude;
            MaximumLongitude = maximumLongitude;
            MinimumLatitude = minimumLatitude;
            MaximumLatitude = maximumLatitude;
        }
    }
}
