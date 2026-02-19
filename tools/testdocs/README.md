# Incursa TestDocs

`Incursa.TestDocs.Cli` and `Incursa.TestDocs.Analyzers` help teams generate and enforce structured XML documentation for tests.

## Packages

- `Incursa.TestDocs.Cli`: CLI / dotnet tool that generates documentation artifacts from test XML docs.
- `Incursa.TestDocs.Analyzers`: Roslyn analyzer package that validates required XML metadata.

## Install

```bash
dotnet tool install --global Incursa.TestDocs.Cli
dotnet add package Incursa.TestDocs.Analyzers
```

## CLI Usage

```bash
incursa-testdocs generate --repoRoot . --outDir docs/testing/generated
```

Common options:
- `--strict`
- `--minCompliance <0-1>`
- `--format markdown|json|both`

## Analyzer Rules

- `TD001`: Missing required XML doc tags.
- `TD002`: Placeholder or empty required tag values.

## Build and Pack

```powershell
dotnet pack ./tools/testdocs/TestDocs.slnx -c Release -o ./nupkgs
```

## Reference

- https://github.com/incursa/platform/blob/main/docs/testing/test-doc-schema.md