# Implementation Plan: Approval Workflow

**Branch**: `001-approval-workflow` | **Date**: 2026-10-22 | **Spec**: [specs/001-approval-workflow/spec.md](specs/001-approval-workflow/spec.md)

**Input**: Feature specification from `/specs/001-approval-workflow/spec.md`

## Status

This plan is intentionally planning-only. The approved feature specification and repo constitution are already in place, and the app code remains unchanged. The implementation work described below is the required execution plan, not a claim that the verification steps have already passed.

## Summary

The feature preserves the agreed business rules:
- Manager final approval is valid for invoices up to and including 50,000.
- Director final approval is valid for invoices above 50,000 and up to and including 500,000.
- Invoices above 500,000 require a Manager first-stage approval and a different Director second-stage approval to finalize.
- Rejected approval attempts must not mutate the invoice or its existing approval history.

The implementation stays within the repository’s established architecture: all approval logic remains in `InvoiceApp.Domain`, the API layer continues to expose the workflow without owning business rules, and tests remain the enforcement mechanism before implementation is considered complete.

## Technical Context

**Language/Version**: C# / .NET 8, ASP.NET Core Web API

**Primary Dependencies**: xUnit, Playwright, existing in-memory repository, and the project’s current bearer-token session auth model

**Storage**: In-memory repository in the API project; repository abstraction remains in the domain project

**Testing**: xUnit domain/unit tests, Playwright E2E validation, and mutation testing with Stryker.NET

**Target Platform**: Local demo app and automated verification in the repository workspace

**Project Type**: Web service + static UI demo

**Performance Goals**: local demo responsiveness; correctness and state preservation are the primary goals

**Constraints**: keep approval rules in `InvoiceApp.Domain`; do not move business logic into controllers or UI; preserve current auth/session structure; keep any rejected attempt as a no-op relative to invoice state

**Scale/Scope**: a small approval workflow with threshold checks, stage tracking, role enforcement, and a focused E2E regression test

## Completed vs Planned Work

### Completed before this plan
- The feature specification for this workflow was reviewed and refined to match the assignment requirements.
- The project constitution was aligned to the repository’s actual architecture and governance.
- The current repo structure and auth model were inspected and documented.
- No application code or tests were changed as part of this planning step.

### Planned implementation work
- Add failing unit tests for each business rule and invalid attempt.
- Implement the single-stage manager/director threshold rules in the domain layer.
- Implement the two-stage approval flow above 500,000 in the domain layer.
- Validate through the API layer without embedding business rules there.
- Add the focused Playwright E2E failure-path test for an over-limit rejection.
- Run mutation testing and capture text evidence before and after the fix.
- Prepare the assignment artifacts, review, and demo deliverables.

## Constitution Check

*Gate status: PASS for the planned implementation, provided all steps below are executed and no claim of completion is made before verification.*

- Business logic remains in `InvoiceApp.Domain`; controllers remain request shapers and keep session-based auth and `RequireRole` enforcement.
- Test-first development remains mandatory: every rule, threshold boundary, and invalid-path scenario must have a failing domain test before production logic is accepted.
- Approval correctness is preserved through explicit threshold checks, staged approval flow above 500,000, and strict no-mutation behavior on rejected attempts.
- API and security consistency remains intact because the existing bearer-token session model and role checks are preserved.
- Minimal scope is maintained by reusing the existing `ApprovalService`, repository pattern, and xUnit conventions.
- Verification is required through unit tests, Playwright validation, and mutation testing before the feature is accepted.

## Existing Authentication Model and Role Representation

The app currently uses a session-based bearer-token flow:
- `SessionAuthMiddleware` validates an `Authorization: Bearer ...` token per API call.
- `SessionStore` holds in-memory `UserSession(username, role)` entries.
- `AuthController` signs users in by username/password and stores the resulting role in the session.
- `RequireRole` enforces role checks without replacing the session model or login flow.

The repo currently contains demo users such as `demo.approver` and `demo.admin`, but the feature specification requires `Manager` and `Director` roles. The plan is to represent the assignment's approval roles through the same runtime auth architecture, not by replacing it:
- add or configure corresponding demo role values in the project’s existing user configuration,
- keep the runtime identity as a session-scoped `UserSession` with a role string,
- validate authorization through the same `RequireRole` pattern and the same HTTP auth middleware,
- use the same mechanism for tests by creating valid bearer-token sessions with `Manager` and `Director` roles when exercising the approval endpoints.

This preserves the existing architecture while making the workflow logic testable and enforceable in the same way the repo already handles auth.

## Project Structure

### Documentation (this feature)

