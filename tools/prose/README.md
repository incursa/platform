---
workbench:
  type: guide
  workItems:
    - TASK-0001
  codeRefs:
    - /tools/prose/src/Incursa.Prose.Cli/Program.cs
    - /tools/prose/src/Incursa.Prose.Cli/ProseAnalyzer.cs
  path: /tools/prose/README.md
  pathHistory: []
---

# Incursa Prose

`Incursa.Prose.Cli` is an advisory .NET tool for reviewing Markdown and plain-text prose. It reports explainable filler, hedge, weasel, and buzzword matches alongside approximate readability metrics. A finding is a prompt to review language in context, not a claim that the language is wrong.

## Install

```bash
dotnet tool install --global Incursa.Prose.Cli
```

## Use

Analyze files or directories recursively:

```bash
incursa-prose analyze README.md docs/
incursa-prose analyze --format json docs/ > prose-report.json
```

Directory discovery includes `.md`, `.markdown`, and `.txt` files and skips `.git`, `.svn`, `artifacts`, `bin`, `node_modules`, and `obj` directories. Duplicate paths are analyzed once.

Successful analysis returns exit code `0` even when findings exist. Invalid arguments, missing paths, unsupported explicit file types, and unreadable files return `1`.

## Output

Each lexical finding includes:

- a one-based line and column;
- the exact matched source text and canonical term;
- one or more categories;
- category-specific, advisory editing guidance.

Each document also reports sentence, word, and syllable counts, Flesch Reading Ease, and Flesch-Kincaid Grade Level. The JSON format has a versioned top-level envelope for stable machine consumption.

## Markdown behavior

The analyzer preserves visible prose and ignores common non-prose regions:

- initial YAML front matter;
- fenced and indented code;
- inline code;
- link destinations and raw URLs while retaining visible link text;
- reference-link definitions and HTML tags.

This is a deliberately small deterministic masker, not a complete CommonMark parser. Complex nested constructs may require manual review.

## Data and algorithm provenance

The tool embeds normalized data from the MIT-licensed `words/fillers`, `words/hedges`, `words/weasels`, and `words/buzzwords` repositories. Syllable estimates and readability formulas are derived from the MIT-licensed `words/syllable`, `words/flesch`, and `words/flesch-kincaid` implementations. Exact versions, commit SHAs, and transformations are recorded in [`provenance.json`](provenance.json), with license text in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

No Node.js runtime, npm package, network access, telemetry, or external service is used.

## Limitations

- List membership is context-dependent and can produce false positives, especially for ordinary technical terms in the buzzword list.
- English syllables and sentence boundaries are approximations; scores should be compared as trends, not treated as precise grade certifications.
- Dale-Chall scoring is intentionally deferred because its familiar-word corpus is much larger than this first focused slice.
- The first release exposes a CLI contract only. A reusable public analysis API can be evaluated after real usage clarifies stability requirements.
