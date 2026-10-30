# Assignment: Invoice Approval Workflow

## Implementation summary

The invoice approval workflow is implemented in the Domain layer:

-   `backend/src/InvoiceApp.Domain/ApprovalService.cs`
-   `backend/src/InvoiceApp.Domain/Invoice.cs`

Supporting tests and repository tracking are in:

-   `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
-   `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`

The Playwright page object and end-to-end test are in:

-   `e2e/pages/InvoiceApprovalPage.ts`
-   `e2e/tests/manager-over-limit-rejection.spec.ts`

The existing session-based authentication and API authorization model
has been retained.

## Business rules

-   A Manager may finally approve invoices up to and including 50,000.
-   A Director may finally approve invoices above 50,000 and up to and
    including 500,000.
-   Invoices above 500,000 require a Manager first-stage approval
    followed by a different Director's final approval.
-   The same user cannot satisfy both approval stages. Username
    comparison is case-insensitive.
-   The Director's second-stage approval requires the Manager stage to
    exist first.
-   Rejected attempts must preserve the invoice's state, relevant
    decision metadata, and prior approval-stage records.

The Manager-first sequence for invoices above 500,000 is the agreed
interpretation used by this implementation.

## Final validation evidence

### Unit tests

Latest recorded command:

``` powershell
dotnet test .\backend\InvoiceApp.sln --nologo
```

-   Passed: 79
-   Failed: 0
-   Skipped: 0
-   Total: 79
-   Exit code: 0

Console evidence:
`specs/001-approval-workflow/unit-tests-final-console.txt`

The two additional decimal-boundary tests verify that amounts of
50,000.01 and 500,000.01 are rejected for the respective final-approval
attempts without mutating the invoice or calling the repository update.
These tests and the updated unit-test evidence were committed in
`ec1f172` (`test: cover decimal invoice approval boundaries`).

### Playwright end-to-end test

Latest recorded result: 1 passed.

The test verifies that a Manager's attempted final approval of an
over-limit invoice is rejected and that the invoice remains unchanged.

Console evidence:
`specs/001-approval-workflow/playwright-final-console.txt`

A fresh run on 2026-10-29 reported one Playwright test passed in 12.6
seconds. This is evidence of the focused scenario, not a claim that the
entire application has been exhaustively tested.

### Stryker mutation testing

#### Fresh final Domain-specific run

A fresh Stryker run was completed successfully on 2026-10-29 using
Stryker.NET 4.13.0, launched from `backend/tests/InvoiceApp.Tests` and
targeting `InvoiceApp.Domain.csproj`.

-   Exit code: 0
-   Mutants created: 152
-   Mutants tested: 119
-   Killed: 119
-   Survived: 0
-   Timed out: 0
-   Ignored: 33
-   Stryker-reported mutation score: 100.00%

The 33 ignored mutants were excluded by Stryker's
`Removed by block already covered filter`. The score applies to the 119
mutants tested in `InvoiceApp.Domain`; it is not a score for the entire
application or API project.

Evidence:

-   Console log:
    `specs/001-approval-workflow/mutation-after-console.txt`
-   JSON report: `specs/001-approval-workflow/mutation-after.txt`
-   Generated report:
    `specs/001-approval-workflow/mutation-after/reports/mutation-report.json`

The fresh run found 79 tests. It was run after the decimal-boundary
tests were committed, so this is the current final Domain-specific
mutation evidence.

#### Historical general mutation reports and baseline limitation

The JSON reports in
`specs/001-approval-workflow/mutation/reports/mutation-report.json` and
`specs/001-approval-workflow/mutation-final/reports/mutation-report.json`
have identical results:

-   Mutants created: 144
-   Mutants tested: 88
-   Killed: 74
-   Survived: 14
-   Ignored: 31
-   No coverage: 25

The calculated killed-to-tested ratio is 84.09% (74 / 88). This is a
calculated ratio and should not be presented as Stryker's official score
unless confirmed by the report's own summary.

The baseline console log
`specs/001-approval-workflow/mutation-before-console.txt` reports 30.77%
(12 killed and 10 survived among 22 tested mutants). Its associated JSON
report records different totals: 74 killed, 14 survived, 25 with no
coverage, and 31 ignored. These baseline artifacts conflict. The two
general JSON reports have identical counts, so they do not establish a
before/after improvement.

Accordingly, no reliable like-for-like mutation-score improvement is
claimed. The fresh Domain-specific 100.00% result is reported separately
because its target and mutant counts differ. The historical baseline
discrepancy remains unresolved.

## PR self-review findings and verdicts

1.  **Approval thresholds and stages --- Fixed and covered by
    source/tests.** The implementation contains the single-stage
    thresholds and the two-stage flow for invoices above 500,000.

2.  **Case-insensitive duplicate approver --- Dismissed with reason.**
    The `Director_CannotBeSameUserAsManager` test checks `manager-user`
    against `MANAGER-USER`, verifies that the Manager stage remains
    intact, and verifies that the repository is not updated.

3.  **Decimal threshold boundaries --- Fixed.** Added tests for
    50,000.01 and 500,000.01, asserting rejection and preservation of
    invoice state and stage records. Commit: `ec1f172`.

4.  **State preservation on rejected decisions --- Partially verified.**
    Targeted unit tests cover relevant state and stage-record
    preservation, and the recorded Playwright scenario covers Manager
    over-limit rejection. Complete coverage of every required
    decision-history field has not been established by this review.

5.  **Separate audit-history storage --- Deferred pending
    clarification.** The reviewed invoice model has decision metadata
    and Manager/Director stage records. The requirement for a separate
    audit-history collection or durable audit log has not been
    established.

6.  **Mutation testing --- Fresh Domain-specific result available.** The
    latest Domain run reported 100.00% for 119 tested mutants, with 33
    ignored. This result does not prove that every possible behavior is
    tested.

7.  **Baseline comparison --- Deferred.** The baseline console and JSON
    report disagree on mutation counts, and the two general JSON reports
    have identical results. No like-for-like improvement is claimed.

8.  **Demo and final packaging --- Partially complete.** The AI
    interaction log and summary are present in
    `specs/001-approval-workflow/`, and the demo video link is included
    below. Confirm that the link is accessible to reviewers, create the
    required pull request, and verify final Git status and packaging
    before declaring the assignment complete.

## Demo video

[Watch the invoice approval workflow demo]- https://nagarro-my.sharepoint.com/:f:/p/shashwat_raghuwanshi/IgBsA8DzGloKQYltCaF77S7uAZ2BoO4hO-AzOY0Scvx4CRk?e=WZM8VN

## Outstanding deliverables

-   [x] AI interaction log and summary are present:
    `specs/001-approval-workflow/ai-interaction-log-assignment.jsonl`
    and
    `specs/001-approval-workflow/ai-interaction-summary-assignment.md`.
    Consult the summary for provenance and limitations.
-   [x] Record the live 8--10 minute demo video and add its actual URL
    to this document. Verify reviewer access to the linked video.
-   [x] Review the specification quality checklist at
    `specs/001-approval-workflow/checklists/requirements.md`; its listed
    requirements are checked.
-   [x] Record the final packaging checklist at
    `specs/001-approval-workflow/FINAL-SUBMISSION-CHECKLIST.md`; perform
    the final local packaging/working-tree verification before
    submission.
-   [ ] Resolve or explicitly leave unresolved the baseline
    mutation-report discrepancy. No comparable before/after improvement
    is claimed in this document.
-   [ ] Confirm whether a separate durable audit-history store is
    required by the assignment specification.
-   [x] Capture fresh final Domain mutation evidence in
    `mutation-after-console.txt` and `mutation-after.txt`.
-   [x] Record validation notes based on actual terminal output: unit
    tests 79/79 passed, focused Playwright test 1/1 passed, and the
    fresh Domain Stryker run completed with exit code 0.
-   [ ] Verify final commit history, working tree, PR description, and
    demo-video link before submission.

## Pull request preparation

The changes are on `feature/invoice-approval-limits` and have been pushed
to the fork. **A pull request has not yet been created**, so no PR URL is
listed here. The assignment requires a PR on the fork, a review of the
changes using an agent, and every review finding to be recorded in the PR
description with a clear verdict: fixed, dismissed with a reason, or
deferred with a reason.

When creating the PR, use the intended base branch specified by the
assignment/repository. The PR description should include the following:

1. **Summary and scope:** approval thresholds and two-stage approval
   above 500,000; case-insensitive distinct-approver validation; rejection
   without unintended state mutation; unit and focused Playwright tests.
2. **Validation evidence:** `dotnet test .\backend\InvoiceApp.sln --nologo`
   (79 passed); `npm test` from `e2e` (1 passed); fresh Domain Stryker
   result (119 tested/killed, 0 survived, 33 ignored, 100.00% reported).
   State that the mutation score applies only to the tested Domain mutants.
3. **Evidence and demo:** link the committed evidence under
   `specs/001-approval-workflow/` and the demo video linked above.
4. **Review findings and verdicts:** include every finding below, retaining
   the stated reason for dismissed/deferred items. Do not imply the
   unresolved baseline discrepancy or audit-history question is fixed.
5. **Known limitations:** the historical baseline console and JSON
   artifacts disagree, so no reliable before/after mutation-score
   improvement is claimed; the requirement for a separate durable
   audit-history store remains to be clarified.

The existing **PR self-review findings and verdicts** section is the
source of truth for the findings list to copy into the PR description.
After creating the PR, add its actual URL here and update this status.

### PR status

- Branch: `feature/invoice-approval-limits`
- Repository: `ShashwatRaghuwanshi/ai-sdlc-demo-app`
- Changes: committed and pushed to the feature branch (verify latest
  working-tree status before submission).
- Pull request: **Not created yet**.

## Submission status as of 2026-10-29

### Evidence available

-   Unit tests: 79 passed, 0 failed, 0 skipped in the recorded run
    (`unit-tests-final-console.txt`).
-   Playwright: one focused end-to-end test passed. A fresh run reported
    1 passed in 12.6 seconds (`playwright-final-console.txt` is the
    recorded evidence path; ensure it reflects the latest run if
    replacing evidence).
-   Stryker: fresh Domain-specific run completed successfully with exit
    code 0 and reported 100.00% for 119 tested mutants; 33 were ignored,
    none survived, and none timed out. The console and JSON evidence are
    committed at the `mutation-after` paths.
-   Baseline comparison: historical baseline console and JSON artifacts
    remain inconsistent; no reliable score delta is claimed.
-   AI interaction log and summary are present in the repository.
-   Git history contains multiple meaningful commits, including
    implementation, test, E2E, and evidence/documentation commits.
    Verify the actual history and working tree before submission.

### Remaining before final submission

1.  Verify that the demo video link is accessible to reviewers.
2.  Reconcile the historical baseline discrepancy if possible; otherwise
    keep the limitation explicitly documented and do not claim a score delta.
3.  Confirm whether a separate durable audit-history store is required
    by the assignment specification; document the outcome or leave it
    explicitly deferred.
4.  Run `git status --short` and review all modified/untracked files;
    commit and push the final `ASSIGNMENT.md` update.
5.  Create the required pull request against the intended base branch.
6.  Have the agent review the PR and include every finding in the PR
    description with a fixed, dismissed-with-reason, or deferred-with-reason
    verdict. Use the findings and wording in this document as the checklist.
7.  Update this document with the actual PR URL and verify the final task
    checklist reflects the successful final mutation run.

## Validation commands

From the repository root in PowerShell:

``` powershell
dotnet test .\backend\InvoiceApp.sln --nologo
```

For the focused Playwright scenario:

``` powershell
Push-Location .\e2e
try {
    npm test
}
finally {
    Pop-Location
}
```

For the pinned Stryker run, launch from `backend/tests/InvoiceApp.Tests`
as specified in `AGENTS.md`:

``` powershell
$repoRoot = (Get-Location).Path
$evidence = Join-Path $repoRoot "specs\001-approval-workflow"
$consoleLog = Join-Path $evidence "mutation-after-console.txt"
$reportOutput = Join-Path $evidence "mutation-after"

