> Detail for `instruction.md` §13. Load when you change the SDK, target framework, packages,
> build files or deployment.

# 13. .NET, Build and Deployment

## 13.1 .NET

* Domain, Application, Infrastructure, Server, Iac and the tests target `net10.0`. `Shared` and the Blazor/MAUI clients stay on `net8.0` (Bit 8). `global.json` selects SDK 10.0.100 with `rollForward: latestFeature`. Building the net8 WASM client needs the `wasm-tools-net8` workload.
* .NET 8 support ends on 2026-11-10. Plan 004 patches .NET 8, and plan 016 moves the server-side
  projects to .NET 10. Do not change the target framework outside those plans.
* Shared build settings are in `src/Directory.Build.props` (C# 12, nullable enabled).
* Nullable reference types are on. Do not add new nullable warnings. The build has 135 warnings
  now (2026-10-27); do not fix them outside the task.
* Use current, supported APIs for the target framework.

## 13.2 Packages — Central Package Management

The solution uses NuGet **Central Package Management** (CPM). Every package version is in
`Directory.Packages.props` at the `Logiwall-API/` root. It applies to every project under it,
including the future `tests/` projects.

* Write `<PackageReference Include="X" />` in a `*.csproj`. Never put a `Version` on it; NuGet
  fails the restore with NU1008 if you do.
* Add the version as `<PackageVersion Include="X" Version="..." />` in `Directory.Packages.props`,
  in the `ItemGroup` whose `Label` fits. `dotnet add package X` does both steps for you.
* Keep other attributes (`PrivateAssets`, `IncludeAssets`, `ExcludeAssets`) on the
  `PackageReference` in the project.
* To change a version, change it only in `Directory.Packages.props`. Every project that uses the
  package gets the new version.
* If one project needs a different version, use `VersionOverride="..."` on its `PackageReference`,
  and write a comment that says why. Do not use it to avoid an upgrade.
* To remove a package, delete its `PackageReference`. If no other project uses it, delete its
  `PackageVersion` too.
* Transitive pinning (`CentralPackageTransitivePinningEnabled`) is off. To fix a vulnerable
  transitive package, add a `PackageVersion` and a direct `PackageReference` (plan 004).

Other package rules:

* Do not upgrade unrelated packages (`instruction.md` §16).
* Do not add a package when the framework already does the job.
* Plan 015 removes unused packages (OData, InMemory, SQLite, ...). Do not add new uses of them.

## 13.3 Build

* Build with `dotnet build src/Logiwall.Server/Logiwall.Server.csproj`.
* Do not build `Logiwall.sln` or `Logiwall.Web.slnf`. They include `Logiwall.Client.Maui`,
  which needs MAUI workloads.

## 13.4 Deployment

* `src/Logiwall.Iac/` is a Pulumi stack for Azure: App Service, Azure SQL, Key Vault and
  Application Insights. See `src/Logiwall.Iac/Readme.md`.
* Nothing applies migrations automatically. Apply them as a separate deployment step.
* There is no Dockerfile and no CI yet. Plan 005 adds GitHub Actions CI.
