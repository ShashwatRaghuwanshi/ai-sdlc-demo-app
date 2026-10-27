# Tasks: Approval Workflow

**Input**: Design documents from `/specs/001-approval-workflow/`

**Prerequisites**: plan.md (required), spec.md (required), constitution.md (required), and the current repo architecture

**Tests**: This task plan explicitly includes test-first work for the approval logic and E2E validation. The tasks below are intentionally conservative: source artifacts and historical evidence may exist in the repo, but task completion is treated as provisional unless a matching run artifact or terminal output is present.

**Organization**: Tasks are grouped by user story to enable independent implementation and validation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on unfinished work)
- **[Story]**: Which user story this task belongs to (for example, [US1], [US2], [US3], [US4])
- Include exact file paths in descriptions

## Phase 1: Setup and Baseline Validation

**Purpose**: Establish the baseline and capture the verification evidence before changing tests or production code.

- [ ] T001 [P] Review the approved feature spec, plan, and constitution in `specs/001-approval-workflow/spec.md`, `specs/001-approval-workflow/plan.md`, and `.specify/memory/constitution.md` and confirm the business rules remain unchanged
- [ ] T002 Run the existing baseline suite `dotnet test backend/InvoiceApp.sln` and capture the output as the current pass/fail baseline before any new approval logic is implemented; if the command fails, record the exit code and the failing output and do not invent a score or a pass result
- [ ] T003 Install the pinned mutation-testing tool `dotnet tool install -g dotnet-stryker --version 4.13.0` and document the exact command in the project notes
- [x] T004 Run the baseline mutation test command `dotnet stryker --project backend/src/InvoiceApp.Domain/InvoiceApp.Domain.csproj --reporter progress --reporter json --output specs/001-approval-workflow/mutation` and save the console output to `specs/001-approval-workflow/mutation-before-console.txt`; if the command produces a JSON report, save it to `specs/001-approval-workflow/mutation-before.txt` and record the actual score only when the tool produces it; otherwise, record the failure output and exit status without inventing a score
- [ ] T005 Record the pre-change baseline indicator in the assignment notes: whether the baseline ran successfully, any failing output, the actual score only when available, surviving mutants, and any test gaps identified before implementation begins
- [ ] T006 Confirm the current bearer-token session auth model remains the only source of runtime authorization in `backend/src/InvoiceApp.Api/Auth/SessionAuthMiddleware.cs`, `SessionStore.cs`, `UserSession.cs`, and `backend/src/InvoiceApp.Api/Controllers/AuthController.cs` before changing role-based approval flows

**Checkpoint**: Baseline evidence is captured and no implementation work begins without the current test and mutation baseline saved.

---

## Phase 2: Foundational Tests and Role/Domain Boundaries

**Purpose**: Establish the failing-tests baseline for the approved business rules, keep API auth validation distinct from domain logic, and preserve the existing bearer-token/session architecture.

- [ ] T010 Review `backend/src/InvoiceApp.Api/appsettings.json` and confirm how the existing demo users map to the assignmentâ€™s Manager and Director role model without replacing the bearer-token/session architecture
- [ ] T011 Review `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`, `backend/src/InvoiceApp.Api/Auth/SessionAuthMiddleware.cs`, `SessionStore.cs`, and `UserSession.cs`; confirm that runtime authorization remains distinct from domain business rules and that `RequireRole` continues to be the enforcement layer
- [ ] T012 Add the shared failing domain invariant setup in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for the approval-state contract before any rule-specific implementation begins: identify the invoice lifecycle state, stage ordering expectations, and the required no-mutation and approval-history invariants without duplicating the scenario-specific tests owned later by the user-story phases
- [ ] T013 Add the shared failing API-auth tests that exercise the existing session-based authorization model separately from the domain rules: valid/invalid bearer-token authentication and role-based rejection through the current controller/session flow while keeping these tests distinct from `ApprovalService` business-rule tests
- [ ] T014 Document the exact domain-state invariants before implementation: final approval rules, required stage order above 500,000, distinct approver identity, preserved history on rejected attempts, and no mutation of invoice state or prior approval history

**Checkpoint**: The failing tests cover every approved threshold, negative rule, role boundary, and state-preservation invariant before production logic is implemented.

---

## Phase 3: User Story 1 - Manager Approval at the Threshold (Priority: P1) ðŸŽ¯ MVP

**Goal**: Enforce the single-stage Manager rule at 50,000 without broadening scope beyond the approved business logic.

**Independent Test**: A manager can finalize an invoice at exactly 50,000 and a manager cannot finalize an invoice above 50,000.

