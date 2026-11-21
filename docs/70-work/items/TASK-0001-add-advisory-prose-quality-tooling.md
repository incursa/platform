---
id: TASK-0001
type: task
status: in-progress
priority: high
owner: platform
created: 2026-11-21
updated: null
tags:
  - tooling
  - documentation
  - prose-quality
related:
  specs:
    - /docs/10-product/advisory-prose-quality.md
  adrs:
    - /docs/40-decisions/ADR-2026-11-21-prose-quality-tooling.md
  files:
    - /tools/prose/README.md
  prs: []
  issues: []
  branches:
    - codex/task-0001-advisory-prose-quality
title: Add advisory prose quality tooling
---

# TASK-0001 - Add advisory prose quality tooling

## Summary

Ship a dependency-free .NET tool that reports explainable, advisory prose findings and readability metrics for Markdown and plain-text documents. The implementation ports pinned MIT-licensed datasets and deterministic algorithms from the `words` organization without adding a Node runtime dependency.

## Acceptance criteria

- Detect filler, hedge, weasel, and buzzword terms with source locations and category-specific explanations.
- Report word, sentence, syllable, Flesch Reading Ease, and Flesch-Kincaid Grade Level metrics.
- Ignore non-prose Markdown regions such as front matter, code fences, inline code, and link destinations.
- Support human-readable and JSON output while keeping findings advisory (exit code zero when analysis succeeds).
- Pin imported source commits, retain MIT notices, and document transformations and limitations.
- Add focused unit and CLI tests plus user-facing installation and usage documentation.
- Run build, test, format/analyzer, package, scope, dependency, license, and security validation appropriate to the change.
- Push the dedicated branch and open a draft PR for review; do not merge it.

## Validation notes

- The new tool and its tests build with warnings treated as errors, and all focused tests pass.
- The packed tool installs and runs from a local NuGet source; its package includes the README, provenance manifest, and third-party notice and declares no runtime dependencies.
- NuGet vulnerability checks report no vulnerable packages for the new tool or its test project after pinning SourceLink 10.0.303.
- The full solution build is currently blocked on pre-existing analyzer/XML-documentation failures in TestDocs, Observability.Analyzers, Storage, and HealthProbe. With normal auditing enabled, existing projects also fail restore on repository-wide vulnerable package pins. The feature project is not among either failure set.
- Workbench strict validation reaches the legacy documentation corpus but reports pre-existing broken links. The generated validation artifact is retained under `artifacts/codex/` and is not committed.
