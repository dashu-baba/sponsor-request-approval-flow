# Testing Best Practice Rules

A practical rulebook for testing the app (xUnit, Testcontainers, React Testing Library).

## What to Test

### 1. Test behavior, not implementation
Assert observable outcomes and contracts, not private internals. Tests should survive a refactor
that preserves behavior.

### 2. Concentrate on business rules and risk
Prioritize the logic that matters: the workflow state machine, RBAC/ownership rules, validators,
and money/date handling. Don't chase coverage on trivial getters.

### 3. Pick the right test level (test pyramid)
Many fast unit tests for domain/application logic; fewer integration tests for API + DB wiring;
a thin layer of end-to-end where it adds confidence. Don't invert the pyramid.

### 4. Unit-test pure logic without infrastructure
Domain and application rules should be testable with no database, HTTP, or cloud — inject fakes
for ports. If a rule needs a DB to test, the design is leaking.

### 5. Integration-test real wiring against real dependencies
Test API flows against a real database via Testcontainers + `WebApplicationFactory`. Cover the
full happy path (Draft → Approved) plus reject/cancel and a concurrency conflict (409).

## How to Write Tests

### 6. One reason to fail per test
Each test verifies one behavior. Many focused tests beat one test with many assertions.

### 7. Arrange–Act–Assert
Structure tests clearly: set up, perform the action, assert the outcome. Keep setup minimal and
intention-revealing.

### 8. Name tests by behavior
`Approve_WhenNotManager_ReturnsForbidden` reads as a spec. The name states scenario + expectation.

### 9. Assert with intent
Use expressive assertions (FluentAssertions) so failures explain what was expected vs actual.
Avoid asserting on incidental details.

### 10. Test the negative and edge cases
Invalid transitions, wrong role, missing ownership, boundary amounts, past event dates, empty
results — not just the happy path.

### 11. Use builders/factories for test data
Centralize object creation with sensible defaults and per-test overrides. Avoid copy-pasted setup.

## Quality of the Test Suite

### 12. Keep tests deterministic
No reliance on wall-clock time, random data, ordering, or network. Inject a clock; seed randomness;
freeze time where behavior depends on it.

### 13. Keep tests independent and isolated
Tests must not depend on each other or shared mutable state. Each integration test gets a clean
database state (fresh container or per-test transaction/reset).

### 14. Make tests fast and parallel-safe
Fast unit tests run constantly. Keep integration tests isolated so they can run in parallel without
cross-talk.

### 15. Don't over-mock
Mock external boundaries (storage, third-party APIs), not the system under test. Over-mocking tests
the mocks, not the code. Prefer real objects for in-process collaborators.

### 16. Avoid logic in tests
No loops/conditionals computing the expected value the same way as production — that hides bugs.
Hard-code expected values.

### 17. Treat tests as production code
Tests are reviewed, formatted, linted, and refactored to the same standard as `src`. Delete dead
tests; fix flaky ones — never ignore them.

### 18. Tests gate merges in CI
The suite runs in CI on every PR and must be green to merge. A failing or skipped required test
blocks the merge.

## Frontend Testing

### 19. Test components the way users use them
Query by role/label/text, interact, and assert what the user sees. Don't reach into component
internals or snapshot huge DOMs.

### 20. Validate the four UI states
Exercise loading, empty, error, and success for data-driven views — not only the happy path.

## Senior Rule

Test behavior at the right level, cover the rules and the failure paths, keep the suite fast,
deterministic, and isolated, and let green CI be the gate for merge.