### Tests for User Story 1

- [ ] T020 [US1] Add and run the failing manager threshold tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for the manager approval boundary: exactly 50,000 is accepted, amounts below 50,000 are accepted as manager final approval, and amounts above 50,000 are rejected with a clear reason
- [ ] T021 [US1] Add and run the failing manager no-mutation test in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` to confirm a rejected over-limit attempt does not change invoice status, final approver, decision timestamp, or approval history

### Implementation for User Story 1

- [x] T022 [US1] Implement the Manager single-stage approval rule in `backend/src/InvoiceApp.Domain/ApprovalService.cs` and keep the logic in the domain layer, not the API or UI layer
- [x] T023 [US1] Preserve invoice state on any rejected manager approval attempt so status, final approver, timestamp, and approval history remain unchanged
- [ ] T024 [US1] Re-run the focused manager tests after implementation and capture the pass/fail output before the single-stage commit boundary; if the tests fail, fix the rule and rerun until the focused test set passes

**Checkpoint**: The manager rule is independently testable and committed as separate work from the >500,000 flow.

---

## Phase 4: User Story 2 - Director Approval Rules and Role Enforcement (Priority: P1)

**Goal**: Enforce the single-stage director rule, preserve the role boundary, and keep authorization checks distinct from domain business rules.

**Independent Test**: A director can finalize an invoice above 50,000 and up to 500,000, while a non-director user or out-of-range case is rejected without mutating the invoice.

### Tests for User Story 2

- [ ] T030 [US2] Add and run the failing director threshold tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for the director approval boundary: exactly 500,000 is accepted, amounts between 50,001 and 500,000 are accepted only by a director, and amounts above 500,000 are rejected unless the required earlier manager approval is present
- [ ] T031 [US2] Add and run the failing invalid-role and no-mutation tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` to confirm role mismatches and rejection without reason do not mutate invoice state or approval history

### Implementation for User Story 2

- [x] T032 [US2] Implement the Director single-stage approval rule in `backend/src/InvoiceApp.Domain/ApprovalService.cs` while preserving the approved threshold interpretation
- [ ] T033 [US2] Keep authorization checks in the existing bearer-token/session flow and the controller-level role enforcement in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`; do not move business-rule logic into the API layer
- [ ] T034 [US2] Re-run the focused director and invalid-role tests after implementation and capture the output before the single-stage commit boundary; if any fail, fix and rerun until the focused set passes

**Checkpoint**: Director approval and role-boundary behavior are independently testable and committed as part of the single-stage threshold work.

---

## Phase 5: User Story 3 - Above 500,000 Two-Stage Approval Flow (Priority: P1)

**Goal**: Enforce the manager-first and different-director-second workflow without widening the scope beyond the approved business rules.

**Independent Test**: A manager and a different director can finalize a large invoice while same-user, missing-stage, or out-of-order stage attempts are rejected without mutation.

### Tests for User Story 3

- [ ] T040 [US3] Add and run the failing two-stage tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for the manager-first and different-director-second approval flow: the manager first stage succeeds, a different director can finalize the second stage, and the same user cannot satisfy both stages
- [ ] T041 [US3] Add and run the failing two-stage negative tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for second-stage before first-stage, out-of-order stage attempts, and preserved stage history after a rejected final-stage attempt

### Implementation for User Story 3

- [x] T042 [US3] Implement the `> 500,000` manager-first / different-director-second approval flow in `backend/src/InvoiceApp.Domain/ApprovalService.cs` and keep all stage sequencing inside the domain layer
- [x] T043 [US3] Preserve the prior manager stage record until a valid final approval is submitted and reject same-user or out-of-order attempts without mutating invoice state or existing approval history
- [ ] T044 [US3] Re-run the focused two-stage tests after implementation and capture the output before the multi-stage commit boundary; if any fail, fix and rerun until the focused set passes

**Checkpoint**: The >500,000 approval flow is independently testable and committed separately from the single-stage threshold rules.

---

## Phase 6: User Story 4 - Invalid Lifecycle and State Preservation (Priority: P2)

**Goal**: Close the final negative cases and enforce no-mutation semantics across all rejected attempts.

**Independent Test**: Approval and rejection attempts that do not meet the current lifecycle, role, or stage requirements are rejected without mutating the invoice.

### Tests for User Story 4

- [ ] T050 [US4] Add and run the failing invalid-lifecycle tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for invoices not in pending approval, repeated final outcomes, and blank rejection reasons
- [ ] T051 [US4] Add and run the failing state-preservation tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` to prove rejected attempts keep the invoice status, approver, timestamp, decision history, and stage records unchanged

