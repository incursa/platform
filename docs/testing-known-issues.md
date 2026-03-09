# Testing Known Issues

Date: 2026-03-08

No runnable `Category=KnownIssue` tests are currently authored in this repository.

That is intentional for this first pass:

- the observational lane now exists
- the artifact path is stable at `artifacts/codex/test-results/observational/`
- the taxonomy is documented so future known-issue tests can be added without redesigning the workflow

When a new known-issue test is added:

- tag it with `Category=KnownIssue`
- keep it runnable in automation
- document the gap and expected behavior here
- do not use `KnownIssue` for flaky, manual-only, or secret-blocked tests
