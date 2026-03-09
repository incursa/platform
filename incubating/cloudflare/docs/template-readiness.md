---
workbench:
  type: doc
  workItems: []
  codeRefs: []
  pathHistory:
    - "C:/docs/template-readiness.md"
  path: /docs/template-readiness.md
---

# Template Readiness Report

## Scope
Repository scanned at `C:\src\bravellian\repomanager` to assess reuse readiness as a private Bravellian library template.

## File Tree (top 2-3 levels)
```text
.
├── .codex/
│   └── skills/
│       ├── dotnet-build-diagnostics/
│       ├── dotnet-format-analyzers/
│       ├── dotnet-repo-discovery/
│       ├── dotnet-symbol-grep-recipes/
│       └── dotnet-test-triage/
├── .github/
│   └── workflows/
│       ├── ci.yml
│       ├── quality.yml
│       └── release.yml
├── .opencode/
│   ├── agents/
│   ├── .gitignore
│   └── package.json
├── src/
│   └── Incursa.Integrations.Cloudflare/
│       ├── CloudflareRoot.cs
│       └── Incursa.Integrations.Cloudflare.csproj
├── tests/
│   └── Incursa.Integrations.Cloudflare.Tests/
│       ├── Incursa.Integrations.Cloudflare.Tests.csproj
│       └── SanityTests.cs
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .pre-commit-config.yaml
├── AGENTS.md
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── global.json
├── Incursa.Integrations.Cloudflare.slnx
├── README.md
├── NOTICE.md
└── *.props / scripts / metadata files
```

## Required Files Present Checklist
- [x] `global.json`
- [x] `Directory.Build.props`
- [x] `Directory.Build.targets`
- [x] `Directory.Packages.props`
- [x] `*.props` present (`analyzers.props`, `common.props`, `metadata.props`, `non-tests.props`, `strongvalidation.props`, `tests.props`)
- [x] `.editorconfig`
- [x] pre-commit config (`.pre-commit-config.yaml`)
- [x] workflows (`.github/workflows/*.yml`)
- [x] `AGENTS.md`
- [x] `README.md`
- [ ] `NOTICE` (exact filename)
- [x] `NOTICE.md` (present, likely intended equivalent)

## Placeholder Locations
### Incursa.Integrations.Cloudflare / template naming
- `Incursa.Integrations.Cloudflare.slnx`
- `src/Incursa.Integrations.Cloudflare/Incursa.Integrations.Cloudflare.csproj`
- `tests/Incursa.Integrations.Cloudflare.Tests/Incursa.Integrations.Cloudflare.Tests.csproj`
- `src/Incursa.Integrations.Cloudflare/CloudflareRoot.cs` (namespace `Incursa.Integrations.Cloudflare`)
- `tests/Incursa.Integrations.Cloudflare.Tests/SanityTests.cs` (namespace + type usage)
- `README.md` build/test/pack commands
- `llms.txt` solution and command examples
- `Init-Repo.ps1` (`$old = "Incursa.Integrations.Cloudflare"` used for rename flow)

### CloudflareRoot placeholder
- `src/Incursa.Integrations.Cloudflare/CloudflareRoot.cs`
- `tests/Incursa.Integrations.Cloudflare.Tests/SanityTests.cs`

### TODOs / obvious incomplete markers
- No `TODO`/`FIXME` markers found in template project/workflow files.

## Obvious Inconsistencies
- Skill docs reference a non-existent solution path `src/Bravellian.slnx`:
  - `.codex/skills/dotnet-format-analyzers/SKILL.md`
  - `.codex/skills/dotnet-test-triage/SKILL.md`
- `NOTICE` vs `NOTICE.md` naming mismatch relative to strict checklist expectations.

## Small Safe Fixes Applied
- Fixed broken default target resolution in format/analyzer helper scripts so they now auto-detect repo-root `.slnx`/`.sln` when `DOTNET_FORMAT_TARGET` is not set:
  - `.codex/skills/dotnet-format-analyzers/scripts/run-format-analyzers.ps1`
  - `.codex/skills/dotnet-format-analyzers/scripts/run-format-analyzers.sh`
- Also corrected shell argument display handling in the bash script to avoid parse issues.
