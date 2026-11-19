# Region maps

ChartForgeX ships 23 region maps inside the core package: the world, the
continents, the United States in four projections and ten countries. They work
offline, need no CDN and carry no runtime dependency. Values join by region
code, alias or name, and markers and routes use the same projection as the
regions.

```csharp
var chart = Chart.Create()
    .WithTitle("Sales by country")
    .AddRegionMap("Sales", ChartMapCatalog.Get("europe"), new[] {
        new ChartRegionMapItem("PL", 82),
        new ChartRegionMapItem("DEU", 64),      // ISO alpha-3 alias
        new ChartRegionMapItem("France", 51)    // name
    })
    .WithMapColorScale(ChartColor.FromHex("#DBEAFE"), ChartColor.FromHex("#1D4ED8"));
```

## Built-in maps

| ID | Regions | Projection | Region codes |
| --- | ---: | --- | --- |
| `world` | 249 | Miller | ISO 3166-1 alpha-2 (`PL`), alpha-3 alias (`POL`), name |
| `world-mercator` | 249 | Web Mercator | same as `world` |
| `continents` | 6 | Miller | `AF`, `AS`, `EU`, `NA`, `OC`, `SA`, or the continent name |
| `africa`, `asia`, `europe`, `north-america`, `oceania`, `south-america` | 14 to 55 | Miller | same as `world` |
| `us-states` | 51 | United States Albers | state abbreviation (`TX`), ISO 3166-2 alias (`US-TX`), name |
| `us-states-lambert` | 51 | United States Lambert | same as `us-states` |
| `us-states-mercator` | 51 | United States Mercator | same as `us-states` |
| `us-states-miller` | 51 | United States Miller | same as `us-states` |
| `spain` | 52 provinces | Miller | ISO 3166-2 (`ES-M`), code without prefix (`M`), name |
| `russia` | 83 federal subjects | Miller | ISO 3166-2 (`RU-MOW`) |
| `canada` | 13 | Miller | ISO 3166-2 (`CA-ON`) |
| `iraq` | 18 governorates | Miller | ISO 3166-2 (`IQ-BG`) |
| `brazil` | 27 states | Miller | ISO 3166-2 (`BR-SP`) |
| `france` | 13 metropolitan regions | Miller | ISO 3166-2 (`FR-IDF`) |
| `germany` | 16 states | Miller | ISO 3166-2 (`DE-BY`) |
| `belgium` | 11 provinces | Miller | ISO 3166-2 (`BE-VAN`) |
| `netherlands` | 12 provinces | Miller | ISO 3166-2 (`NL-NH`) |
| `united-kingdom` | 4 countries | Miller | `GB-ENG`, `GB-SCT`, `GB-WLS`, `GB-NIR` |

`ChartMapCatalog.EmbeddedEntries()` lists the same maps with their source,
licence, join key and notes, so a host can show attribution next to the chart.
Maps built from Natural Earth atlases are decompressed and parsed the first
time they are requested and then cached. Maps that share an atlas (the world
and the continents) share the decompressed text, but each one parses it once on
first use. `us-states` uses prebuilt Albers shapes and needs no parsing.

The United States projections draw the lower 48 states at full size and move
Alaska and Hawaii into insets, so all four variants share one layout. They
can't be combined with an antimeridian viewport.

## Markers and routes

Region maps, markers and routes compose in one frame because every built-in map
declares its projection. Markers come from a dotted map dataset: each
`ChartMapPoint` has a label, longitude, latitude, an optional weight that sets
its size and an optional colour. Routes are straight or multi-point lines
between coordinates.