### Implementation for User Story 4

- [x] T052 [US4] Enforce invalid lifecycle guards in `backend/src/InvoiceApp.Domain/ApprovalService.cs` so final decisions remain unambiguous and state-preserving
- [ ] T053 [US4] Review the domain-level no-mutation semantics across manager, director, and multi-stage scenarios and confirm the API layer remains a thin contract adapter rather than the source of business rules
- [ ] T054 [US4] Re-run the focused invalid-lifecycle and no-mutation tests after implementation and capture the output before the final review stage

**Checkpoint**: Final invalid-path and state-preservation coverage is complete and isolated to the domain logic, with API authorization kept separate.

---

## Phase 7: Playwright E2E, Authorization Coverage, and Final Validation

**Purpose**: Keep the UI validation narrowly scoped to the assignment and maintain a clear separation between runtime auth validation and domain business-rule tests.

- [x] T060 Add a focused Playwright E2E test under `e2e/tests/` for an over-limit invoice rejection through the app UI and keep the scope limited to the required business scenario
- [x] T061 Add the supporting UI helper or page object under `e2e/pages/` for login, invoice selection, and approval action execution without expanding UI behavior beyond the assignment
- [x] T062 In the E2E test, log in with a valid Manager role or equivalent runtime role, attempt a final approval for an invoice above the manager limit, and assert the visible rejection reason is clear and the invoice remains unchanged
- [x] T063 In the E2E test, verify the UI records no final approver, no final decision timestamp, and no change to the invoiceâ€™s recorded state after the rejected action
- [x] T064 Keep the E2E auth path distinct from business rules by using the existing bearer-token/session architecture and the projectâ€™s current demo-user model rather than bypassing the runtime role checks
- [ ] T065 Confirm the Playwright validation command remains `cd e2e && npm install && npx playwright install chromium && npm test` and capture the exact command used in the assignment notes
- [x] T066 Run the final unit-test suite `dotnet test backend/InvoiceApp.sln` after the implementation and capture the console output; do not mark it passing until the terminal output confirms the result
- [ ] T067 Run the final mutation command after implementation and capture the console output to `specs/001-approval-workflow/mutation-after-console.txt` using `dotnet stryker --project backend/src/InvoiceApp.Domain/InvoiceApp.Domain.csproj --reporter progress --reporter json --output specs/001-approval-workflow/mutation`; if the command produces a JSON report, save it to `specs/001-approval-workflow/mutation-after.txt`; if it fails, record the failure output and exit status without inventing a score
- [ ] T068 Compare the baseline and final mutation evidence captured in the actual before/after console and report files; record the mutation score delta and any surviving mutants only when actual values are present; otherwise clearly state that the run failed and no score was produced
- [ ] T085 Review the evidence set already produced by T004, T067, and T068 and record the before/after mutation comparison, the unit-test result, and any honest failure details; do not rerun the mutation command or unit suite for this step unless a new evidence need is identified

**Checkpoint**: The focused E2E flow, the unit suite, and both mutation checkpoints are recorded as evidence and remain explicitly pending until executed.

---

## Phase 8: Commit Strategy and Implementation Boundaries

**Purpose**: Keep the implementation reviewable and separated by business-rule phase while ensuring at least three meaningful commits overall.

- [ ] T070 Create a commit boundary after the baseline evidence and the shared failing-domain setup are in place, so the threshold-rule work is isolated from the existing code and any score capture is preserved before production logic is changed
- [ ] T071 Commit the Manager/Director single-stage threshold implementation and its matching tests as one meaningful, reviewable change, with the commit message reflecting the threshold logic and negative cases
- [ ] T072 Create a second commit boundary before the `> 500,000` manager-first/director-second flow implementation
- [ ] T073 Commit the multi-stage approval flow and its matching tests as a separate meaningful change, with explicit stage ordering, distinct approver identity, and same-user protections in the message
- [ ] T074 Create a third distinct commit boundary after the E2E validation, documentation, or assignment-review work is ready so the repo has a separate review/deliverable checkpoint beyond the two business-rule implementation commits
- [ ] T075 Keep the final review, documentation, or assignment-artifact commit distinct from the business-rule implementation commits and ensure it is only created after the implementation and validation evidence are complete

**Checkpoint**: There are at least three meaningful, independently reviewable commits, with the single-stage and two-stage flows intentionally separated and a third distinct test/review/documentation checkpoint included.

---

## Phase 9: Assignment Deliverables and Final Verification

