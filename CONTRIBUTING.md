# Contributing to sql-migration-lint

Thanks for your interest in contributing. This guide covers how to build, test, and submit changes.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (the projects target `net10.0`; CI uses `10.0.x`)
- Git

No other tools are required. There is no `global.json` and no `requirements` file; NuGet packages are restored automatically.

## Build

```bash
dotnet build sql-migration-lint.csproj
```

Run the CLI from source:

```bash
dotnet run --project sql-migration-lint.csproj -- <path> [--fail-on Error] [--format text|json|github]
```

## Test

```bash
dotnet test tests/SqlMigrationLint.Tests/SqlMigrationLint.Tests.csproj
```

Run a subset with a filter, for example:

```bash
dotnet test tests/SqlMigrationLint.Tests/SqlMigrationLint.Tests.csproj --filter "FullyQualifiedName~MissingWhereRuleTests"
```

Before opening a PR, run the build and the tests in `Release` configuration, as CI does:

```bash
dotnet build tests/SqlMigrationLint.Tests/SqlMigrationLint.Tests.csproj -c Release
dotnet test tests/SqlMigrationLint.Tests/SqlMigrationLint.Tests.csproj -c Release
```

## Code style

- There is no `.editorconfig` and no custom analyzer set, and warnings are **not** treated as errors (`TreatWarningsAsErrors` is not enabled). Still, keep the build free of new warnings.
- Match the style of the surrounding code. Use `dotnet format` if you need to format a file.
- `Nullable` and `ImplicitUsings` are enabled, and `LangVersion` is `latest`.
- `GenerateDocumentationFile` is on: add XML doc comments to public members.
- One public type per file; the file name matches the type name. The namespace is the flat `SqlMigrationLint`.
- Rules are stateless singletons exposed via `public static readonly Instance`.
- Tests use xUnit. Name test classes `<Subject>Tests` and methods `Method_Scenario_Expected`.
- Read [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) before changing parsing or rule registration.
- `src/Program.cs` duplicates the scanning logic in `MigrationLinter`. If you change one, update the other.

## Submitting a pull request

1. Create a branch from an up-to-date `main`. Do not commit directly to `main`.
2. Keep each PR to **one logical change**. Split unrelated fixes and refactors into separate PRs.
3. Add or update tests for behavior changes.
4. Make sure the build and tests pass locally (see above).
5. Open a PR against `main`. In the description, explain what changed and why, and how you verified it. Link the related issue if there is one.
6. CI (`.github/workflows/ci.yml`) runs restore, build, and test on every PR to `main`. The PR must be green before it is merged.

Use short, imperative commit subjects. The history uses prefixes such as `feat:`, `fix:`, and `chore:`.

## Reporting bugs

Open an issue at <https://github.com/Sarmkadan/sql-migration-lint/issues> and include:

- A short description of the problem and what you expected to happen
- Steps to reproduce, ideally with a minimal migration file that triggers the issue
- The exact command you ran and its output
- The tool version (the `<Version>` in `sql-migration-lint.csproj`, or the commit SHA if you built from source) and your OS
- The .NET SDK version (`dotnet --version`)

## License

By contributing, you agree that your contributions are licensed under the MIT License, the same license as the project.
