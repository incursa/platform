---
workbench:
  type: spec
  workItems:
    - TASK-0001
  codeRefs:
    - /tools/prose/src/Incursa.Prose.Cli/ProseAnalyzer.cs
    - /tools/prose/src/Incursa.Prose.Cli/MarkdownMasker.cs
    - /tools/prose/src/Incursa.Prose.Cli/Readability.cs
    - /tests/Incursa.Prose.Cli.Tests/ProseAnalyzerTests.cs
    - /tests/Incursa.Prose.Cli.Tests/ReadabilityTests.cs
  path: /docs/10-product/advisory-prose-quality.md
  pathHistory: []
---

# Advisory prose quality tooling

## Summary

Provide an `incursa-prose` .NET tool that helps authors notice potentially vague or difficult prose. Results are evidence for editing, not judgments: every lexical finding names the matched text, its source location, its category, and why the category may deserve review. Readability scores are measurements with documented approximations, not quality gates.

## Goals

- Analyze Markdown and plain-text files without requiring Node.js or a network connection.
- Detect terms from the pinned `words/fillers`, `words/hedges`, `words/weasels`, and `words/buzzwords` datasets.
- Report document-level word, sentence, syllable, Flesch Reading Ease, and Flesch-Kincaid Grade Level metrics using deterministic local logic derived from `words/syllable`, `words/flesch`, and `words/flesch-kincaid`.
- Preserve file, line, and column locations and emit stable text or JSON output.
- Avoid false findings from common non-prose Markdown regions.
- Keep successful analysis advisory: findings do not change the process exit code.

## Non-goals

- Grammar correction, rewriting, sentiment analysis, or claims that a term is always wrong.
- A blocking CI policy or configurable pass/fail threshold.
- Natural-language parsing beyond deterministic phrase matching and approximate sentence/syllable counting.
- Dale-Chall scoring in the first slice; its substantially larger familiar-word corpus can be added later with separate provenance and size review.
- A reusable general-purpose value type in `Incursa.Types`; the result models are specific to this tool's diagnostics and remain platform-tooling concerns.

## Requirements

1. `incursa-prose analyze <path>...` accepts files and recursively discovers `.md`, `.markdown`, and `.txt` files in directories.
2. `--format text|json` selects deterministic human or machine output; text is the default.
3. Duplicate paths are analyzed once and output is ordered by normalized path and source position.
4. Markdown analysis excludes YAML front matter at the beginning of a file, fenced and indented code, inline code, HTML tags, image/link destinations, and reference-link definitions while retaining visible link text.
5. Phrase matching is case-insensitive, token-boundary aware, and prefers the longest phrase at a token position. A single finding may carry multiple lexical categories.
6. Each finding includes the path, one-based line and column, matched source text, canonical term, categories, and an advisory explanation.
7. Each document summary includes sentence, word, and syllable counts plus Flesch Reading Ease and Flesch-Kincaid Grade Level when text contains words.
8. Analysis exits `0` after successful processing even when findings exist. Usage, unreadable-path, or serialization failures return nonzero.
9. The NuGet tool package contains all data and attribution required for offline operation and adds no runtime package dependency.
10. Imported data and algorithms identify upstream repository, package version, full commit SHA, license, and local transformations.
