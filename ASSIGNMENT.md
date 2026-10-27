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

Recorded command: `dotnet test backend/InvoiceApp.sln --no-restore`

- Passed: 43
- Failed: 0
- Skipped: 0

Console evidence: `specs/001-approval-workflow/unit-tests-final-console.txt`

### Playwright end-to-end test

Recorded result: 1 passed.

The test verifies that a Manager's attempted final approval of an over-limit invoice is rejected and that the invoice remains unchanged.

Console evidence: `specs/001-approval-workflow/playwright-final-console.txt`

### Stryker mutation testing

Recorded final mutation score: 65.49%.

- Mutants created: 144
- Killed: 74
- Survived: 14
- No coverage: 25
- Ignored: 31
- Timed out: 0

Console evidence: `specs/001-approval-workflow/mutation-final-console.txt`

JSON report: `specs/001-approval-workflow/mutation-final-report.json`

The historical baseline used a different mutant count, so its score should not be compared as a strictly like-for-like measurement without accounting for the different mutant sets.

## Review findings and verdicts

1. **Approval thresholds and stages — Supported by source and tests.** The implementation contains the single-stage thresholds and the two-stage flow for amounts above 500,000.
2. **State preservation on rejected decisions — Covered by targeted tests and the Playwright scenario.** The recorded end-to-end test passed for the Manager over-limit rejection path.
3. **Mutation testing — Completed for the recorded final run.** The score is 65.49%; 14 mutants survived and 25 had no coverage. These results do not establish that every possible behavior is tested.
4. **Surviving mutants — Further review recommended.** The recorded survivors are primarily string/default-value mutations in the Domain layer. Review their report details before deciding whether additional assertions are worthwhile; do not change production behavior solely to improve the score.
5. **Full delivery review — Pending.** The complete assignment checklist, AI interaction log, demo video, and final packaging review still need verification.

## Outstanding deliverables

- [ ] Verify and complete the required AI interaction log with actual interactions and tools used.
- [ ] Record the link to the live 8–10 minute demo video after it exists.
- [ ] Complete the final assignment checklist and packaging review.
- [ ] Verify the final commit history and working tree before submission.

## Conclusion

The repository contains implementation changes and recorded unit-test, Playwright, and mutation-testing results. The recorded final checks passed as described above. The assignment is not declared fully complete until the remaining documentation, demo, and final review deliverables have been verified.
