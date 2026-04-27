# Releasing

This repository is private, so releases should run on the private self-hosted GitHub Actions runner.

## Checklist

1. Choose and add package license metadata before a public NuGet release.
2. Update `Version` and `PackageReleaseNotes` in `ChartForgeX/ChartForgeX.csproj`.
3. Move relevant entries from `CHANGELOG.md` `Unreleased` into the release version.
4. Run:

```powershell
./Build.ps1 -Configuration Release
```

5. Inspect generated examples at `ChartForgeX.Examples/bin/Release/net8.0/output/index.html`.
6. Publish both package files from `ChartForgeX/bin/Release`:

```text
ChartForgeX.<version>.nupkg
ChartForgeX.<version>.snupkg
```

## Package Invariants

- The core `.nupkg` must not contain runtime NuGet dependencies.
- `README.md` and `CHANGELOG.md` must be included in the package root.
- XML documentation should be present for every target framework.
- The `.snupkg` symbol package must be generated alongside the `.nupkg`.
- A clean temporary `net8.0` console app must be able to install the freshly packed local package and render SVG, HTML, and PNG output.
