# Test Documentation

This folder describes the XML doc schema for MSTest, xUnit, and NUnit tests and hosts generated documentation.

The `Test Documentation` GitHub workflow is intended as a PR/manual documentation aid. It publishes generated artifacts and PR summaries without acting as a main-branch merge gate while the broader test inventory continues to evolve.

Workbench quality integration uses:

- Authored intent contract: `docs/30-contracts/test-gate.contract.yaml`
- Smoke lane: `scripts/quality/run-smoke-tests.ps1`
- Blocking lane: `scripts/quality/run-blocking-tests.ps1`
- Observational lane: `scripts/quality/run-observational-tests.ps1`
- Advisory evidence runner: `scripts/quality/run-advisory-quality-tests.ps1`
- Full quality workflow: `scripts/quality/run-quality-evidence.ps1`
- Normalized Workbench outputs: `artifacts/quality/testing/`
- Operating model and happy path: `docs/testing-operating-model.md`
- Known-issue register: `docs/testing-known-issues.md`

- Schema: `docs/testing/test-doc-schema.md`
- Generated docs: `docs/testing/generated/README.md`

## How to document new tests
1. Add XML doc tags to each MSTest method (`summary`, `intent`, `scenario`, `behavior`).
2. Use optional tags only when they are already supported by the test name/body.
3. Run the generator locally to refresh `docs/testing/generated/`.

Optional: install the analyzer package `Incursa.TestDocs.Analyzers` to get warnings for missing/placeholder metadata.

Local generation:

```powershell
pwsh ./tools/testdocs/scripts/Invoke-TestDocs.ps1
```
