# Feature Specification: Approval Workflow

**Feature Branch**: `001-approval-workflow`

**Created**: 2026-10-22

**Status**: Draft

**Input**: User description: "Managers may finally approve invoices up to 50,000. Directors may finally approve invoices up to 500,000. For invoices above 500,000, a Manager provides the first-stage approval, and a different Director provides the second-stage approval that finalizes the invoice. The same user cannot satisfy both approval stages. Invalid approval attempts must return a clear reason and leave the invoice unchanged."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Routine invoice approval by a manager (Priority: P1)

A manager needs to finalize an invoice that is within the manager approval limit without escalating to a director. The workflow must be clear, auditable, and easy to verify when the amount is at the threshold boundary.

**Why this priority**: This is the primary business path for daily invoice processing and must behave correctly at exact threshold boundaries before any exception path is considered.

**Independent Test**: A manager can finalize an invoice exactly at the manager limit and the invoice records a clear final decision without ambiguity.

**Acceptance Scenarios**:

1. **Given** an invoice in Pending Approval with an amount of exactly 50,000, **When** a manager submits a final approval, **Then** the invoice is approved and the approving manager is recorded.
2. **Given** an invoice in Pending Approval with an amount of 50,001, **When** a manager submits a final approval, **Then** the request is rejected with a clear reason and the invoice remains unchanged.
3. **Given** an invoice in Pending Approval with an amount below 50,000, **When** a manager submits a final approval, **Then** the invoice is approved as the manager’s final decision.

---

### User Story 2 - Director approval for larger invoices (Priority: P1)

A director needs to finalize invoices that are within the director approval limit. The process must ensure that only the correct role can decide at that threshold and that invalid role attempts are rejected without mutating the invoice.

**Why this priority**: Director approvals involve larger values and require explicit role enforcement to avoid unauthorized final decisions.

**Independent Test**: A director can finalize an invoice at 500,000, while non-director users or out-of-range approvals are rejected with a clear reason and no invoice mutation.

**Acceptance Scenarios**:

1. **Given** an invoice in Pending Approval with an amount of exactly 500,000, **When** a director submits a final approval, **Then** the invoice is approved and the director is recorded as the final approver.
2. **Given** an invoice in Pending Approval with an amount above 500,000, **When** a director attempts a final approval without the required first-stage manager approval, **Then** the request is rejected with a clear reason and the invoice remains unchanged.
3. **Given** an invoice in Pending Approval with an amount above 500,000, **When** a user without the Director role attempts the final approval, **Then** the request is rejected with a clear reason and no invoice fields are changed.

---

### User Story 3 - Multi-stage approval for very large invoices (Priority: P1)

A very large invoice requires a two-stage approval path: a manager completes the first stage, and a different director completes the second stage to finalize the invoice. The same user cannot satisfy both stages, and rejected attempts must not alter approval history.

**Why this priority**: This is the highest-risk approval scenario, and it requires explicit separation of duties and strict state preservation to prevent invalid progression.

**Independent Test**: A manager and a different director can complete the required stages for a large invoice, while same-user, missing-stage, or out-of-order stage attempts are rejected without mutation.

**Acceptance Scenarios**:

1. **Given** an invoice above 500,000 that is pending a two-stage approval flow, **When** a manager records the first-stage approval and a different director records the second-stage final approval, **Then** the invoice is finalized successfully and the stage history reflects the two different approvers.
2. **Given** an invoice above 500,000, **When** a second-stage approval is attempted before the first-stage manager approval is recorded, **Then** the attempt is rejected with a clear reason and the invoice remains unchanged.
3. **Given** an invoice above 500,000, **When** the same user attempts to satisfy both stages, **Then** the second-stage attempt is rejected and the invoice’s status, stage records, final approver, decision timestamp, and decision note history remain unchanged.
4. **Given** an invoice above 500,000, **When** the required manager stage is recorded but a different director fails to complete final approval, **Then** the invoice remains pending and the prior stage record remains intact until a valid final approval is submitted.

---

### User Story 4 - Invalid approval attempts, lifecycle errors, and state preservation (Priority: P2)

An approver needs immediate feedback whenever the approval action is invalid. The system must explain the reason, preserve the invoice state, and keep rejection behavior distinct from approval so the business can correct the action confidently.

**Why this priority**: Invalid approval attempts are high-risk from a governance perspective because they can create confusion, duplicate decisions, or silent state drift if they are not clearly blocked.

**Independent Test**: Approval and rejection attempts that do not meet the current lifecycle, role, or data requirements are rejected without mutating the invoice’s recorded state.

**Acceptance Scenarios**:

