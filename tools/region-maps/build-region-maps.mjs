// Regenerates the built-in region map geometry embedded in ChartForgeX.
//
// Source: Natural Earth 5.1.2 (public domain), https://www.naturalearthdata.com/about/terms-of-use/
// Simplification: mapshaper (pinned in package.json), shared borders simplified once so neighbours stay aligned.
//
// Usage:
//   npm ci
//   node build-region-maps.mjs <download-cache-directory>
//
// The script writes ChartForgeX/Maps/Data/*.geojson.gz. Each feature keeps only code, name, aliases, label and group.
import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { gzipSync } from "node:zlib";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repository = resolve(here, "..", "..");
const output = join(repository, "ChartForgeX", "Maps", "Data");
const cache = resolve(process.argv[2] ?? join(here, ".cache"));
const mapshaper = join(here, "node_modules", ".bin", process.platform === "win32" ? "mapshaper.cmd" : "mapshaper");
const naturalEarth = "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/v5.1.2/geojson/";

// The German point-of-view edition follows internationally recognised borders (for example Crimea is part of Ukraine);
// it matches the default edition for Kosovo, Western Sahara and Taiwan.
const countries = "ne_10m_admin_0_countries_deu";
const mapUnits = "ne_10m_admin_0_map_units";
const provinces = "ne_10m_admin_1_states_provinces";

const continentCodes = {
  "Africa": "AF", "Asia": "AS", "Europe": "EU", "North America": "NA", "South America": "SA", "Oceania": "OC"
};

// ISO 3166-2 codes for the 13 metropolitan French regions, keyed by the Natural Earth region name.
const frenchRegions = {
  "Auvergne-Rhône-Alpes": "FR-ARA", "Bourgogne-Franche-Comté": "FR-BFC", "Bretagne": "FR-BRE", "Centre-Val de Loire": "FR-CVL",
  "Corse": "FR-20R", "Grand Est": "FR-GES", "Hauts-de-France": "FR-HDF", "Île-de-France": "FR-IDF", "Normandie": "FR-NOR",
  "Nouvelle-Aquitaine": "FR-NAQ", "Occitanie": "FR-OCC", "Pays de la Loire": "FR-PDL", "Provence-Alpes-Côte-d'Azur": "FR-PAC"
};

const britishCountries = { ENG: ["GB-ENG", "England"], SCT: ["GB-SCT", "Scotland"], WLS: ["GB-WLS", "Wales"], NIR: ["GB-NIR", "Northern Ireland"] };

const admin1 = (country, interval, precision, extra = {}) => ({
  source: provinces,
  // Natural Earth marks unnamed fragments (for example a small Kara Sea island) with a "~" code; they carry no region.
  filter: `iso_a2 == '${country}' && !iso_3166_2.includes('~')` + (extra.filter ? ` && (${extra.filter})` : ""),
  fields: ["iso_3166_2", "name", "name_en", "postal", "code_hasc", "region", "type_en", "longitude", "latitude"],
  interval, precision,
  map: extra.map ?? (p => ({
    code: p.iso_3166_2,
    name: p.name,
    aliases: [p.iso_3166_2.split("-").slice(1).join("-"), p.name_en, p.code_hasc],
    label: [p.longitude, p.latitude]
  })),
  dissolve: extra.dissolve,
  unwrap: extra.unwrap
});

const worldCountries = (filter, extra = {}) => ({
    source: countries,
    filter,
    fields: ["ISO_A2_EH", "ISO_A3_EH", "ADM0_A3", "NAME", "NAME_LONG", "ADMIN", "CONTINENT", "LABEL_X", "LABEL_Y"],
    interval: 12000, precision: 0.01,
    map: p => {
      // Dependencies that share their sovereign's ISO code (for example Ashmore and Cartier Islands) keep their own A3 code.
      const primary = /^[A-Z]{2}$/.test(p.ISO_A2_EH) && (p.ADM0_A3 === p.ISO_A3_EH || !/^[A-Z]{3}$/.test(p.ISO_A3_EH));
      const code = primary ? p.ISO_A2_EH : p.ADM0_A3;
      return {
        code, name: p.NAME,
        aliases: [primary && /^[A-Z]{3}$/.test(p.ISO_A3_EH) ? p.ISO_A3_EH : null, p.ADM0_A3, p.NAME_LONG, p.ADMIN],
        group: continentCodes[p.CONTINENT] ?? "XX",
        label: [p.LABEL_X, p.LABEL_Y]
      };
    },
    ...extra
});

