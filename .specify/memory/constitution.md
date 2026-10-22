# Invoice Approval Demo Constitution

<!--
Sync Impact Report
Version change: 1.0.0 → 1.1.0
Modified principles:
- I. Domain-Driven Business Rules → I. Business Logic Layering
- II. Role-Secured API Access → IV. API and Security Consistency
- III. Test-First Delivery → II. Test-First Development
- IV. Verified User Experience → VI. Verification and Review
- V. Simplicity and Change Safety → V. Minimal, Maintainable Changes
Added principles:
- III. Approval Correctness
- VI. Verification and Review (expanded from review expectations)
Added sections:
- Operational Standards
- Delivery Workflow
Removed sections:
- None
Deferred items:
- TODO(RATIFICATION_DATE): confirm the original adoption date for the project constitution before final ratification.
-->

## Core Principles

### I. Business Logic Layering
The invoice lifecycle, approval transitions, threshold checks, and state mutations MUST live in InvoiceApp.Domain, especially in ApprovalService. API controllers and UI code MAY accept input and display outcomes, but they MUST NOT define business rules or decide whether an invoice is valid to approve.

This preserves the existing architecture and makes the approval policy explicit, reviewable, and testable without coupling business logic to the web layer.

### II. Test-First Development
Every business rule and boundary condition MUST have meaningful unit tests before implementation is considered complete. AI-generated tests are not accepted as proof of correctness unless the tests are reviewed for rule coverage and the code would fail if the relevant invariant were violated.

The project’s xUnit suite is the primary enforcement mechanism for domain behavior, and any new approval rule must be expressed as a testable invariant rather than an inferred implementation detail.

### III. Approval Correctness
Approval logic MUST be explicit, testable, and invariant-based. Invoices must produce clear, reviewable approval outcomes for manager and director flows, multi-stage approval, distinct approver identities, and rejection without invoice mutation.

The detailed approval thresholds and stage sequencing are left to the feature specification, but the implementation MUST preserve these invariants: managers may finally approve invoices up to 50,000; directors may finally approve invoices up to 500,000; for amounts above 500,000, a manager provides the first-stage approval and a different director provides the second-stage final approval; the same user cannot satisfy both approval stages; and invalid approval attempts return a clear reason and leave the invoice unchanged.

### IV. API and Security Consistency
All API behavior MUST preserve the existing authentication, role-based authorization, and error-mapping conventions in InvoiceApp.Api. Controllers must continue to inherit from ApiControllerBase, use the established Handle(...) flow for service errors, and remain behind sign-in and role enforcement.

This prevents drift between the contract and the security model and keeps the demo consistent with the repository’s intended architecture.

### V. Minimal, Maintainable Changes
Changes MUST reuse the existing domain/service boundaries, repository conventions, and test patterns. The project must avoid unnecessary dependencies, unrelated refactoring, or broader architecture churn when a small, reviewable change will satisfy the requirement.

This protects the demo’s simplicity and makes the repo easier to reason about during AI-in-SDLC exercises and reviews.

### VI. Verification and Review
Before any rule is treated as accepted, the project MUST run dotnet test backend/InvoiceApp.sln, review the resulting diff, and document findings rather than assuming generated code is correct. Fresh evidence is the default standard for accepting behavior changes.

A change is not validated by generation alone; it is validated when the relevant tests and review artifacts demonstrate the rule is enforced in the codebase.

## Operational Standards

The invoice approval demo MUST remain aligned with the following standards:

- InvoiceApp.Domain owns business rules and repository contracts; InvoiceApp.Api owns controllers, authentication, and UI assets.
- Repositories are interfaces in the domain project with in-memory implementations in the API project for local, single-process execution.
- Controllers MUST inherit from ApiControllerBase and route using the conventional api/<controller> pattern.
- Request records are declared in the controller and used only to shape API input; no business logic is allowed there.
- Every approval rule and boundary condition MUST be covered by a test that would fail if the rule were violated.
- Approval thresholds and multi-stage paths must be explicit in the feature specification and validated with tests before completion.
- Invalid approval attempts MUST return a clear reason and leave the invoice unchanged; no mutation is allowed on a rejected approval attempt.
- Naming MUST be explicit and readable; one-letter variables and vague flag parameters are prohibited.
- Demo credentials are for local use only and MUST never be treated as real production credentials or committed as sensitive secrets.

## Delivery Workflow

Changes to this project MUST follow the repository workflow described in AGENTS.md and the project README:

- Business rules are implemented in the domain project before the API layer is updated.
- Authentication and authorization requirements are enforced before endpoints are considered complete.
- Tests are required for every new rule, endpoint, or regression fix.
- Manual verification is expected for the approval flow and role behavior when a user-facing change is made.
- Pull requests and code review MUST confirm that the change respects the domain boundary, the role model, and the project’s simple architecture.

## Governance

This Constitution supersedes informal practices for this repository. All project changes MUST be evaluated against it before being considered complete.

Amendments require a clear rationale, updated versioning, and a review of the impacted principles and workflow sections. The project MUST record the change in this constitution, note the reason for the amendment, and retain a traceable history of the decision.

Versioning MUST follow semantic versioning: MAJOR changes remove or redefine governance rules in a backward-incompatible way, MINOR changes add or materially expand a principle or section, and PATCH changes clarify wording or refine non-semantic guidance. A change that adds a governance requirement or materially narrows allowed behavior is a MAJOR or MINOR change based on impact.

Compliance review is mandatory before a change is considered ready. Reviewers MUST verify that the behavior, tests, and documentation remain aligned with the domain rule, the role model, and the project’s simplicity standard.

**Version**: 1.1.0 | **Ratified**: TODO(RATIFICATION_DATE): confirm the original adoption date for the project constitution before final ratification. | **Last Amended**: 2026-10-22
