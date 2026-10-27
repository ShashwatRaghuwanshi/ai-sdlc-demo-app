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
- The same user cannot satisfy both approval stages.
- The Director's second-stage approval requires the Manager stage to exist first.
- Rejected attempts must preserve the invoice's state and relevant decision metadata and approval history.

The Manager-first sequence for invoices above 500,000 is the agreed interpretation used by this implementation.

## Final validation evidence

### Unit tests

Recorded command:

```powershell
dotnet test .\backend\InvoiceApp.sln --no-restore
```

- Passed: 77
- Failed: 0
- Skipped: 0
- Total: 77
- Exit code: 0

Console evidence: `specs/001-approval-workflow/unit-tests-final-console.txt`

### Playwright end-to-end test

Recorded result: 1 passed.

The test verifies that a Manager's attempted final approval of an over-limit invoice is rejected and that the invoice remains unchanged.

Console evidence: `specs/001-approval-workflow/playwright-final-console.txt`

### Stryker mutation testing

**Final successful run**

- Stryker.NET version: 4.13.0
- Tests discovered: 77
- Mutants created: 152
- Mutants tested: 119
- Mutants ignored: 33
- Final mutation score: 100.00%
- Exit code: 0

The 119 mutants tested in this run were all killed by the tests. The 33 ignored mutants were excluded by Stryker's `Removed by block already covered filter`.

Evidence:

- Console log: `specs/001-approval-workflow/mutation-final-report-console.txt`
- HTML report: `specs/001-approval-workflow/mutation-final-success/reports/mutation-report.html`

### Baseline comparison limitation

The baseline console log records a mutation score of 30.77%, with 12 killed and 10 surviving mutants. However, the baseline JSON report records 74 killed, 14 survived, 25 with no coverage, and 31 ignored. These results conflict and cannot be treated as a reliable baseline comparison.

Consequently, the final 100.00% result demonstrates the outcome of the successful final run, but a reliable mutation-score improvement against the baseline cannot be claimed until the baseline discrepancy is resolved. The final HTML report and console log listed above are the authoritative evidence for the successful final run.

## Review findings and verdicts

1. **Approval thresholds and stages — Supported by source and tests.** The implementation contains the single-stage thresholds and the two-stage flow for invoices above 500,000.
2. **State preservation on rejected decisions — Covered by targeted tests and the Playwright scenario.** The recorded end-to-end test passed for the Manager over-limit rejection path.
3. **Mutation testing — Successful final run recorded.** The final successful Stryker run achieved a 100.00% mutation score across 119 tested mutants, with 33 ignored. This result does not establish that every possible behavior or scenario is tested.
4. **Baseline comparison — Not yet reliable.** The baseline console and JSON report disagree on mutation counts. Do not claim a like-for-like improvement until the discrepancy has been investigated.
5. **Surviving mutants in earlier reports — Requires contextual review.** Earlier reports recorded surviving mutants and mutants with no coverage. Review those report details before deciding whether additional assertions are worthwhile; do not change production behavior solely to improve a score.
6. **Full delivery review — Pending verification.** The complete assignment checklist, AI interaction log, demo video, and final packaging review still need verification.

## Outstanding deliverables

- [ ] Verify and complete the required AI interaction log using actual interactions and tools used.
- [ ] Record the link to the live 8–10 minute demo video after it exists.
- [ ] Complete the final assignment checklist and packaging review.
- [ ] Review the baseline mutation-report discrepancy and document the outcome.
- [ ] Verify the final commit history and working tree before submission.

## Conclusion

The repository contains the approval workflow implementation and recorded unit-test, Playwright, and Stryker validation evidence. The latest recorded unit-test run passed all 77 tests, the Playwright end-to-end test passed, and the final successful Stryker run reported a 100.00% mutation score for 119 tested mutants.

The baseline mutation evidence remains inconsistent, so a reliable improvement claim cannot yet be made. The assignment should not be declared fully complete until the outstanding documentation, demo, checklist, and final packaging review deliverables have been verified.