const atlases = {
  "world-countries": worldCountries("CONTINENT != 'Antarctica'"),
  // Oceania crosses the antimeridian, so its western-hemisphere islands are stored at longitudes above 180.
  "oceania-countries": worldCountries("CONTINENT == 'Oceania'", { unwrap: true }),
  "world-continents": {
    source: countries,
    filter: "CONTINENT != 'Antarctica' && CONTINENT != 'Seven seas (open ocean)'",
    fields: ["CONTINENT"],
    dissolve: "CONTINENT",
    interval: 15000, precision: 0.01,
    map: p => ({ code: continentCodes[p.CONTINENT], name: p.CONTINENT, aliases: [] })
  },
  "us-states": admin1("US", 6000, 0.01, {
    map: p => ({ code: p.postal, name: p.name, aliases: [p.iso_3166_2], label: [p.longitude, p.latitude] })
  }),
  "canada": admin1("CA", 9000, 0.01),
  "brazil": admin1("BR", 5000, 0.01),
  // Natural Earth lists Crimea and Sevastopol under Russia with Ukrainian ISO codes; they belong to Ukraine on the world maps.
  "russia": admin1("RU", 12000, 0.01, {
    unwrap: true,
    filter: "!iso_3166_2.startsWith('UA-')",
    // Natural Earth swaps the codes of Moscow city (ISO RU-MOW) and Moscow Oblast (ISO RU-MOS) and gives both the "Moscow" alias.
    map: p => p.name === "Moskva" ? { code: "RU-MOW", name: p.name, aliases: ["MOW", "Moscow"], label: [p.longitude, p.latitude] }
      : p.name === "Moskovskaya" ? { code: "RU-MOS", name: p.name, aliases: ["MOS", "Moscow Oblast"], label: [p.longitude, p.latitude] }
      : { code: p.iso_3166_2, name: p.name, aliases: [p.iso_3166_2.split("-").slice(1).join("-"), p.name_en, p.code_hasc], label: [p.longitude, p.latitude] }
  }),
  "spain": admin1("ES", 1500, 0.001),
  "iraq": admin1("IQ", 1200, 0.001),
  "germany": admin1("DE", 1000, 0.001),
  "belgium": admin1("BE", 350, 0.001),
  "netherlands": admin1("NL", 350, 0.001, { filter: "!iso_3166_2.startsWith('NL-BQ')" }),
  "france": admin1("FR", 1500, 0.001, {
    filter: "type_en == 'Metropolitan department'",
    dissolve: "region",
    map: p => {
      const code = frenchRegions[p.region];
      if (!code) throw new Error("No ISO 3166-2 code for French region " + p.region);
      return { code, name: p.region === "Provence-Alpes-Côte-d'Azur" ? "Provence-Alpes-Côte d'Azur" : p.region, aliases: [code.slice(3), p.region] };
    }
  }),
  "united-kingdom": {
    source: mapUnits,
    filter: "ADM0_A3 == 'GBR'",
    fields: ["GU_A3", "LABEL_X", "LABEL_Y"],
    interval: 1500, precision: 0.001,
    map: p => {
      const [code, name] = britishCountries[p.GU_A3];
      return { code, name, aliases: [code.slice(3)], label: [p.LABEL_X, p.LABEL_Y] };
    }
  }
};

async function download(name) {
  mkdirSync(cache, { recursive: true });
  const path = join(cache, name + ".geojson");
  if (existsSync(path)) return path;
  const response = await fetch(naturalEarth + name + ".geojson");
  if (!response.ok) throw new Error("Download failed for " + name + ": " + response.status);
  writeFileSync(path, Buffer.from(await response.arrayBuffer()));
  return path;
}

