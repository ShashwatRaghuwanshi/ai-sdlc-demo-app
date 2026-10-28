# Final Submission Checklist â€” Invoice Approval Workflow

**Review date:** 2026-10-28
**Scope:** Reconciled against the supplied repository archive and its recorded evidence. This review did not execute .NET or Playwright commands; the current environment has no .NET SDK.

## Present and documented

- [x] Domain implementation for Manager/Director thresholds and the Manager-first/Director-second workflow above 500,000.
- [x] Tests for exact thresholds, invalid roles/lifecycle, same-user duplicate approver, state preservation, and decimal boundaries are present in the test project.
- [x] Recorded unit-test output: 79 passed, 0 failed, 0 skipped.
- [x] Recorded Playwright output: 1 passed for the focused Manager over-limit rejection scenario.
- [x] Historical Stryker report and console evidence are present; historical score is 100.00% for 119 tested mutants, 33 ignored.
- [x] `AGENTS.md` contains approval rules, layering, test, review and mutation-testing guidance.
- [x] Root `ASSIGNMENT.md` summarizes implementation, evidence, self-review findings and limitations.
- [x] AI interaction JSONL and summary are present under `specs/001-approval-workflow/`.
- [x] Specification quality checklist is completed.
- [x] Git history contains multiple meaningful commits for implementation, tests, E2E, and evidence/documentation.
- [x] Mutation-report discrepancy is explicitly disclosed; no unsupported baseline improvement claim is made.

## Must finish before calling the submission complete

- [ ] Record the 8â€“10 minute demo video and add its actual URL to `ASSIGNMENT.md`.
- [ ] Run `dotnet test .\backend\InvoiceApp.sln` against the final current working tree and preserve the actual terminal output.
- [ ] Run `cd e2e && npm install && npx playwright install chromium && npm test` against the final current working tree and preserve the actual output.
- [ ] Run the pinned Stryker 4.13.0 mutation test against the latest test set; save output and JSON to the filenames required by `tasks.md`.
- [ ] Resolve the baseline mutation discrepancy if possible; otherwise preserve the documented limitation and do not report a score delta.
- [ ] Review the current untracked/modified files, including the large mutation logs, and decide which should be committed or excluded from the submission.
- [ ] Run `git status --short` and inspect `git diff --check` before final commit/submission.
- [ ] Confirm whether a separate durable audit-history store is required. Do not claim one exists unless implemented and verified.

## Known evidence limitations

- The unit-test and Playwright logs are historical and were not regenerated during this review.
- The successful Stryker report predates the latest two decimal-boundary tests.
- Baseline console and JSON report totals conflict; a reliable before/after mutation-score comparison is not currently available.
- The supplied archive has a non-clean Git working tree. This review must not be interpreted as a claim that all changes are committed.
- The demo-video URL is absent.
