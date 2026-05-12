# Topology Icon Assets

This folder contains generated, provenance-tracked topology icon packs that are intentionally separate from the ChartForgeX core runtime.

Current curated packs:

- `chartforgex-ad-network-premium` - 51 first-party AD/network icons for forests, domains, FSMO roles, DCs, RODCs, sites, site links, subnets, bridgeheads, users, groups, computers, service accounts, contacts, containers, printers, shares, DNS, PKI, schema, policies, and security objects
- `backup` - 14 original ChartForgeX diagram icons for backup servers, proxies, repositories, scale-out storage, hardened storage, jobs, labs, consoles, recovery, and WAN acceleration
- `network-security` - 16 original ChartForgeX diagram icons for firewall, switching, wireless, management, analytics, VPN, SD-WAN, identity, WAF, mail, endpoint, sandbox, SIEM, cloud, and fabric roles
- `cloud-productivity` - 18 original ChartForgeX diagram icons for cloud productivity, tenant, identity, security, messaging, collaboration, storage, governance, endpoint, low-code, subscription, networking, and SIEM roles
- `network-infrastructure` - 14 original ChartForgeX device-like icons for rack switches, core switches, L3 switches, PoE switches, routers, wireless APs, firewall appliances, load balancers, VPN gateways, patch panels, fiber uplinks, and SFP transceivers
- `chartforgex-geo-incidents-world-events` - 45 first-party icons for maps, routes, hazards, incidents, response services, intelligence feeds, monitoring, and logistics
- `chartforgex-charts-analytics` - 38 first-party icons for charts, analytics, KPI badges, dashboards, reporting, thresholds, anomalies, and drilldowns
- `chartforgex-topology-network` - 38 first-party icons for generic topology, network devices, connectivity, links, services, datacenters, and edge infrastructure
- `chartforgex-security-risk-certificates` - 61 first-party icons for identity, access, certificates, trust, findings, incidents, risk, compliance, evidence, and forensics
- `chartforgex-data-ownership-intelligence` - 80 first-party icons for datasets, ownership, entities, correlation, evidence, reporting, integrations, and governance
- `chartforgex-people-org-workflow` - 71 first-party icons for people, roles, organization structure, lifecycle operations, goals, approvals, workflows, collaboration, and HR data
- `chartforgex-identity-directory` - 38 first-party icons for directory structures, principals, infrastructure, directory services, access, authentication, certificates, and protocol flows
- `chartforgex-microsoft-365-collaboration` - 72 first-party generic Microsoft 365-style icons for tenants, messaging, collaboration, productivity, compliance, integrations, and lifecycle governance

Imported vendor packs:

- `tabler-icons/tabler-icons-outline` - 5,093 sanitized Tabler Icons outline SVGs imported from `@tabler/icons` 3.44.0 under the MIT license. This pack is kept separate from first-party ChartForgeX artwork so consumers can use Tabler immediately while native ChartForgeX packs continue to improve deliberately.

After editing `svg/*.svg`, refresh the derived PNG previews and validation report with:

```powershell
dotnet run --project .\ChartForgeX.Tools.IconImport\ChartForgeX.Tools.IconImport.csproj -c Release -- --refresh-pack .\assets\topology-icons\chartforgex-ad-network-premium --preview-size 128
```

Generated vendor or branded packs should include:

- a portable `manifest.json` file produced from `TopologyIconPackJson`
- editable/renderable `svg/*.svg` artwork files referenced by manifest `artwork.svgPath`
- optional `previews/*.png` thumbnails for picker UIs and documentation
- source metadata in every manifest: source URL, source revision, license, and per-icon source path
- an import report when a whole external collection is ingested

Use vendor or product names only when the pack is genuinely branded or imported from a licensed/provenance-tracked vendor source. Generic first-party artwork should stay under neutral pack ids such as `backup`, `network-security`, or `cloud-productivity`.

The conversion tooling lives under `ChartForgeX.Tools.IconImport` and `tools/topology-icons`. It may use tooling-only dependencies for SVG-to-PNG previews, but those packages are not runtime dependencies of `ChartForgeX`.

See `docs/topology-icon-packs.md` for the reusable pack layout, importer command, and review checklist for future vendor/community packs.
