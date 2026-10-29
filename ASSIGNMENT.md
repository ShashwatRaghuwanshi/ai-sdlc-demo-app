# Assignment: Invoice Approval Workflow

## Implementation summary

The invoice approval workflow is implemented in the Domain layer:

- `backend/src/InvoiceApp.Domain/ApprovalService.cs`
- `backend/src/InvoiceApp.Domain/Invoice.cs`

Supporting tests and repository tracking are in:

- `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`
- `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`

The Playwright page object and end-to-end test are in:

- `e2e/pages/InvoiceApprovalPage.ts`
- `e2e/tests/manager-over-limit-rejection.spec.ts`

The existing session-based authentication and API authorization model has been retained.

## Business rules

- A Manager may finally approve invoices up to and including 50,000.
- A Director may finally approve invoices above 50,000 and up to and including 500,000.
- Invoices above 500,000 require a Manager first-stage approval followed by a different Director's final approval.
- The same user cannot satisfy both approval stages. Username comparison is case-insensitive.
- The Director's second-stage approval requires the Manager stage to exist first.
- Rejected attempts must preserve the invoice's state and relevant decision metadata and approval-stage records.

The Manager-first sequence for invoices above 500,000 is the agreed interpretation used by this implementation.

## Final validation evidence

### Unit tests

Latest recorded command:

```powershell
dotnet test .\backend\InvoiceApp.sln --nologo
```

- Passed: 79
- Failed: 0
- Skipped: 0
- Total: 79
- Exit code: 0

Console evidence: `specs/001-approval-workflow/unit-tests-final-console.txt`

The two additional decimal-boundary tests verify that amounts of 50,000.01 and 500,000.01 are rejected for the respective final-approval attempts without mutating the invoice or calling the repository update. These tests and the updated unit-test evidence were committed in `ec1f172` (`test: cover decimal invoice approval boundaries`).

### Playwright end-to-end test

Recorded result: 1 passed.

The test verifies that a Manager's attempted final approval of an over-limit invoice is rejected and that the invoice remains unchanged.

Console evidence: `specs/001-approval-workflow/playwright-final-console.txt`

The recorded console output shows one Playwright test passed in 12.3 seconds. This is evidence of the focused scenario, not a claim that the entire application has been exhaustively tested.

### Stryker mutation testing

#### Final Domain-specific run

- Tool: Stryker.NET 4.13.0
- Target: `InvoiceApp.Domain.csproj`
- Mutants created: 152
- Mutants tested: 119
- Killed: 119
- Survived: 0
- Timed out: 0
- Ignored: 33
- Stryker-reported mutation score: 100.00%

The 33 ignored mutants were excluded by Stryker's `Removed by block already covered filter`. This result applies to `InvoiceApp.Domain`, not the API project.

Evidence:

- Console log: `specs/001-approval-workflow/mutation-final-report-console.txt`
- Summary: `specs/001-approval-workflow/mutation-domain-summary.md`
- JSON report: `specs/001-approval-workflow/mutation-domain/reports/mutation-report.json`

The final Domain-specific report is available, but the evidence provided does not establish that this run includes the two latest decimal-boundary tests. Do not describe it as a post-decimal-test run unless that is verified.

#### General mutation reports and baseline limitation

The JSON reports in `specs/001-approval-workflow/mutation/reports/mutation-report.json` and `specs/001-approval-workflow/mutation-final/reports/mutation-report.json` have identical results:

- Mutants created: 144
- Mutants tested: 88
- Killed: 74
- Survived: 14
- Ignored: 31
- No coverage: 25

The calculated killed-to-tested ratio is 84.09% (74 / 88). This is a calculated ratio and should not be presented as Stryker's official score unless confirmed by the report's own summary.

The baseline console log `specs/001-approval-workflow/mutation-before-console.txt` reports 30.77% (12 killed and 10 survived among 22 tested mutants). Its associated JSON report records 74 killed, 14 survived, 25 with no coverage, and 31 ignored. These baseline artifacts conflict. The two general JSON reports have identical counts, so they do not establish a before/after improvement.

Accordingly, no reliable like-for-like mutation-score improvement is claimed. The Domain-specific 100.00% result is reported separately because its target and mutant counts differ.

## PR self-review findings and verdicts

1. **Approval thresholds and stages — Fixed and covered by source/tests.** The implementation contains the single-stage thresholds and the two-stage flow for invoices above 500,000.

2. **Case-insensitive duplicate approver — Dismissed with reason.** The `Director_CannotBeSameUserAsManager` test checks `manager-user` against `MANAGER-USER`, verifies that the Manager stage remains intact, and verifies that the repository is not updated.

