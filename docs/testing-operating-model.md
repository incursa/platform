# Testing Operating Model

Date: 2026-03-06

## Purpose

This document defines the first-pass testing operating model for the PayeWaive repository.

The goal is to keep CI and deploy safety strong without pretending the entire product is already easy to test. The model separates tests that must stay green from tests that are valuable but currently expose real known gaps.

## Baseline summary

Verified from the current repository before these changes:

- the repository already had substantial automated test coverage, but it was uneven
- commit and main/release flows ran tests, but hotfix PR flow skipped them
- coverage artifacts were produced, but not strongly surfaced as a decision-making tool
- some areas still contained placeholder, ignored, or empty test assets
- architecture verification existed mostly as a placeholder
- external/manual/sandbox integrations were not part of a routine automated lane
- some existing test projects had framework or compile debt that limited how much could be moved into a verified blocking path in one pass

For the deeper static baseline, see [testing-validation-verification-report.md](./testing-validation-verification-report.md).

## Test lanes

### Lane 1: Blocking / required

This is the CI-safe lane that gates normal delivery.

Definition:

- excludes `Explicit`
- excludes `ExtendedTest`
- excludes `KnownIssue`

Primary configuration:

- runsettings: `src/blocking.runsettings`

Primary entrypoints:

- local: `./build-local.ps1 -Mode Test`
- commit CI: `./build-commit-ci.ps1`
- main/release CI: `./build-main.ps1`
- hotfix CI: `./build-hotfix.ps1`

This lane should stay green. If a test belongs here and fails, treat it as a release-safety problem.

### Lane 2: Observational / known issue

This lane is for tests that are important enough to keep in the repo, but are currently expected to fail because they expose real product or architecture gaps.

Definition:

- includes `KnownIssue`
- excludes `Explicit`
- runs in CI as non-blocking

Primary configuration:

- runsettings: `src/observational.runsettings`
- runner script: `./run-observational-tests.ps1`
- runner target: defaults to `src/PayeWaive.All.NoDb.slnf`, but can also point at a specific `.csproj` for focused local validation

Expected behavior:

- failures are visible in console output
- failures write test results into `TestResults/observational/`
- GitHub Actions appends a short summary when `GITHUB_STEP_SUMMARY` is available
- this lane does not fail the overall workflow

## Test taxonomy

This operating model adds one new cross-cutting category:

- `KnownIssue`

Use `KnownIssue` when:

- the test is runnable in automation
- the failing behavior is real
- the failure is worth keeping visible
- fixing it would require product work or non-trivial follow-up

Do not use `KnownIssue` when:

- the test is manual, credential-gated, or not automatable yet
- the test is flaky or broken because of the harness itself
- the behavior is already correct and the test should simply be fixed

Continue using existing categories where they already apply:

- `Explicit`
- `ExtendedTest`
- `Smoke`
- any existing priority/component/environment categories

Use `Smoke` for the curated, high-signal tests that answer "did we break something important?" quickly. Smoke tests are still part of the blocking lane; the category only provides a smaller focused entrypoint.

## Current known-issue tests

See [testing-known-issues.md](./testing-known-issues.md).

## Validation snapshot

Verified locally in this pass:

- `src/tooling/PayeWaive.Architecture.Tests` with `src/blocking.runsettings`: 3 passed
- `src/modules/PayeWaive.Comms.Email.Tests` with `src/blocking.runsettings`: 13 passed
- `src/app/tests/PayeWaive.App.Web.Tests` with `src/blocking.runsettings`: 47 passed, including `HealthEndpointsTests`
- `src/tests/PayeWaive.Server.Services.Tests` with `src/blocking.runsettings`: 26 passed, including `RequireTenantPermissionAttributeTests`, `AppUrlProviderTests`, `JobServiceTests`, and `PayablesMatchEngineTests`
- `run-observational-tests.ps1` targeting `src/modules/PayeWaive.Comms.Email.Tests/PayeWaive.Comms.Email.Tests.csproj` with `src/observational.runsettings`: 2 expected failures, non-blocking exit, TRX written to `TestResults/observational/windows/observational-windows.trx`

## Legacy test cleanup status

The initial validation pass left two local pockets of excluded legacy assets. Their current disposition is:

- `src/app/tests/PayeWaive.App.Web.Tests`: older browser/NUnit scaffolding was deleted after confirming it was unreferenced, stale, and not part of the verified MSTest surface
- `src/tests/PayeWaive.Server.Services.Tests/Urls/AppUrlProviderTests.cs`: restored to the blocking lane
- `src/tests/PayeWaive.Server.Services.Tests/Job/JobServiceTests.cs`: restored to the blocking lane
- `src/tests/PayeWaive.Server.Services.Tests/PayablesMatch/PayablesMatchEngineTests.cs`: restored to the blocking lane
- `src/tests/PayeWaive.Server.Services.Tests/PayablesMatch/Utilities/InvoiceMatchModelUtility.cs`: deleted as an empty obsolete helper
- `src/tests/PayeWaive.Server.Services.Tests/PayApp/PayAppRejectTests.cs`: still excluded because it targets renamed notification namespaces and older repository/service contracts
- `src/tests/PayeWaive.Server.Services.Tests/PayApp/PayAppServiceTests.cs`: still excluded because it targets renamed notification namespaces and older repository/service contracts

## Hard-to-test areas documented for later work

The following areas were intentionally not pushed into the blocking lane in this pass:

