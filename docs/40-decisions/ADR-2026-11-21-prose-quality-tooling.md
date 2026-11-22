---
workbench:
  type: adr
  workItems:
    - TASK-0001
  codeRefs:
    - /tools/prose/src/Incursa.Prose.Cli/Incursa.Prose.Cli.csproj
    - /tools/prose/provenance.json
  path: /docs/40-decisions/ADR-2026-11-21-prose-quality-tooling.md
  pathHistory: []
---

# Place prose quality analysis in platform tooling

## Status

Accepted for implementation on 2026-11-21.

## Context

Platform already owns shared, repository-agnostic tooling under `tools/`, including the TestDocs generator/analyzer. The core `src/` packages are reserved for provider-neutral runtime capabilities. TestDocs validates structured test XML metadata and generates test inventories; broad prose analysis would dilute that tool's purpose and force unrelated users to carry its Roslyn dependency.

The `words` organization publishes useful English lexical datasets and readability algorithms as JavaScript packages. Pulling those packages at runtime would add Node.js and npm supply-chain requirements to a .NET repository. The candidate datasets and algorithms are deterministic and small enough to port. All selected upstream repositories are MIT-licensed at the pinned commits.

The sibling `types` repository currently contains unrelated active ShortCode work and owns reusable cross-library primitives. Prose findings and metrics are domain-specific output contracts for this tool, not general value objects, so no `types` change is required.

## Decision

Create a focused `Incursa.Prose.Cli` .NET tool under `tools/prose`, exposed as `incursa-prose`. Keep its analysis models internal in the first release. Import normalized copies of the filler, hedge, weasel, and buzzword datasets and port the minimum syllable/Flesch/Flesch-Kincaid logic needed for deterministic document metrics.

Treat lexical matches and readability scores as advisory. The CLI returns success when analysis completes, regardless of findings. It emits exact locations and explanations so an author can decide whether the language is appropriate in context.

Pin every imported repository to a full commit SHA in a machine-readable provenance file, ship the upstream MIT notice, and test representative upstream formula cases. Do not add a Node runtime or JavaScript package dependency.

## Consequences

- The tool can run offline in .NET-only build and documentation workflows.
- A separate command and package keep TestDocs focused and avoid its Roslyn dependency for prose-only users.
- Matching is transparent and reproducible, but list membership can produce context-dependent false positives; documentation must reinforce that findings are prompts, not verdicts.
- Readability metrics inherit the limitations of heuristic English syllable and sentence counting.
- Dale-Chall is deferred to avoid importing a much larger word corpus before usage justifies it.
- A future public analysis API may require reviewing whether any promoted general-purpose identifiers or value types belong in `Incursa.Types`; this PR does not create that coupling.