function round(value, precision) {
  const digits = Math.round(-Math.log10(precision));
  return Number(value.toFixed(digits));
}

function unwrapLongitude(value) {
  return value < 0 ? value + 360 : value;
}

function transformCoordinates(coordinates, transform) {
  if (typeof coordinates[0] === "number") return transform(coordinates);
  return coordinates.map(item => transformCoordinates(item, transform));
}

function clean(values, code, name) {
  const seen = new Set([code.toUpperCase(), name.toUpperCase()]);
  const result = [];
  for (const value of values) {
    if (!value || typeof value !== "string") continue;
    const trimmed = value.trim();
    if (!trimmed || trimmed === "-99" || seen.has(trimmed.toUpperCase())) continue;
    seen.add(trimmed.toUpperCase());
    result.push(trimmed);
  }
  return result;
}

mkdirSync(output, { recursive: true });
for (const [id, atlas] of Object.entries(atlases)) {
  const input = await download(atlas.source);
  const simplified = join(cache, id + ".simplified.geojson");
  const commands = ["-i", input, "-filter", atlas.filter, "-filter-fields", atlas.fields.join(",")];
  if (atlas.dissolve) commands.push("-dissolve", atlas.dissolve);
  commands.push("-simplify", "interval=" + atlas.interval, "keep-shapes", "-clean", "-o", simplified, "format=geojson", "force");
  execFileSync(mapshaper, ["-quiet", ...commands], { stdio: "inherit" });

  const collection = JSON.parse(readFileSync(simplified, "utf8"));
  const features = collection.features.filter(feature => feature.geometry).map(feature => {
    const mapped = atlas.map(feature.properties);
    const properties = { code: mapped.code, name: mapped.name, aliases: clean(mapped.aliases, mapped.code, mapped.name) };
    if (mapped.group) properties.group = mapped.group;
    if (mapped.label && mapped.label.every(Number.isFinite)) {
      properties.label = [round(atlas.unwrap ? unwrapLongitude(mapped.label[0]) : mapped.label[0], 0.01), round(mapped.label[1], 0.01)];
    }
    const geometry = {
      type: feature.geometry.type,
      coordinates: transformCoordinates(feature.geometry.coordinates, ([lon, lat]) => [
        round(atlas.unwrap ? unwrapLongitude(lon) : lon, atlas.precision), round(lat, atlas.precision)])
    };
    return { type: "Feature", properties, geometry };
  }).sort((a, b) => a.properties.code < b.properties.code ? -1 : a.properties.code > b.properties.code ? 1 : 0);

  const codes = new Set();
  for (const feature of features) {
    if (codes.has(feature.properties.code)) throw new Error(id + " has a duplicate region code " + feature.properties.code);
    codes.add(feature.properties.code);
  }
  // An alias shared by two regions, or equal to another region's code or name, would match neither; keep only unique ones.
  const uses = new Map();
  const count = value => uses.set(value.toUpperCase(), (uses.get(value.toUpperCase()) ?? 0) + 1);
  for (const feature of features) [feature.properties.code, feature.properties.name, ...feature.properties.aliases].forEach(count);
  for (const feature of features) feature.properties.aliases = feature.properties.aliases.filter(alias => uses.get(alias.toUpperCase()) === 1);
  const text = "{\"type\":\"FeatureCollection\",\"features\":[\n" + features.map(feature => JSON.stringify(feature)).join(",\n") + "\n]}\n";
  // gzip output carries no timestamp or file name, so identical geometry produces identical bytes.
  const packed = gzipSync(Buffer.from(text, "utf8"), { level: 9 });
  writeFileSync(join(output, id + ".geojson.gz"), packed);
  console.log(id.padEnd(18), String(features.length).padStart(4), "regions", String(Math.round(text.length / 1024)).padStart(5), "KiB,",
    String(Math.round(packed.length / 1024)).padStart(4), "KiB packed");
}
