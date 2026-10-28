# Assignment: Invoice Approval Workflow

## Implementation summary

The approval workflow is implemented in the Domain layer:

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
dotnet test .\backend\InvoiceApp.sln --no-restore
```

- Passed: 79
- Failed: 0
- Skipped: 0
- Total: 79
- Exit code: 0

Console evidence: `specs/001-approval-workflow/unit-tests-final-console.txt`

The two additional decimal-boundary tests verify that amounts of 50,000.01 and 500,000.01 are rejected for the respective final-approval attempts without mutating the invoice or calling the repository update. These tests and the updated unit-test evidence were committed in `ec1f172` (`test: cover decimal invoice approval boundaries`).

### Playwright end-to-end test

Previously recorded result: 1 passed.

The test verifies that a Manager's attempted final approval of an over-limit invoice is rejected and that the invoice remains unchanged.

Console evidence: `specs/001-approval-workflow/playwright-final-console.txt`

The Playwright test was not rerun as part of the latest decimal-boundary test change. The console artifact reflects the focused test invoked by the npm `test` script; the documented full validation command is `cd e2e && npm install && npx playwright install chromium && npm test`.

### Stryker mutation testing

**Previously recorded final successful run**

- Stryker.NET version: 4.13.0
- Tests discovered: 77
- Mutants created: 152
- Mutants tested: 119
- Mutants ignored: 33
- Final mutation score: 100.00%
- Exit code: 0

The 119 mutants tested in that run were all killed by the tests. The 33 ignored mutants were excluded by Stryker's `Removed by block already covered filter`.

Evidence:

- Console log: `specs/001-approval-workflow/mutation-final-report-console.txt`
- HTML report: `specs/001-approval-workflow/mutation-final-success/reports/mutation-report.html`

**Important:** This mutation result predates the two latest decimal-boundary tests. No post-change mutation run is claimed here.

### Baseline comparison limitation

The baseline console log records a mutation score of 30.77%, with 12 killed and 10 surviving mutants. However, the baseline JSON report records 74 killed, 14 survived, 25 with no coverage, and 31 ignored. These results conflict and cannot be treated as a reliable baseline comparison.

Consequently, the recorded 100.00% result demonstrates the outcome of the earlier successful final run, but a reliable mutation-score improvement against the baseline cannot be claimed until the discrepancy is resolved. The baseline and final reports must be compared using consistent runs and metrics.

## PR self-review findings

1. **Approval thresholds and stages — Fixed and covered by source/tests.** The implementation contains the single-stage thresholds and the two-stage flow for invoices above 500,000.
2. **Case-insensitive duplicate approver — Dismissed with reason.** The existing `Director_CannotBeSameUserAsManager` test checks `manager-user` against `MANAGER-USER`, verifies that the Manager stage remains intact, and verifies that the repository is not updated.
3. **Decimal threshold boundaries — Fixed.** Added tests for 50,000.01 and 500,000.01, asserting rejection and preservation of invoice state and stage records. Commit: `ec1f172`.
4. **State preservation on rejected decisions — Partially verified.** Targeted unit tests cover relevant state and stage-record preservation, and the recorded Playwright scenario covers Manager over-limit rejection. Complete coverage of every required decision-history field has not been established by this review.
5. **Separate audit-history storage — Deferred pending clarification.** The reviewed invoice model has decision metadata and Manager/Director stage records. The requirement for a separate audit-history collection or durable audit log has not been established.
6. **Mutation testing — Earlier successful run recorded.** The prior Stryker run reported a 100.00% score for 119 tested mutants, with 33 ignored. This does not prove that every possible behavior is tested, and it predates the latest two tests.
7. **Baseline comparison — Deferred.** The baseline console and JSON report disagree on mutation counts. Do not claim a like-for-like improvement until the discrepancy has been investigated.
8. **Full delivery review — Partially complete.** The AI interaction log and summary are now present in `specs/001-approval-workflow/`. The live demo video and final local packaging/working-tree verification remain outstanding.

## Outstanding deliverables

- [x] Create the AI interaction log and summary using available evidence. The repository contains `specs/001-approval-workflow/ai-interaction-log-assignment.jsonl` and `ai-interaction-summary-assignment.md`; consult the summary for provenance/limitations.
- [ ] Record the link to the live 8–10 minute demo video after it exists.
- [x] Review the specification quality checklist at `specs/001-approval-workflow/checklists/requirements.md`; its listed requirements are checked. The final packaging review is summarized in `specs/001-approval-workflow/FINAL-SUBMISSION-CHECKLIST.md`.
- [ ] Review the baseline mutation-report discrepancy and document the outcome.
- [ ] Review the untracked mutation logs and retain only the evidence files that are needed.
- [ ] Verify the final commit history and working tree before submission.
- [ ] Confirm whether a separate audit-history store is required by the assignment specification.
- [x] Record a final verification note based on existing terminal output. Unit-test and Playwright evidence are cited above; no new test run is claimed from this review environment.

## Submission status as of 2026-10-28

### Evidence available in the repository

- Unit tests: 79 passed, 0 failed, 0 skipped in the recorded run (`unit-tests-final-console.txt`).
- Playwright: 1 passed in the recorded focused E2E run (`playwright-final-console.txt`).
- Stryker: an earlier successful run reported 100.00% for 119 tested mutants, with 33 ignored (`mutation-final-report-console.txt` and the HTML report). This predates the two decimal-boundary tests.
- AI interaction log and summary are present in this repository.
- Git history includes multiple meaningful commits, including implementation, test, E2E, and evidence/documentation commits.

### Outstanding before final submission

1. Record and add the required 8–10 minute demo video link.
2. Run the unit suite and focused Playwright test on the current working tree if time permits, then replace evidence only with the output from those actual runs.
3. Run Stryker on the latest test set and save the console output/JSON to the exact `mutation-after` filenames required by `tasks.md`; do not reuse the older score as if it included the decimal-boundary tests.
4. Resolve or explicitly leave unresolved the baseline console/JSON discrepancy. The current files are not a trustworthy before/after comparison.
5. Run `git status --short` and review all modified/untracked files before committing/submitting. This archive's Git working tree is not clean; many source/config files and several evidence logs are modified or untracked.
6. Confirm the assignment owner's expectation for a separate durable audit-history store; current implementation preserves invoice decision/stage metadata, but the need for a separate audit store is not established by the provided requirement evidence.

### Exact validation commands

From the repository root in PowerShell:

```powershell
dotnet test .\backend\InvoiceApp.sln
```

For the focused Playwright scenario:

```powershell
cd e2e
npm install
npx playwright install chromium
npm test
cd ..
```

For the pinned Stryker run, use the validated invocation from the repository's `AGENTS.md`/prior evidence and save the actual output to the required `mutation-after-console.txt` and `mutation-after.txt` paths. Do not claim completion until the command actually runs successfully.

## Recent commit

- `ec1f172` — `test: cover decimal invoice approval boundaries`

Earlier implementation and validation commits remain in the branch history. The four untracked mutation logs have not been committed.

## Conclusion

The repository contains the approval workflow implementation and recorded unit-test, Playwright, and Stryker validation evidence. The latest unit-test run passed all 79 tests, the previously recorded Playwright end-to-end test passed, and an earlier successful Stryker run reported a 100.00% mutation score for 119 tested mutants.

The mutation baseline remains inconsistent, and the latest decimal-boundary tests have not been included in a new mutation run. The assignment should not be declared fully complete until the remaining documentation, demo, checklist, and final packaging items have been verified.