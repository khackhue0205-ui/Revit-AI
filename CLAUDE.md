# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Autodesk Revit add-in targeting Revit versions 2023–2027. Built on .NET Framework 4.8 (Revit 2023–2024) and .NET 10 (Revit 2025–2027) depending on the selected solution configuration.

## Build Commands

**Compile the add-in (all supported Revit versions):**
```shell
cd build; dotnet run
```

**Create MSI installer and Autodesk `.bundle` package:**
```shell
cd build; dotnet run -- pack
```

**Build a specific Revit version in the IDE:**
Select solution configuration `Debug.R27` (for Revit 2027) or `Release.R27` before building. The `R{xx}` suffix maps directly to the Revit year version.

## Solution Structure

| Folder | Description |
|--------|-------------|
| `BTVN1 Application/` | Main Revit add-in source code |
| `build/` | ModularPipelines build automation (net10.0 console app) |
| `install/` | WiX-based MSI installer, called by the build pipeline |
| `output/` | Generated artifacts (`.bundle` zip, MSI) — not committed |

## Architecture

### Add-in (`BTVN1 Application/`)

Follows MVVM with Revit-specific entry points:

- **`Application.cs`** — `ExternalApplication` entry point. Initializes Serilog logging and creates the Revit ribbon panel with push buttons.
- **`Commands/`** — `ExternalCommand` subclasses registered as ribbon buttons in `Application.cs`. Each command uses `[Transaction(TransactionMode.Manual)]` and implements `Execute()`.
- **`Models/`** — Data and DTOs.
- **`ViewModels/`** — Bindable properties and commands for WPF views.
- **`Views/`** — WPF windows and user controls.
- **`Resources/`** — Icons (16px and 32px ribbon images), localisation files.
- **`Utils/`** — Shared extensions and helpers.

Key dependencies:
- `Nice3point.Revit.Toolkit` — base classes `ExternalApplication`, `ExternalCommand`, ribbon helpers
- `Nice3point.Revit.Extensions` — Revit API extension methods
- `Serilog` — logging (debug sink; production sinks can be added)
- `ILRepack` — merges dependencies into the output DLL at publish time

### Build Pipeline (`build/`)

Uses [ModularPipelines](https://github.com/thomhurst/ModularPipelines). Modules run in dependency order:

1. `ResolveVersioningModule` — resolves version from git tags / `appsettings.json`
2. `ResolveConfigurationsModule` — reads solution configurations (e.g. `Debug.R23`…`Release.R27`)
3. `CompileProjectModule` — runs `dotnet build` for each configuration
4. `CreateBundleModule` — assembles `PackageContents.xml` manifest and zips a `.bundle`
5. `CreateInstallerModule` — invokes the `install` project to produce the MSI

Configuration lives in `build/appsettings.json` (output directory, vendor metadata).

## Multi-Version Targeting

The project SDK `Nice3point.Revit.Sdk` automatically sets `TargetFramework` from the `RevitVersion` extracted out of the solution configuration name.

Revit API NuGet packages use a wildcard version tied to the build configuration:
```xml
<PackageReference Include="Nice3point.Revit.Api.RevitAPI" Version="$(RevitVersion).*"/>
```

Use conditional compilation to handle API differences across versions:
```csharp
#if REVIT2023_OR_GREATER
    // API available since 2023
#else
    // fallback for 2022 and earlier
#endif
```

Available constants follow the pattern `REVIT{year}` and `REVIT{year}_OR_GREATER`.

> **Important:** Edit `.csproj` configuration blocks manually — IDEs (including Rider) can corrupt the multi-configuration `<Configurations>` property.
