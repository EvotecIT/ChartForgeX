# Contributing

Thanks for helping make ChartForgeX better.

## Local Quality Loop

Run the full repository quality loop before opening a pull request:

```powershell
./Build.ps1 -Configuration Release
```

That command restores packages, builds all projects, runs the smoke suite through `dotnet test`, regenerates example outputs, packs the library, verifies the package has no runtime NuGet dependencies, and verifies the symbol package is created.

For a faster test-only loop:

```powershell
dotnet test .\ChartForgeX.sln -c Release
```

## Change Expectations

- Add or update smoke tests for every new renderer behavior.
- Add an example when a change affects visible chart output.
- Keep the core package free of runtime package dependencies.
- Keep project package references private unless they are part of the core package contract.
- Keep SVG/HTML output static and self-contained.
- Preserve `net472`, `netstandard2.0`, `net8.0`, and `net10.0` support.
- Do not suppress warnings with `NoWarn`.

## CI Runner

The GitHub Actions workflow is intentionally configured for private repositories. It requires a self-hosted runner with both `self-hosted` and `private` labels.