3. **Decimal threshold boundaries — Fixed.** Added tests for 50,000.01 and 500,000.01, asserting rejection and preservation of invoice state and stage records. Commit: `ec1f172`.

4. **State preservation on rejected decisions — Partially verified.** Targeted unit tests cover relevant state and stage-record preservation, and the recorded Playwright scenario covers Manager over-limit rejection. Complete coverage of every required decision-history field has not been established by this review.

5. **Separate audit-history storage — Deferred pending clarification.** The reviewed invoice model has decision metadata and Manager/Director stage records. The requirement for a separate audit-history collection or durable audit log has not been established.

6. **Mutation testing — Domain-specific result available.** The final Domain run reported 100.00% for 119 tested mutants, with 33 ignored. This does not prove that every possible behavior is tested. The available evidence does not establish that the run includes the latest decimal-boundary tests.

7. **Baseline comparison — Deferred.** The baseline console and JSON report disagree on mutation counts, and the two general JSON reports have identical results. No like-for-like improvement is claimed.

8. **Full delivery review — Partially complete.** The AI interaction log and summary are present in `specs/001-approval-workflow/`. The live demo video link and final local packaging/working-tree verification remain outstanding until confirmed.

## Outstanding deliverables

- [x] AI interaction log and summary are present: `specs/001-approval-workflow/ai-interaction-log-assignment.jsonl` and `specs/001-approval-workflow/ai-interaction-summary-assignment.md`. Consult the summary for provenance and limitations.
- [ ] Record the link to the live 8–10 minute demo video after it exists.
- [x] Review the specification quality checklist at `specs/001-approval-workflow/checklists/requirements.md`; its listed requirements are checked.
- [x] Record the final packaging checklist at `specs/001-approval-workflow/FINAL-SUBMISSION-CHECKLIST.md`; final local packaging/working-tree verification still needs to be performed.
- [ ] Resolve or explicitly leave unresolved the baseline mutation-report discrepancy.
- [ ] Review untracked mutation logs and retain only evidence files needed for the submission.
- [ ] Verify final commit history and working tree before submission.
- [ ] Confirm whether a separate durable audit-history store is required by the assignment specification.
- [x] Record validation notes based on existing terminal output. No new test run is claimed by this document.

## Submission status as of 2026-10-29

### Evidence available

- Unit tests: 79 passed, 0 failed, 0 skipped in the recorded run (`unit-tests-final-console.txt`).
- Playwright: 1 focused end-to-end test passed (`playwright-final-console.txt`).
- Stryker: the Domain-specific run reported 100.00% for 119 tested mutants, with 33 ignored. General mutation JSON reports each record 74 killed, 14 survived, 31 ignored, and 25 with no coverage; the baseline artifacts remain inconsistent.
- AI interaction log and summary are present in the repository.
- Git history reportedly contains multiple meaningful commits, including implementation, test, E2E, and evidence/documentation commits. Verify the actual history and working tree before submission.

### Remaining before final submission

1. Record and add the actual 8–10 minute demo video link.
2. If time permits, rerun the unit suite and focused Playwright test on the current working tree, and replace evidence only with output from those actual runs.
3. Run Stryker against the latest test set and save actual console output and JSON to the exact `mutation-after` filenames required by `tasks.md`. Do not reuse an older score as if it included the decimal-boundary tests.
4. Keep the baseline discrepancy explicitly documented unless it can be resolved with traceable, comparable runs.
5. Run `git status --short` and review all modified/untracked files before committing/submitting.
6. Confirm whether a separate durable audit-history store is required by the assignment specification.

## Validation commands

From the repository root in PowerShell:

```powershell
dotnet test .\backend\InvoiceApp.sln --nologo
```

For the focused Playwright scenario:

```powershell
Push-Location .\e2e
try {
    npm test
}
finally {
    Pop-Location
}
```

For the pinned Stryker run, use the validated invocation from `AGENTS.md` and `tasks.md`. Save the actual output to the required `mutation-after-console.txt` and `mutation-after.txt` paths only after a successful run. Do not claim completion until the command runs successfully.

## Recent commits

- `ec1f172` — `test: cover decimal invoice approval boundaries`
- `cdca12c` — `docs: align demo accounts with approval roles` (reported as pushed; verify current branch/history before submission).

## Conclusion

The repository contains the approval workflow implementation and recorded unit-test, Playwright, and Stryker evidence. The recorded unit-test run passed all 79 tests, the focused Playwright test passed, and the Domain-specific Stryker run reported 100.00% for 119 tested mutants.

The general mutation reports do not establish a reliable before/after comparison, and the available evidence does not confirm a new mutation run after the decimal-boundary tests. The assignment should not be declared fully complete until the demo link, mutation-evidence decision, audit-history clarification, and final packaging/working-tree checks have been addressed.
