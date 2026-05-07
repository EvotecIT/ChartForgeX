# Releasing

This repository is private, so releases should run on the private self-hosted GitHub Actions runner.

## Checklist

1. Choose and add package license metadata before a public NuGet release.
2. Update `Version` and package release metadata in `ChartForgeX/ChartForgeX.csproj`, `ChartForgeX.Interactivity/ChartForgeX.Interactivity.csproj`, and `ChartForgeX.Interactivity.Html/ChartForgeX.Interactivity.Html.csproj`.
3. Move relevant entries from `CHANGELOG.md` `Unreleased` into the release version.
4. Run:

```powershell
./Build.ps1 -Configuration Release
```

5. Inspect generated examples at `ChartForgeX.Examples/bin/Release/net8.0/output/index.html`.
6. Publish the package files from `artifacts/packages/Release`:

```text
ChartForgeX.<version>.nupkg
ChartForgeX.<version>.snupkg
ChartForgeX.Interactivity.<version>.nupkg
ChartForgeX.Interactivity.<version>.snupkg
ChartForgeX.Interactivity.Html.<version>.nupkg
ChartForgeX.Interactivity.Html.<version>.snupkg
```

## Build Timeouts

`Build.ps1` time-limits every `dotnet` validation step so a stuck restore, build, test, example, pack, or package-consumer run fails with a named timeout instead of hanging indefinitely.

- `-DotNetCommandTimeoutSeconds` controls the general restore/build/test/example/pack timeout. The default is 900 seconds.
- `-PackageConsumerTimeoutSeconds` controls the final temporary-console-app package smoke run. The default is 180 seconds.

If a private runner is under heavy load, rerun with a larger timeout rather than skipping package or visual validation:

```powershell
./Build.ps1 -Configuration Release -DotNetCommandTimeoutSeconds 1800 -PackageConsumerTimeoutSeconds 300
```

## Package Invariants

- `ChartForgeX` and `ChartForgeX.Interactivity` must not contain runtime NuGet dependencies.
- `ChartForgeX.Interactivity.Html` must depend on the matching local `ChartForgeX` and `ChartForgeX.Interactivity` packages.
- `README.md` and `CHANGELOG.md` must be included in the package root.
- XML documentation should be present for every target framework.
- A `.snupkg` symbol package must be generated alongside every `.nupkg`.
- A clean temporary `net8.0` console app must be able to install the freshly packed HTML interactivity package and render SVG, HTML, PNG, and interactive HTML output.

## Visual Baseline

Release builds validate `ChartForgeX.Examples/visual-baseline.json` against the generated SVG/PNG comparison manifest. The baseline tracks SVG dimensions, high-DPI PNG output scale, minimum SVG visual-node counts, minimum PNG visible-pixel/color counts, clipped SVG text, near-edge SVG text, and PNG edge pressure.

Before refreshing the baseline, inspect:

- `ChartForgeX.Examples/bin/Release/net8.0/output/quality-dashboard.html`
- `ChartForgeX.Examples/bin/Release/net8.0/output/svg-png-comparison.html`
- `ChartForgeX.Examples/bin/Release/net8.0/output/svg-png-comparison.json`

If a visual change is intentional, refresh the baseline with:

```powershell
./Build.ps1 -Configuration Release -UpdateVisualBaseline
```

Commit the refreshed baseline only with the renderer or example change that justifies it. Do not refresh the baseline to hide warnings, clipped text, edge pressure, or accidental PNG/SVG parity regressions.

## Topology Visual Coverage

Topology and geographic topology examples are not part of `visual-baseline.json` yet. They are release-gated through `ChartForgeX.Examples/bin/Release/net8.0/output/topology-demo/visual-capability-manifest.json`, required SVG metadata fragments, generated SVG/HTML/PNG artifacts, and PNG size checks.

Keep that split until dense routing and geographic layout polish settle enough for numeric visual baselines. When promoting topology artifacts into the regular baseline, start with the manifest's `baselineCandidates` list and update `Build.ps1`, `ChartForgeX.Examples/visual-baseline.json`, and this release guide in the same PR.
