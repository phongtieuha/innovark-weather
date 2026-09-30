# Innovark Weather API

A .NET 10 Web API that returns the last 10 hourly weather records for Ho Chi Minh City from Open-Meteo.

## Getting started

Requires the .NET 10 SDK.

```bash
# Run the API on http://localhost:5122 (Scalar UI at /scalar)
./scripts/run-api.sh

# Run the tests without live
./scripts/run-tests.sh

# Run the tests with live
./scripts/run-tests-with-explicit.sh
```

The scripts work from any directory. Without bash, for example on Windows, run the same commands from the repository root:

```powershell
# Run the API on http://localhost:5122 (Scalar UI at /scalar)
dotnet run --project src/Innovark.Weather.Api --launch-profile http

# Run the tests without live
dotnet test

# Run the tests with live
dotnet test -- --explicit on
```

| Script                                 | Runs                                                                                                                    |
| -------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `./scripts/run-api.sh`                 | The API in Development on `http://localhost:5122`, with the Scalar UI at `/scalar`. Extra arguments go to `dotnet run`. |
| `./scripts/run-tests.sh`               | All offline tests. Explicit tests, such as the live Open-Meteo test, are skipped.                                       |
| `./scripts/run-tests-with-explicit.sh` | All tests, including explicit ones. Needs network access.                                                               |

The test scripts pass extra arguments to `dotnet test`, for example `./scripts/run-tests.sh -c Release`.

## Architecture

```
src/
├── Innovark.Weather.Api/             # HTTP in: endpoints, error mapping, composition root
├── Innovark.Weather.Application/     # Business rules and use cases; defines the interfaces it needs
└── Innovark.Weather.Infrastructure/  # External systems: implements those interfaces (Open-Meteo)
tests/
├── Innovark.Weather.UnitTests/         # Application and Infrastructure, no network
└── Innovark.Weather.IntegrationTests/  # The full API in memory via WebApplicationFactory
```

Dependencies point inward:

```
Api ──► Infrastructure ──► Application
 └──────────────────────────►┘
```

`Application` references no other project and no HTTP or ASP.NET packages, so the compiler enforces the boundary.

| Layer              | Responsibility                                                                                                                                                                                                              | Must not contain                                                                                        |
| ------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| **Application**    | Request validation (not in the future, not older than 72 hours, fixed UTC+7), the weather history use case (10-hour window, completeness check, °C→°F, newest first), response models, and the `IOpenMeteoClient` interface | `HttpClient`, `HttpContext`, Open-Meteo's JSON format                                                   |
| **Infrastructure** | The typed `HttpClient` for Open-Meteo: builds the query, reads the JSON response, parses upstream errors, options with startup validation, resilience policies                                                              | Business rules or HTTP request handling                                                                 |
| **Api**            | Minimal API endpoints, snake_case JSON, OpenAPI docs, exceptions mapped to ProblemDetails (400/502/503/500), dependency registration, health check, hosting settings for ECS                                                | Business rules or Open-Meteo details; endpoints only bind input, call the service and return the result |

## Resilience and caching

### Calling Open-Meteo

The Open-Meteo client uses .NET's standard resilience handler (`Microsoft.Extensions.Http.Resilience`). Its settings are in `OpenMeteo:Resilience` in `appsettings.json`, so they can be tuned per environment, for example with `OpenMeteo__Resilience__Retry__MaxRetryAttempts` on ECS.

| Setting         | Value                                                  | Why                                                                                                            |
| --------------- | ------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------- |
| Total timeout   | 15 s                                                   | The longest a caller waits, including retries                                                                  |
| Attempt timeout | 5 s                                                    | One slow attempt doesn't use the whole budget                                                                  |
| Retry           | 2 retries, exponential backoff with jitter from 500 ms | The request is an idempotent GET, so retrying is safe. Few retries, so a struggling upstream isn't overloaded. |
| Circuit breaker | 30 s sampling, 15 s break                              | After repeated failures, requests fail fast instead of waiting on timeouts                                     |

- **Only transient failures are retried:** 5xx, 408, 429, connection errors and timeouts. A 400 from Open-Meteo means the request is wrong and won't succeed on retry.
- **Failures map to clear status codes:** Open-Meteo errors and incomplete data return **502**, while timeouts, an open circuit and connection failures return **503**, all as ProblemDetails.
- **A missing `Resilience` section falls back to the library defaults** instead of failing at startup.

### Caching

Responses are cached in memory with `HybridCache` for **30 minutes** per location and requested hour.

- **Why 30 minutes, not forever:** for recent hours, the Historical Weather API serves provisional model data (ECMWF IFS) until ERA5 reanalysis replaces it about 5 days later, so values can still change.
- **Only complete data is cached.** If Open-Meteo returns missing hours or null values, the request fails with 502, nothing is stored, and the next request tries again.
- **Stampede protection:** when several requests for the same hour arrive together, only one calls Open-Meteo. The others wait for that fetch and get its result; if it fails, they all get the error and nothing is cached. A test sends 20 simultaneous requests and checks that Open-Meteo is called once.
- **Visible to clients:** every successful response has an RFC 9211 `Cache-Status` header, either `innovark-weather; hit` or `innovark-weather; fwd=miss`. Only the request that called Open-Meteo reports `fwd=miss`; requests that waited for its fetch report `hit`.
- **Horizontal ECS scaling:** each instance has its own in-memory cache. Registering an `IDistributedCache`, such as Redis on ElastiCache, adds a shared second level without code changes.

### Testing it

- Integration tests use a clock that freezes only "now" (`FixedNowTimeProvider`). The resilience pipeline uses the same `TimeProvider`, and a fully fake clock would stop its timeouts and retry delays, so requests would hang.
- The tests shorten the resilience settings to milliseconds, and cover: retries, no retry on 400, timeouts, the circuit opening, cache hits and misses, and incomplete data not being cached.

## Editor

This project is developed in [Visual Studio Code](https://code.visualstudio.com/). Required extensions:

| Extension                                                                                 | Used for                                                                                                  |
| ----------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) | C# editing, the Solution Explorer, and running and debugging tests                                        |
| [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)      | Sending the sample requests in `src/Innovark.Weather.Api/Innovark.Weather.Api.http` with **Send Request** |

The shared workspace settings in `.vscode/settings.json` turn on Explorer file nesting, for example grouping `appsettings.*.json` under `appsettings.json`. Any editor that supports the .NET 10 SDK works; the build and tests run from the command line. Visual Studio and JetBrains Rider can send `.http` requests without an extension.

## Disclaimer

GitHub Copilot is used in this project.