```text
specs/001-approval-workflow/
├── spec.md
├── plan.md
├── research.md                  # required if implementation research uncovers edge conditions
├── data-model.md                # planned domain-state documentation if stage tracking is added
├── quickstart.md                # planned local validation notes
├── contracts/                   # only if API contract shapings need explicit documentation
└── tasks.md                     # not created in this planning step and not required here
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── InvoiceApp.Domain/
│   │   ├── ApprovalService.cs
│   │   ├── Invoice.cs
│   │   ├── InvoiceStatus.cs
│   │   └── IInvoiceRepository.cs
│   └── InvoiceApp.Api/
│       ├── Auth/
│       ├── Controllers/
│       ├── InMemoryInvoiceRepository.cs
│       └── Program.cs
├── tests/
│   └── InvoiceApp.Tests/
│       ├── ApprovalServiceTests.cs
│       └── FakeInvoiceRepository.cs
└── InvoiceApp.sln

e2e/
├── pages/
├── tests/
├── package.json
└── playwright.config.ts
```

**Structure Decision**: Keep the feature inside the existing two-project .NET structure; no new project layer is introduced. Business logic and approval invariants stay in the domain project, and the API/test layers validate the behavior through the existing auth and test architecture.

## Implementation Phases

### Phase 0: Test-first domain rules

Goal: lock the business rules down before implementation.

Planned work:
- Add failing xUnit tests for all threshold boundaries and negative cases in `ApprovalServiceTests`.
- Cover manager approval at exactly 50,000 and rejection at 50,001.
- Cover director approval at exactly 500,000 and rejection above 500,000 without the required manager stage.
- Cover repeated decisions, invalid status transitions, and rejection without a reason.
- Cover no-mutation on rejected approval and invalid role attempts.

Exit criteria:
- The relevant tests fail before the production logic is implemented.
- Each test explicitly states the invariant being enforced.
- No rule is accepted without a failing test or an existing regression test that would fail if the invariant were violated.

### Phase 1: Single-stage threshold implementation commit

Goal: implement the single-stage business rules in a dedicated commit with matching tests.

Planned domain logic:
- Manager final approval is valid at `<= 50,000`.
- Manager final approval is rejected above 50,000.
- Director final approval is valid for amounts above 50,000 and up to and including 500,000.
- Director final approval above 500,000 is rejected unless a valid manager first-stage approval already exists.
- Rejected attempts leave the invoice state unchanged, including status, final approver, timestamps, and history.

Planned tests:
- valid manager approval at 50,000,
- invalid manager approval at 50,001,
- valid director approval at 500,000,
- invalid director approval above 500,000 without manager stage,
- role mismatch and blank rejection reasons,
- state preservation after rejected attempts.

Commit plan:
- Commit 1: `test: add single-stage approval thresholds and negative cases`
- Commit 2: `feat: enforce manager and director single-stage approval rules`

This keeps the threshold work separate from the multi-stage workflow work.

### Phase 2: Two-stage approval flow above 500,000 commit

Goal: implement the manager-first / different-director-second flow in a separate commit.

Planned domain logic:
- For invoices above 500,000, require a Manager first-stage approval before a Director final-stage approval can be considered.
- The second-stage approver must be a different user from the first-stage approver.
- Same-user attempts are rejected.
- Missing or out-of-order stages are rejected without mutation.
- The invoice remains pending and retains stage history until a valid completion occurs.

Planned tests:
- manager first-stage success followed by different director final-stage success,
- second-stage attempt before manager stage is rejected,
- same-user satisfaction of both stages is rejected,
- final-stage rejection preserves existing stage history,
- final approval after a prior stage still preserves prior records.

Commit plan:
- Commit 3: `test: add multi-stage approval flow boundaries and same-user protections`
- Commit 4: `feat: enforce manager-first then director-final approval flow above 500k`

This satisfies the requirement to keep the single-stage and two-stage implementations in separate commits while ensuring each commit carries the relevant tests.

### Phase 3: Playwright E2E validation

Goal: keep E2E scope tight and relevant to the assignment.

Planned E2E scenario:
- Create a UI flow that creates or loads an invoice above the manager limit.
- Log in as a Manager.
- Attempt a final approval through the UI for an over-limit invoice.
- Verify the page shows a clear rejection reason such as “Manager approval is only valid up to 50,000” or the equivalent assignment-appropriate message.
- Verify the invoice is still recorded as pending and that any approval history remains unchanged.

This is intentionally limited to a single rejection-path scenario to keep scope aligned with the assignment and to avoid unnecessary UI expansion.

Planned E2E assertions:
- approval action is attempted via the existing application UI,
- error message is visible to the user,
- the invoice remains in the pre-attempt state,
- no final approver or decision timestamp is recorded after the rejected action.

This test is specifically designed to capture the no-mutation requirement for rejected over-limit approvals.

### Phase 4: Mutation testing and final verification