**Purpose**: Capture the required artifact set and verification evidence without claiming success before the commands are run.

- [x] T080 Update `AGENTS.md` as an explicit planned deliverable with the approval thresholds, domain-layer ownership, test-first rule, validation commands, review expectations, and Stryker.NET 4.13.0 instructions
- [ ] T081 Write or update the repo-root `ASSIGNMENT.md` with the implementation write-up, summary of the business rules, test coverage, mutation evidence links, and the final demo-video link
- [ ] T082 Capture the AI interaction log and store the relevant repository-compatible evidence for the assignment summary and PR review notes
- [ ] T083 Run a PR self-review and mark each finding as `fixed`, `dismissed with reason`, or `deferred with reason` in the assignment notes, covering the domain boundary, role model, threshold logic, and no-mutation behavior
- [ ] T084 Record the 8â€“10 minute live-demo video showing the app or tests running and include the link in `ASSIGNMENT.md`
- [ ] T085 Review the final evidence set already produced by T004, T067, and T068 and record the before/after mutation comparison, the unit-test result, and any honest failure details; do not rerun the mutation command or unit suite for this step
- [ ] T086 Summarize the final deliverables, dependencies, unresolved risks, and any remaining clarifications for the assignment review
- [ ] T087 Verify that the final verification note accurately reflects the actual terminal output from the recorded unit-test and UI validation commands and does not claim success without evidence

**Checkpoint**: All required assignment artifacts are present; no verification claim is asserted before the terminal output is reviewed.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No dependencies; captures the baseline before implementation.
- **Phase 2**: Depends on Phase 1 and establishes the domain and auth boundaries.
- **Phase 3**: Depends on Phase 2 and covers the first approved business rule: manager threshold behavior.
- **Phase 4**: Depends on Phase 2 and covers director threshold behavior and role enforcement.
- **Phase 5**: Depends on Phase 2 and covers the >500,000 multi-stage approval flow.
- **Phase 6**: Depends on Phase 3, 4, and 5 and closes the invalid lifecycle and no-mutation cases.
- **Phase 7**: Depends on the approval logic and server-side fixes and validates the UI path specifically for rejected over-limit approvals.
- **Phase 8**: Depends on the implementation boundaries being stable and ensures commit reviewability.
- **Phase 9**: Depends on all business logic and validation tasks and is the final artifact and evidence stage.

### User Story Dependencies

- **US1**: Can start after Phase 2; delivers the manager threshold behavior.
- **US2**: Can start after Phase 2; covers the director threshold and role checks.
- **US3**: Can start after Phase 2; covers the multi-stage large-invoice flow.
- **US4**: Depends on the approval rules in US1-US3 and closes invalid-lifecycle and no-mutation behavior.

### Parallel Opportunities

- Baseline setup tasks in Phase 1 can run in parallel.
- Review and role-mapping tasks in Phase 2 can run in parallel if the implementation work is split across the auth and domain boundaries.
- Within each user-story phase, the failing tests for related scenarios can be added in parallel before the implementation tasks.
- Final assignment-artifact tasks in Phase 9 can run in parallel once the logic and validation are complete.

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 baseline and Phase 2 domain/auth boundaries.
2. Complete Phase 3 manager threshold logic.
3. Complete Phase 4 director threshold logic.
4. Validate the single-stage rules with unit tests before moving to the large-invoice flow.
5. Complete Phase 5 multi-stage flow.
6. Complete Phase 6 invalid lifecycle and state-preservation behavior.
7. Complete the focused E2E workflow in Phase 7.
8. Finish the final assignment artifact and review tasks in Phase 9.

### Incremental Delivery

- Deliver the manager and director threshold rules in separate commits from the >500,000 manager-first/director-second flow.
- Treat the over-limit UI rejection and mutation evidence as a dedicated validation milestone.
- Keep the feature scope stable and aligned with the approved specification rather than expanding into additional UI or architecture work.

### Parallel Team Strategy

With multiple contributors:

1. One contributor handles the domain test and `ApprovalService` logic.
2. One contributor handles the auth/demo-user alignment and API review.
3. One contributor handles the E2E test and the final assignment artifact setup.
4. Another contributor handles the mutation-score capture and the PR self-review.

---

## Notes

- [P] tasks are parallelizable and can be split across files or workstreams.
- Each task includes explicit file paths so the work remains reviewable and aligned with the repo layout.
- The implementation remains in the domain project and the existing session-based auth model; no application code or protected test files are altered outside the planned implementation steps.
- This file intentionally marks work as incomplete; no verification is claimed to have passed before execution.

