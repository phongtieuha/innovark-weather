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

## Testing strategy

Replace only what is outside our control: the network and the clock. Everything else runs as real code, including JSON parsing, the `HttpClient` pipeline, options validation and DI registration.

| Level | Project | Replaced | Runs |
|---|---|---|---|
| Unit | `Innovark.Weather.UnitTests` | Open-Meteo via `StubHttpMessageHandler` + fixture; the clock via `FakeTimeProvider`; in service tests, `IOpenMeteoClient` via `FakeOpenMeteoClient` | Always |
| Integration | `Innovark.Weather.IntegrationTests` | Only the Open-Meteo handler and the clock; the real `Program.cs`, routing, JSON and ProblemDetails run in memory | Always |
| Live contract | `OpenMeteoClientLiveTests` | Nothing: the real API | Only on request (`--explicit only`), never in CI |

Stub the `HttpMessageHandler`, not the client, so the real client code (URL, query, status handling, JSON) is exercised. Use the saved real response in `Fixtures/` for normal data and small inline JSON for edge cases a real response rarely contains.

## Test cases to cover

For each method or endpoint, consider every category below that applies. Most bugs in this API are at boundaries, around time zones, and in upstream data.

| Category | What to test | Examples in this repo |
|---|---|---|
| Happy path | The normal case returns the expected values, not just "no exception" | 10 records with the fixture's real values |
| Boundaries | Exactly at each limit, and one step past it on both sides | Exactly 72h is valid, 73h is not; the current hour is valid, the next is not; hours 0 and 23 vs -1 and 24 |
| Invalid input | Each rule fails on its own, with the field name and message asserted | `hour` errors in ValidationProblem shape |
| Time and "now" | A fixed clock; the result changing as time moves on (`FakeTimeProvider.Advance`) | Valid now, too old 36 hours later |
| Time zones | Midnight in UTC+7 while UTC is still the previous day; inputs given in UTC give the same result; every output time carries `+07:00` | Requests at 00:10 +07:00; windows crossing midnight request both local days |
| Upstream errors | Non-2xx with and without a JSON body, invalid JSON, missing fields, mismatched array lengths, wrong UTC offset | `OpenMeteoException` with status and reason |
| Upstream data quality | Nulls, missing hours, duplicate hours, a shifted window, rows in any order | `IncompleteWeatherDataException`; newest first regardless of input order |
| Numbers and rounding | Midpoints, rounding direction, negative zero, whole numbers sent as decimals | 31.25 °C → 88.3 °F; 57.5% → 58; -17.78 °C → 0 °F, not -0 |
| Extreme values | Minimum and maximum of the input types must return errors, not throw | `DateOnly.MinValue`, `DateOnly.MaxValue` |
| Registration | Services resolve; lifetimes are correct; test doubles registered earlier are kept | Service is scoped; `TryAdd` keeps a `FakeTimeProvider`; full container builds with `ValidateOnBuild` |

Test results first: assert what a method returns or throws. Check interactions (which dependencies were called, with what) only when the result cannot show the behavior. For example, `FakeOpenMeteoClient.Calls` shows that an invalid request never reaches Open-Meteo, although the result is `Invalid` either way.

Assert exact values: prefer `ShouldBe(expected)` on the whole result over `ShouldNotBeNull()`. Records compare by value, so a single assertion covers every field.

After writing tests for tricky logic, check that they catch bugs: break the code on purpose (for example use UTC instead of UTC+7, drop the trimming, or count rows instead of checking exact hours), confirm the right tests fail, then restore it. A suite that stays green on broken code gives false confidence.

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
- Tests never depend on the real date. Inject `TimeProvider` in code. Unit tests use `FakeTimeProvider`. Integration tests use `FixedNowTimeProvider`, which freezes "now" but keeps real timers, because the resilience pipeline takes the same `TimeProvider` from DI and `FakeTimeProvider` would freeze its timeouts and retry delays.
- Tests against the real Open-Meteo API are `[Fact(Explicit = true)]` with `[Trait("Category", "Live")]`, so they only run on request.
- Registration methods (`AddApplication`, `AddInfrastructure`) have their own `DependencyInjectionTests`.

Run `./scripts/run-tests.sh` before committing; the build treats warnings as errors.
