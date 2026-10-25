# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only, approvals are Approver or Admin.
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Approval business rules

- The invoice approval workflow must remain in the Domain layer; controllers and `Program.cs` are not allowed to contain business logic.
- A Manager may finally approve an invoice up to and including 50,000.
- A Director may finally approve an invoice above 50,000 and up to and including 500,000.
- An invoice above 500,000 requires two distinct approvers. For this project, the agreed workflow interpretation is that a Manager performs the first approval stage and a different Director performs the second and final approval stage.
- The same user must never satisfy both stages for the same invoice.
- The second-stage Director approval cannot occur before the required Manager stage.
- Final approval is distinct from the first stage of the two-stage workflow. Do not imply that the assignment explicitly defines the first approver's role for invoices above 500,000; identify Manager-first as this project's agreed interpretation.
- Rejected approval attempts must return a clear, useful reason and must not mutate the invoice's status, final approver, decision timestamp, or relevant prior approval history.
- Invalid roles, invalid amounts, invalid lifecycle transitions, duplicate decisions, and incomplete or out-of-order approval attempts must be handled consistently with the specification.
- Business rules must preserve the invoice history and state correctly when a decision is rejected, including the unchanged prior approval stages and final decision metadata.

## Test-first workflow

- Write or update unit tests before implementing each rule.
- Run `dotnet test backend/InvoiceApp.sln` and review the failing output before and after each implementation step.
- Cover exact boundaries and edge cases: 50,000; 50,001; 500,000; above 500,000; valid roles; invalid roles; stage ordering; same-user attempts; invalid lifecycle transitions; and state/history preservation.
- Verify that rejected attempts leave the invoice unchanged.
- Review every generated test for meaningful assertions; do not add tests that merely repeat implementation details.
- Add the required Playwright validation for a Manager's rejected over-limit approval attempt through the UI, verifying the rejection message and unchanged invoice state.
- Keep domain-rule tests separate from API authorization tests where appropriate.
- Do not treat a failing or rejected approval path as successful just because the exception or message was raised; confirm the invoice state remains unchanged.

## Architecture and authorization guardrails

- Inspect the existing authentication/session handling and `RequireRole` logic before changing authorization behavior.
- Keep endpoint role restrictions consistent with the implemented approval workflow.
- Add or update demo users only through the existing configuration/authentication mechanism; do not replace the current authentication architecture.
- Avoid new dependencies unless necessary and justified.
- Do not hardcode real credentials, tokens, or customer data.
- Preserve the existing layering: `backend/src/InvoiceApp.Domain` holds business rules and the invoice lifecycle; `backend/src/InvoiceApp.Api` holds controllers, authentication, in-memory repository, static UI, and session logic; `backend/tests/InvoiceApp.Tests` holds xUnit tests; `e2e` holds Playwright tests and page objects.
- Preserve `ApiControllerBase.Handle(...)` for error mapping and the current role-based endpoint authorization model.

## Mutation testing

- Use the pinned Stryker version and do not upgrade it.
- Install the tool if needed: `dotnet tool install -g dotnet-stryker --version 4.13.0`
- Run the unit tests before mutation analysis: `dotnet test backend/InvoiceApp.sln`
- Run Stryker from the test project directory using the validated repository invocation: `dotnet stryker --reporter progress --reporter json --output ..\..\..\specs\001-approval-workflow\mutation`
- Require a baseline mutation run before changing application code or tests, followed by a final mutation run after implementation.
- Record the actual console output and JSON reports, compare the before/after scores, and never invent a score or mark a failed run as successful.
- If mutation testing fails or the reports are incomplete, treat the run as unsuccessful until the cause is understood and the evidence is recorded.

## AI workflow, review, and delivery expectations

- Follow the committed feature specification, plan, and task list under `specs/001-approval-workflow/`.
- Implement in small, test-first stages rather than one giant prompt.
- Use separate meaningful commits for the single-stage Manager/Director threshold rules and the greater-than-500,000 two-stage workflow.
- Review generated changes critically; fix or document each review finding with a clear verdict.
- Maintain the required AI interaction log and record the actual tools and actions used.
- Keep `ASSIGNMENT.md` at the repository root updated with the required write-up, actual mutation results, review findings/verdicts, and demo-video link.
- Include a live 8–10 minute demo showing meaningful execution, such as tests, mutation testing, or the Playwright test.
- Do not claim that a test, review, commit, video, or deliverable exists unless it has actually been completed and verified.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`
- Install Stryker if needed: `dotnet tool install -g dotnet-stryker --version 4.13.0`
- Run Stryker from the test project directory: `dotnet stryker --reporter progress --reporter json --output ..\..\..\specs\001-approval-workflow\mutation`

## Demo accounts (not real credentials)

- `demo.approver` / `ChangeMe123!` (role Approver)
- `demo.admin` / `ChangeMe123!` (role Admin)

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.