Push-Location ".\backend\tests\InvoiceApp.Tests"
try {
    dotnet stryker `
        --project InvoiceApp.Domain.csproj `
        --reporter progress `
        --reporter json `
        --output $reportOutput 2>&1 |
        Tee-Object -FilePath $consoleLog

    $strykerExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

Write-Host "Stryker exit code: $strykerExitCode"
```

The successful final run's actual console output and JSON report are
preserved in the `mutation-after` evidence paths. Do not reuse the
historical baseline score as if it were a comparable result.

## Recent commits

-   `dbadc37` --- `test: capture final domain mutation evidence`
-   `af3ac46` --- `docs: reconcile assignment validation evidence`
-   `cdca12c` --- `docs: align demo accounts with approval roles`
-   `ec1f172` --- `test: cover decimal invoice approval boundaries`

Verify the current branch and history before submission.

## Pull Request

- **PR:** [feat: implement invoice approval limits and validation](https://github.com/ShashwatRaghuwanshi/ai-sdlc-demo-app/pull/1)
- **Status:** Open
- **Base branch:** main
- **Feature branch:** feature/invoice-approval-limits

## Conclusion

The repository contains the invoice approval workflow implementation and
recorded unit-test, Playwright, and fresh Domain-specific mutation
evidence. The recorded unit-test run passed all 79 tests, the focused
Playwright test passed, and the latest Domain-specific Stryker run
reported 100.00% for 119 tested mutants, with 33 ignored, 0 survived,
and 0 timed out.

The historical general mutation reports do not establish a reliable
before/after comparison, so no mutation-score improvement is claimed.
The demo video link is now included, but reviewer access should be checked.
The assignment should not be declared fully complete until the baseline
comparison decision, audit-history clarification, PR creation/review, and
final packaging checks have been addressed.