```csharp
var states = ChartMapCatalog.Get("us-states");
var chart = Chart.Create()
    .WithTitle("Regional offices")
    .WithDataLabels()
    .AddRegionMap("Coverage", states, new[] {
        new ChartRegionMapItem("CO", 40),
        new ChartRegionMapItem("NY", 90)
    })
    .AddDottedMap("Offices", new[] {
        new ChartMapPoint("Denver", -104.99, 39.74, 40),
        new ChartMapPoint("New York", -74.00, 40.71, 90, ChartColor.FromHex("#DC2626")),
        new ChartMapPoint("Anchorage", -149.90, 61.22, 20),
        new ChartMapPoint("Honolulu", -157.86, 21.31, 30)
    })
    .AddMapRoute("Denver to New York", -104.99, 39.74, -74.00, 40.71)
    .AddMapRouteBetweenPoints("Denver to Anchorage", "Denver", "Anchorage");
```

Use one colour per status (for example green, amber and red) to show a tone
for each marker; `WithColorValue` on a point maps it through the dataset's
colour scale instead. `ChartMapPointSizeOptions` (`ConfigureMapPointSize`)
controls how weights become marker radii.

The frame fits the regions and every marker and route. A marker far outside the
map widens the frame; leave it out, or fix the frame with
`WithRegionMapBounds`.

The `russia` and `oceania` maps store places east of the antimeridian (Chukotka,
Fiji, Samoa and others) at longitudes above 180 so the region stays in one
piece. Markers there, which use longitudes between -180 and 180, aren't placed
on these two maps.

## Replacing jsVectorMap datasets

| jsVectorMap map | ChartForgeX ID |
| --- | --- |
| `world` | `world` |
| `world_merc` | `world-mercator` |
| `us_aea_en` | `us-states` |
| `us_lcc_en` | `us-states-lambert` |
| `us_merc_en` | `us-states-mercator` |
| `us_mill_en` | `us-states-miller` |
| `continents_mill` | `continents` |
| `africa_mill`, `asia_mill`, `europe_mill`, `north_america_mill`, `oceania_mill`, `south_america_mill` | `africa`, `asia`, `europe`, `north-america`, `oceania`, `south-america` |
| `spain`, `russia`, `canada`, `iraq`, `brazil` | `spain`, `russia`, `canada`, `iraq`, `brazil` |
| `france_mill`, `germany_mill`, `belgium_mill`, `netherlands_mill` | `france`, `germany`, `belgium`, `netherlands` |
| `uk_countries_mill` | `united-kingdom` |

World and continent maps use the same ISO alpha-2 codes as jsVectorMap.
Subdivision maps accept ISO 3166-2 codes. TopoJSON input stays with the host:
convert it to GeoJSON and load it with `ChartMapGeoJson.ToMapDefinition`.

## Sources and licences

| Maps | Source | Licence |
| --- | --- | --- |
| `world`, `world-mercator`, continents | Natural Earth 5.1.2, 1:10m admin-0 countries, German point-of-view edition | Public domain |
| `us-states` | U.S. Census Bureau state boundaries via us-atlas `states-albers-10m` | Public domain |
| `us-states-lambert`, `us-states-mercator`, `us-states-miller` | Natural Earth 5.1.2, 1:10m admin-1 | Public domain |
| country maps | Natural Earth 5.1.2, 1:10m admin-1 (`united-kingdom` from 1:10m map units) | Public domain |

The world maps use Natural Earth's German point-of-view edition, which follows
internationally recognised borders (for example Crimea is part of Ukraine) and
otherwise matches the default edition. The `russia` map leaves out Crimea and
Sevastopol for the same reason. Antarctica is omitted. Natural Earth terms of
use: <https://www.naturalearthdata.com/about/terms-of-use/>.

## Regenerating the geometry

`tools/region-maps/build-region-maps.mjs` downloads the pinned Natural Earth
release, simplifies it with mapshaper (a pinned build-only tool, so shared
borders stay aligned) and writes `ChartForgeX/Maps/Data/*.geojson.gz`. The
output is byte-for-byte reproducible.

```bash
cd tools/region-maps
npm ci
node build-region-maps.mjs <download-cache-directory>
```

All maps together add about 250 KiB of compressed geometry to the assembly.
