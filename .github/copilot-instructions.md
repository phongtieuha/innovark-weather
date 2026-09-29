# Copilot instructions

## Every new method or endpoint gets a test

When you add a public or internal method, or an API endpoint, add a test for it in the same change.

- A real test is preferred. If the behavior is not settled yet, a placeholder is fine, but it must show up as skipped, not as a passing test:

  ```csharp
  [Fact(Skip = "Pending: define behavior for incomplete upstream data")]
  public void GetHistoryAsync_IncompleteData_Throws()
  {
  }
  ```

- Replace placeholders with real tests before the feature is considered done; skipped tests must not remain at submission.
- Private helpers are covered through the method that uses them, not tested directly.

## Where tests go

| Code | Tests |
|---|---|
| `src/Innovark.Weather.Application/**` | `tests/Innovark.Weather.UnitTests/Application/**` |
| `src/Innovark.Weather.Infrastructure/**` | `tests/Innovark.Weather.UnitTests/Infrastructure/**` |
| API endpoints (`src/Innovark.Weather.Api/**`) | `tests/Innovark.Weather.IntegrationTests/**` via `WebApplicationFactory<Program>` |

Folders mirror `src/`, with one test class per class under test, named `<Class>Tests`.

## Conventions

- xUnit v3 and Shouldly. Test names follow `Method_Scenario_Expected`, and tests use arrange/act/assert separated by blank lines.
- Pass `TestContext.Current.CancellationToken` to async calls.
- Tests never touch the network. Use `StubHttpMessageHandler` with a fixture from `Fixtures/` for Open-Meteo.
- Tests never use the real clock. Inject `TimeProvider` in code and use `FakeTimeProvider` in tests.
- Tests against the real Open-Meteo API are `[Fact(Explicit = true)]` with `[Trait("Category", "Live")]`, so they only run on request.
- Registration methods (`AddApplication`, `AddInfrastructure`) have their own `DependencyInjectionTests`.

Run `./scripts/run-tests.sh` before committing; the build treats warnings as errors.