Goal: ensure the domain logic remains robust against equivalent mutations and that the assignment’s evidence is captured in text form.

Required installation command:

```bash
dotnet tool install -g dotnet-stryker --version 4.13.0
```

Reproducible run command for the relevant project:

```bash
dotnet stryker --project backend/src/InvoiceApp.Domain/InvoiceApp.Domain.csproj --reporter progress --reporter json --output specs/001-approval-workflow/mutation
```

Capture plan for before/after scores:
- Run the above command before increasing rule coverage and redirect the console output to a text file, for example:
  `dotnet stryker --project backend/src/InvoiceApp.Domain/InvoiceApp.Domain.csproj --reporter progress --reporter json --output specs/001-approval-workflow/mutation > specs/001-approval-workflow/mutation-before.txt 2>&1`
- After the tests and implementation are complete, rerun the same command and save the output to:
  `specs/001-approval-workflow/mutation-after.txt`
- Compare the resulting mutation scores from both files and record the delta in the final assignment summary or PR review notes.

The goal is not to claim a passing score before the command is run; it is to preserve reproducible evidence of the before/after result.

## Planned Delivery and Verification Checklist

The feature is not accepted until the following assignment deliverables are in place and evidence is recorded.

### 1. Unit tests for every business rule
- Add xUnit tests for every approval threshold, negative rule, and state-preservation case.
- Include tests for manager above threshold, director above threshold, missing manager stage, same-user second-stage attempt, invalid role attempts, repeated final decisions, and blank rejection reasons.
- Verify that every rejected attempt leaves the invoice unchanged.

### 2. Playwright E2E test
- Add a focused E2E test for a rejected over-limit manager approval through the UI.
- Assert a clear rejection message is visible.
- Assert the invoice remains unchanged and pending, with no final decision recorded.

### 3. Before/after mutation scores as text
- Save mutation output before and after implementation to text files.
- Record the mutation score difference and any surviving mutants that were intentionally retained only with supporting tests.

### 4. AI interaction log
- Retain the repository-compatible Copilot/spec-kit interaction log and review evidence in the assignment notes.
- Record the workflow used, important clarifying questions addressed, and the final plan review decisions.

### 5. PR self-review
- Perform a self-review before completion.
- For each finding, mark it as `fixed`, `dismissed with reason`, or `deferred with reason`.
- The review must explicitly cover the domain-layer boundary, role model, approval thresholds, and no-mutation behavior.

### 6. Repository-root assignment write-up
- Create or update `ASSIGNMENT.md` at the repo root with the implementation write-up and any required narrative summary.
- Include the video link and a brief explanation of how the app or tests were validated.

### 7. Demo video
- Record an 8–10 minute video showing the application or tests running live.
- The video must demonstrate the relevant manager/director approval rules and the rejection behavior for an over-limit invoice.

## AGENTS.md Planned Deliverable

This plan includes an explicit repository guidance update as a deliverable, even though no code changes are made in this planning step.

The AGENTS.md update must explicitly include:
- the approval rules and threshold boundaries,
- the domain-layer ownership boundaries,
- the requirement that rules and regressions are covered by xUnit tests before acceptance,
- the expected validation commands,
- the repo’s review expectations,
- and the mutation-test command and pinned Stryker.NET version.

This change is part of the assignment deliverables and is not a claim of implementation completion.

## Assumptions and Risks

### Assumptions
- The current auth model remains the source of truth for runtime authorization and login behavior.
- The project’s existing demo users are extended to represent the role model required by the feature, without replacing the bearer-token session system.
- Rejected attempts are no-ops with respect to invoice state and history.
- The threshold logic follows the approved feature specification rather than any undocumented business exceptions.

### Risks
- Risk: stage tracking may be under-modeled if the domain records only the final decision. Mitigation: treat stage progression as part of the domain state and verify stage history and no-mutation behavior with tests.
- Risk: controllers may accidentally own business rule logic. Mitigation: keep authorization and request validation in the API layer while preserving all approval semantics in `ApprovalService`.
- Risk: role enforcement could drift from the session model. Mitigation: keep the demo role model aligned to the auth middleware and configuration and test the role checks end-to-end.
- Risk: mutation tests may reveal uncovered rules. Mitigation: add or tighten tests to cover every surviving mutant before the feature is accepted.

## Documentation and Review Notes

- The approved feature spec remains the source of truth for business behavior.
- The repo constitution remains the source of truth for architecture and verification standards.
- This plan distinguishes planned work from work already completed and avoids claiming any verification has passed before execution.
- The final set of assignment artifacts must include the recorded text evidence, the self-review, the assignment write-up, and the demonstration link.

## Complexity Tracking

No repository architecture violation is expected. The planned work stays within the existing domain-layer architecture, session-based auth model, xUnit suite, and Playwright E2E layer.