- Box sandbox and other credential-driven external integrations
- Vista/manual ERP integration paths
- Docker-dependent tests that are currently marked ignored or require environment setup beyond normal CI
- large end-to-end business workflows that would require broader product seams or environment orchestration
- mixed-framework cleanup across the entire repo
- the remaining legacy PayApp service tests in `src/tests/PayeWaive.Server.Services.Tests`, which still target renamed notification namespaces and older repository/service contracts

## Intentionally not changed

This pass did not attempt to:

- fully standardize MSTest/NUnit/xUnit usage
- make all ignored/manual tests automatable
- add broad coverage gates or repo-wide coverage thresholds
- rewrite application architecture to satisfy the new architecture tests
- fix product defects surfaced by new known-issue tests

## How to use this model

### Add a normal blocking test

Add the test without `KnownIssue`. It will run in the default lane as long as it is not marked `Explicit` or otherwise filtered.

### Add a visible non-blocking test for a real gap

Mark the test with `KnownIssue` and document it in [testing-known-issues.md](./testing-known-issues.md).

Keep the test runnable. Do not replace it with `Ignore` unless it truly cannot run in automation.

### Run the lanes locally

Blocking lane:

```powershell
./build-local.ps1 -Mode Test
```

Observational lane:

```powershell
./run-observational-tests.ps1 -Platform windows -Configuration Debug
```

Smoke suite:

```powershell
./run-smoke-tests.ps1 -Platform windows -Configuration Debug
```

Current smoke coverage:

- web-host health endpoint availability and API-key gating
- tenant-permission allow/deny behavior
- email queue happy-path enqueue and forbidden-attachment rejection
- organization-aware absolute URL generation
- payables-match valid-path and vendor-mismatch behavior

CI shape:

- smoke suite runs first as a blocking fast-fail pre-check in commit, hotfix PR, and main/release workflows
- the broader blocking lane still runs after smoke and remains the primary required validation
- the observational lane still runs separately after the blocking lane and stays non-blocking

## Next recommended moves

1. Convert the highest-value manual/ignored integration cases into CI-safe tests only when the harness work is small and local.
2. Add one current-seam PayApp smoke test only if it can be built against today's contracts without reviving the excluded legacy PayApp harness.
3. Expand architecture verification carefully, using the observational lane first when rules expose real existing coupling.
4. Improve coverage visibility in PR review once the lane split has settled.
5. Repair the broken mixed-framework test projects so web-host and service-layer additions can be validated in the normal local/CI path.

## PayApp current seams

Current PayApp page-model coverage now covers:

- anonymous PayApp submit behavior on `PayAppAnonymousEntry`
- authenticated submit/approve behavior on `PayAppDetail`
- authenticated reject behavior on `PayAppDetail`
- authenticated resend-email forwarding on `PayAppDetail`
- authenticated report-generation behavior on `PayAppDetail`

At this point, additional `PayAppDetail` page-model expansion has diminishing returns. The remaining handler-level seams are mostly pass-through calls into the UI engine, so more page-model tests would add count without adding much confidence.

That next meaningful modern seam has now been opened with `PayeWaive.App.Server.Tests` and a small `PayAppDetailUiEngine` slice covering:

- `ResendPayAppRequestEmailAsync`, which branches on pay-app status and selects either signature-request email behavior or standard resend behavior
- `ApprovePaymentApplicationAsync`, which applies compliance-gate behavior before transitioning state

Current harness shape for that seam:

- instantiate `PayAppDetailUiEngine` directly
- fake `IPayAppService`
- fake `IComplianceGate`
- fake `IUrlResolver`
- fake `ITenantContextProvider`, `IDocumentService`, `IDocumentsRepository`, `IVendorService`, `IOrganizationService`, and `IHub`
- use a focused `PayeWaive.App.Server.Tests` MSTest project plus `NSubstitute`, matching the repository's existing test style

Currently validated scenarios for that seam:

1. `ResendPayAppRequestEmailAsync` uses signer-request email behavior when pay-app status is `Approved`.
2. `ResendPayAppRequestEmailAsync` uses standard resend behavior for non-approved statuses such as `Submitted`.
3. `ApprovePaymentApplicationAsync` blocks approval when the compliance gate denies the action.
4. `ApprovePaymentApplicationAsync` approves and triggers signature-request email when the compliance gate allows the action.

Current PayApp smoke coverage is intentionally limited to a single server-level case:

- `PayAppDetailUiEngineTests.ApprovePaymentApplicationAsync_WhenComplianceGateAllows_ApprovesAndRequestsSignatures`

That test was promoted because it is the highest-signal current PayApp seam that is still fast and deterministic. It validates the real approval transition plus signature-request side effect without carrying the extra page-model or report-generation fixture complexity of the web-layer tests.

The broader legacy PayApp service tests remain out of scope for this slice. The next meaningful PayApp confidence gain beyond this point is likely a deeper modern service/UI-engine seam around richer state transitions or authorization-sensitive flows, not more thin page-model pass-through tests.

Smoke evaluation:

- no PayApp test is being added to smoke in this pass
- the best current candidate is `OnPostSubmitPayAppAsync_WhenSubmissionAllowed_ApprovesPayApp` in `src/app/tests/PayeWaive.App.Web.Tests/Tests/PayAppDetailSubmissionTests.cs`
- it is fast and meaningful, but it still depends on a relatively handcrafted view-model fixture; it should only move into smoke after another cycle of proving stability