1. **Given** an invoice that is not in Pending Approval, **When** an approver attempts to approve or reject it, **Then** the system rejects the attempt with a clear reason and preserves the invoice’s status, approver, timestamp, and decision note history.
2. **Given** an invoice that has already received a final approval or rejection, **When** a repeated approval or rejection attempt is submitted, **Then** the system rejects the attempt and leaves the invoice decision unchanged.
3. **Given** an invoice pending approval, **When** a user submits a rejection without a reason, **Then** the system rejects the rejection without saving a decision or mutating the invoice.
4. **Given** a user without the required role for the current stage, **When** they attempt to complete that approval stage, **Then** the action is rejected with a clear reason and no invoice fields are changed.
5. **Given** a rejected approval attempt, **When** the system evaluates the resulting state, **Then** the invoice retains the same status, approval-stage records, final approver, decision timestamp, and decision note/history that existed before the rejected attempt.

---

### Expected boundary and state-preservation behaviors

- Exact threshold values are accepted only where the rule allows them: a Manager final approval is valid at 50,000, and a Director final approval is valid at 500,000.
- Amounts above the manager threshold are rejected for Manager final approval; amounts above the director threshold are rejected for Director final approval unless the required Manager first-stage approval has already been recorded.
- Invoices above 500,000 follow the two-stage rule: a Manager must complete stage one, a different Director must complete stage two, and the same user cannot satisfy both stages.
- Any rejected approval or rejection attempt leaves the invoice unchanged, including status, approval-stage records, final approver, decision timestamp, and decision note/history that existed before the rejected attempt.
- Repeated decisions after a final outcome has already been recorded, out-of-order stage attempts, invalid-role attempts, and blank rejection reasons are rejected without mutating the invoice or its existing approval history.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a Manager to provide a final approval for an invoice of exactly 50,000.
- **FR-002**: The system MUST allow a Director to provide a final approval for any invoice above 50,000 and up to and including 500,000.
- **FR-003**: The system MUST reject a Manager final approval attempt for any invoice above 50,000.
- **FR-004**: The system MUST reject a Director final approval attempt for any invoice above 500,000 unless the required Manager first-stage approval has already been recorded.
- **FR-005**: The system MUST require a Manager first-stage approval before a Director can finalize an invoice above 500,000.
- **FR-006**: The system MUST require a different approver identity for the final Director stage than the Manager who completed the earlier stage.
- **FR-007**: The system MUST reject any approval attempt where the same user would satisfy both approval stages for the same invoice.
- **FR-008**: The system MUST reject any approval attempt by users without the required role for that stage, with a clear reason and no invoice mutation.
- **FR-009**: The system MUST reject approval attempts against invoices that are not in Pending Approval without mutating the invoice.
- **FR-010**: The system MUST reject repeated approval or rejection attempts after a final decision has already been recorded without changing the recorded decision.
- **FR-011**: The system MUST reject any rejection attempt that does not include a valid reason without saving the decision.
- **FR-012**: For any rejected approval or rejection attempt, the system MUST preserve the invoice’s status, approval-stage records, final approver, decision timestamp, and decision note/history that existed before the rejected attempt.
- **FR-013**: The system MUST maintain a clear distinction between approval and rejection outcomes so that final states remain unambiguous.
- **FR-014**: The system MUST preserve the existing authentication and authorization model while enforcing the approval workflow rules.

### Key Entities *(include if feature involves data)*

- **Invoice**: The business record being reviewed, including amount, status, approval stages, and decision history.
- **Approver**: A person or role-based actor who can take part in an approval action.
- **Approval Stage**: A specific step in the approval sequence, including the Manager first-stage approval and the Director final-stage approval.
- **Approval Decision**: The final outcome of the invoice review, including approval, rejection, the actor, and the reason when applicable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of Manager final approval attempts at 50,000 are accepted, and 100% of Manager final approval attempts above 50,000 are rejected with a clear reason and no invoice mutation.
- **SC-002**: 100% of Director final approval attempts at 500,000 are accepted, and 100% of Director final approval attempts above 500,000 are rejected unless a valid Manager first-stage approval has already been recorded.
- **SC-003**: 100% of approval flows above 500,000 require a Manager first-stage approval and a different Director second-stage approval before finalization occurs.
- **SC-004**: 100% of same-user, out-of-order, or missing-stage approval attempts are rejected, and the invoice remains unchanged for every rejected attempt.
- **SC-005**: 100% of invalid-role approval attempts are rejected with a clear reason and do not modify the invoice status, stage records, final approver, decision timestamp, or decision note/history.
- **SC-006**: 100% of repeated approval attempts after a final decision and blank rejection attempts are rejected without changing the recorded decision.
- **SC-007**: Users can understand the approval outcome and the reason for rejection without inspecting hidden implementation details or backend state not exposed by the workflow rules.

## Assumptions

- The approval policy is defined by the project’s business rules rather than by ad hoc controller logic.
- Existing role-based authentication and authorization rules remain in effect for all approval actions.
- Approval decisions are evaluated against the invoice amount and current status before any approval is accepted.
- A rejected approval attempt is considered unsuccessful only when the invoice state remains unchanged in all fields relevant to approval status, stage records, final approver, decision timestamp, and decision note/history.
- The detailed feature specification may refine the user-facing labels and messages, but the approval invariants remain non-negotiable.
- The system can be reviewed and validated through the existing unit and workflow test conventions already used in the repository.
