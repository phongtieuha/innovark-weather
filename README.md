# Innovark Weather API

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4) ![C# 14](https://img.shields.io/badge/C%23-14-239120)

A .NET 10 Web API that returns 10 hourly weather records for Ho Chi Minh City (10.762622, 106.660172) from Open-Meteo's [Historical Weather API](https://open-meteo.com/en/docs/historical-weather-api): the requested hour and the 9 hours before it, newest first. The date and hour are given in UTC+7 and may be up to 3 days in the past. Each record has the temperature in °C and °F and the relative humidity.

**Contents:** [Demo](#demo) · [Getting started](#getting-started) · [Approach](#approach) · [API](#api) · [Web](#web) · [Design decisions](#design-decisions) · [Architecture](#architecture) · [Resilience and caching](#resilience-and-caching) · [Logging](#logging) · [Testing](#testing) · [Container](#container) · [Code quality](#code-quality) · [Security](#security) · [Further considerations](#further-considerations) · [Editor](#editor) · [Disclaimer](#disclaimer)

## Demo

[▶ Watch the demo](https://github.com/user-attachments/assets/c1db315a-a219-4d57-bcb0-9857a5c5d36e): starting the API, successful requests with different test cases, cache hit/miss, and validation errors.

[▶ Watch the demo](https://github.com/user-attachments/assets/01e12a76-a3d8-4491-8a4d-bfb0dee43647): Web UI, form validation, separate web api.

## Getting started

Requires the .NET 10 SDK, or only Docker to run the container.

```bash
# Run the API on http://localhost:5122 (Scalar UI at /scalar)
./scripts/run-api.sh

# Run the tests without live
./scripts/run-tests.sh

# Run the tests with live
./scripts/run-tests-with-explicit.sh

# Run the web app on http://localhost:5048, with the API it calls (needs bun; installs packages on first run)
./scripts/run-web.sh

# Or run both in containers: the web app on http://localhost:5048, the API on http://localhost:5122
# (Development, Scalar UI at /scalar)
docker compose up --build

# Production settings: the API only inside the compose network (JSON logs, no Scalar)
docker compose -f docker-compose.yml up --build
```

The scripts work from any directory. Without bash, for example on Windows, run the same commands from the repository root:

```powershell
# Run the API on http://localhost:5122 (Scalar UI at /scalar)
dotnet watch run --project src/Innovark.Weather.Api --launch-profile http

# Run the tests without live
dotnet test

# Run the tests with live
dotnet test -- --explicit on
```

`run-api.sh` uses `dotnet watch`, so code changes are hot-reloaded. Extra arguments go to `dotnet watch run` or `dotnet test`, for example `./scripts/run-tests.sh -c Release`.

## Approach

This project follows a test-driven mindset with slogan as _"Code is cheap now, but quality is not."_ Every piece of code is written together with its tests: no feature, endpoint or fix is added without them.

## API

`GET /api/v1/weather/history?date=2026-09-28&hour=14`, where `date` and `hour` (0–23) are in UTC+7. The requested hour must be no later than the current hour and no more than 72 hours before it.

```json
{
  "location": { "latitude": 10.762622, "longitude": 106.660172, "utc_offset": "+07:00" },
  "requested_time": "2026-09-28T14:00:00+07:00",
  "records": [
    { "current_time": "2026-09-28T14:00:00+07:00", "temperature_c": 33.1, "temperature_f": 91.6, "relative_humidity": 57 },
    …
    { "current_time": "2026-09-28T05:00:00+07:00", "temperature_c": 25.3, "temperature_f": 77.5, "relative_humidity": 98 }
  ]
}
```

`records` always has 10 hours: the requested hour first, then the 9 before it. °F is calculated from °C. Responses have a `Cache-Status` header (`hit` or `fwd=miss`).

Errors are [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) ProblemDetails with a `trace_id`:

| Status | When                                                                                                                                |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| 400    | `hour` in the future, more than 72 hours old, or outside 0–23 (under `errors.hour`); or a missing or malformed parameter            |
| 404    | Unknown route                                                                                                                       |
| 502    | Open-Meteo returned an error or unusable data (invalid JSON, missing fields, an unexpected offset), or not all 10 hours with values |
| 503    | Open-Meteo timed out or couldn't be reached, or the circuit breaker is open                                                         |
| 500    | Unexpected error; details are logged, not returned                                                                                  |

Also: `GET /health`, and in Development `GET /scalar` (API docs) and `GET /openapi/v1.json`. Sample requests are in `src/Innovark.Weather.Api/Innovark.Weather.Api.http`. Each Debug build also writes the OpenAPI document to `src/Innovark.Weather.Api/openapi.json`, so API changes show up as a diff; the web app writes its own `openapi.json` next to its `.csproj`. Neither is copied to the build output or published.

## Web

A one-page web app for the same request: a form with a date and an hour (UTC+7), validated in the browser with the API's rules, and a **Send** button. It calls the web app's own endpoint, `GET /api/weather/history?date=…&hour=…`, which calls the API and returns its 10 records in camelCase. The API's errors come back as ProblemDetails with the same status, so the page shows the API's validation messages; an unreachable API is a 503.

```bash
./scripts/run-web.sh            # Vite dev server (:5173), the web app (:5048) and the API (:5122)
./scripts/generate-api-clients.sh   # after an endpoint change: regenerate the Kiota and Orval clients

cd src/Innovark.Weather.UI
bun install                                          # once: installs the three workspace packages
bun run --cwd Innovark.Weather.Storybook storybook   # Storybook on http://localhost:6006
bun run --cwd Innovark.Weather.Storybook test        # every story as a test, in headless Chromium
bun run format:check                                 # Prettier, for the three packages only
```

| Package                       | What it is                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Innovark.Weather.Web`        | ASP.NET Core Razor Pages host. Each page (`Pages/Home/Home.cshtml`) mounts a React app from the `.ts` file of the same name: from the Vite dev server in Development, and from the hashed bundle in `wwwroot/js/build` otherwise (a Release build runs `bun run build`).                                                                                                                                                                                                                             |
| `Innovark.Weather.Components` | `@innovark-weather/components`: [shadcn/ui](https://ui.shadcn.com) components with the default theme and fonts, a `DatePicker`, and form helpers: `ControlledField` (a react-hook-form field with a collapsing error), `fetchApiAsync` (the fetch function for Orval's hooks: throws ProblemDetails on errors), `toProblemState` (field errors and the alert message from a ProblemDetails), `createQueryClient`, and `useFormAction` (the result alert). Shipped as TSX source, with no build step. |
| `Innovark.Weather.Storybook`  | Stories for every component. API calls are answered by MSW, with one story per outcome (200, 400, 500, network error). Every story runs as a test, with WCAG 2 A/AA checks that fail it on a violation.                                                                                                                                                                                                                                                                                              |

### API clients

Both hops use a client generated from an OpenAPI document, so request, response and error types always match the C# code. `generate-api-clients.sh` rebuilds both documents and regenerates both clients; the generated code is committed, so a change shows up as a diff.

| Hop                      | Generator                                                                                                                                                                    | Generated into                                                              | From                                    |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- | --------------------------------------- |
| Web app (server) → API   | [Kiota](https://learn.microsoft.com/openapi/kiota/), pinned as a local tool in `.config/dotnet-tools.json`                                                                   | `Innovark.Weather.Web/WeatherApi` (settings in its `kiota-lock.json`)       | `src/Innovark.Weather.Api/openapi.json` |
| Page (browser) → web app | [Orval](https://orval.dev), as a fetch function per endpoint (`getWeatherHistory`, which the page sends with react-query's `useMutation`), react-query hooks, and its models | `Innovark.Weather.Web/client/api/generated` (settings in `orval.config.ts`) | `Innovark.Weather.Web/openapi.json`     |

Don't edit generated code. Every Orval request goes through `fetchApiAsync` (Orval's "mutator", `client/api/fetch-api.ts`), so hooks resolve to the response body and their `error` is the ProblemDetails; `toProblemState(error)` gives the page its field errors and alert. Paths are resolved against `<base href="~/">`, so the app works under a sub-path.

| Decision                                        | Why                                                                                                                                                                                                                                                                                                     |
| ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Called from the web server, not the browser** | The browser only talks to its own origin: no CORS on the API, and the API's URL stays configuration (`WeatherApi:BaseUrl`)                                                                                                                                                                              |
| **Typed HttpClient from `IHttpClientFactory`**  | `WeatherApiClientFactory` builds Kiota's request adapter on it, so handlers and connection pooling are managed by the factory                                                                                                                                                                           |
| **20-second timeout, no retries**               | Longer than the API's own 15 s budget for Open-Meteo, so the API answers first; the API already retries, so retrying here would multiply calls                                                                                                                                                          |
| **Own response contract**                       | The endpoint maps Kiota's models to its own records, so a change in the API shows up as a compile error in one place                                                                                                                                                                                    |
| **Errors mapped in one place**                  | Endpoints only call the API and return the result; `GlobalExceptionHandler` turns Kiota's exceptions into ProblemDetails (the API's own pass through with the same status, undocumented errors and contract breaks → 502, unreachable or timed out → 503), so every new endpoint gets the same behavior |
| **Strict JSON numbers** in the web app          | ASP.NET's web defaults also accept numbers as strings, which makes every number `number \| string` in the document and the Orval types                                                                                                                                                                  |

The three live in `src/Innovark.Weather.UI` as a bun workspace (`src/Innovark.Weather.UI/package.json`), so the web app and Storybook use the components package directly. The form's validation (zod) mirrors `HistoryRequestValidator`: the hour must be no later than the current hour and no more than 72 hours before it, in UTC+7.

## Design decisions

| Decision                                                    | Why                                                                                                                                                                                                                                                       |
| ----------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Historical Weather API** (`archive-api.open-meteo.com`)   | Named in the task.                                                                                                                                                                                                                                        |
| **Request whole days, then keep the 10 hours**              | The archive API supports only `start_date`/`end_date`; a window crossing midnight UTC requests two days                                                                                                                                                   |
| **Ask in UTC**, `timezone=GMT`, with the window's UTC dates | The archive accepts `end_date` only up to its current UTC date. From 00:00 to 07:00 UTC+7, Vietnam's date is a day ahead of that, so asking for Vietnam's dates would fail for the most recent hours. A response with an offset other than 0 is rejected. |
| **Fixed +07:00 offset** for the output                      | Vietnam has no daylight saving time, and chiseled images have no time zone database. Hours are converted from UTC to +07:00.                                                                                                                              |
| **`DateTimeOffset` and an injected `TimeProvider`**         | The offset is never lost, and tests can fix "now"                                                                                                                                                                                                         |
| **`date` + `hour` input**, not one `datetime`               | As the task specifies; avoids URL-encoding `+07:00` and rounding minutes                                                                                                                                                                                  |
| **72 hours, current hour allowed**                          | "3 days" taken as 72 hours, applied to the requested hour                                                                                                                                                                                                 |
| **Requested hour first, newest first**                      | "Starting from the specified time and counting backwards"                                                                                                                                                                                                 |
| **°F calculated**, rounded half away from zero              | One upstream call, and both values describe the same reading                                                                                                                                                                                              |
| **502 unless exactly the 10 expected hours have values**    | Never return partial data; comparing timestamps also catches gaps, duplicates and shifted windows                                                                                                                                                         |

## Architecture

```
src/
├── Innovark.Weather.Api/             # HTTP in: endpoints, error mapping, composition root
├── Innovark.Weather.Application/     # Business rules and use cases; defines the interfaces it needs
├── Innovark.Weather.Infrastructure/  # External systems: implements those interfaces (Open-Meteo)
└── Innovark.Weather.UI/              # bun workspace for the web front end
    ├── Innovark.Weather.Web/         # Razor Pages + React (Vite) web app and its endpoint
    ├── Innovark.Weather.Components/  # Shared React components (shadcn/ui)
    └── Innovark.Weather.Storybook/   # Stories for the components
tests/
├── Innovark.Weather.UnitTests/         # Application and Infrastructure, no network
├── Innovark.Weather.IntegrationTests/      # The full API in memory via WebApplicationFactory
└── Innovark.Weather.Web.IntegrationTests/  # The web app in memory, with a stub in place of the API
scripts/                                # run-api.sh, run-web.sh, generate-api-clients.sh and the test scripts
Directory.Build.props, Directory.Packages.props, global.json   # shared build settings, package versions, SDK
Dockerfile, Dockerfile.web, docker-compose.yml, docker-compose.override.yml
```

Dependencies point inward:

```
Api ──► Infrastructure ──► Application
 └──────────────────────────►┘
```

`Application` references no other project and no HTTP or ASP.NET packages, so the compiler enforces the boundary. There is no separate Domain project: the service owns no state and has no entities, so it would be empty.

| Layer              | Responsibility                                                                                                                                                                                                              | Must not contain                                                                                        |
| ------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| **Application**    | Request validation (not in the future, not older than 72 hours, fixed UTC+7), the weather history use case (10-hour window, completeness check, °C→°F, newest first), response models, and the `IOpenMeteoClient` interface | `HttpClient`, `HttpContext`, Open-Meteo's JSON format                                                   |
| **Infrastructure** | The typed `HttpClient` for Open-Meteo: builds the query, reads the JSON response, parses upstream errors, options with startup validation, resilience policies                                                              | Business rules or HTTP request handling                                                                 |
| **Api**            | Minimal API endpoints, snake_case JSON, OpenAPI docs, exceptions mapped to ProblemDetails (400/502/503/500), dependency registration, health check                                                                          | Business rules or Open-Meteo details; endpoints only bind input, call the service and return the result |

## Resilience and caching

### Calling Open-Meteo

The Open-Meteo client uses .NET's standard resilience handler (`Microsoft.Extensions.Http.Resilience`). Its settings are in `OpenMeteo:Resilience` in `appsettings.json`, so they can be tuned per environment, for example with the environment variable `OpenMeteo__Resilience__Retry__MaxRetryAttempts`.

| Setting         | Value                                                  | Why                                                                                                            |
| --------------- | ------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------- |
| Total timeout   | 15 s                                                   | The longest a caller waits, including retries                                                                  |
| Attempt timeout | 5 s                                                    | One slow attempt doesn't use the whole budget                                                                  |
| Retry           | 2 retries, exponential backoff with jitter from 500 ms | The request is an idempotent GET, so retrying is safe. Few retries, so a struggling upstream isn't overloaded. |
| Circuit breaker | 30 s sampling, 15 s break                              | After repeated failures, requests fail fast instead of waiting on timeouts                                     |

- **Only transient failures are retried:** 5xx, 408, 429, connection errors and timeouts. A 400 from Open-Meteo means the request is wrong and won't succeed on retry.
- **A missing `Resilience` section falls back to the library defaults** instead of failing at startup.
- **Tested:** a test boots the app with `appsettings.json` alone and checks every value in the table, so a changed value or a misspelled key (which config binding silently ignores) fails the tests. Other tests count the calls that reach Open-Meteo: each transient status and connection errors are retried, 400 and 404 aren't, the total timeout stops retries early, and an open circuit makes no call.

### Caching

Responses are cached in memory with `HybridCache` for **30 minutes** per location and requested hour.

- **Why 30 minutes, not forever:** for recent hours, the Historical Weather API serves provisional model data (ECMWF IFS) until ERA5 reanalysis replaces it about 5 days later, so values can still change.
- **Only complete data is cached.** If Open-Meteo returns missing hours or null values, the request fails with 502, nothing is stored, and the next request tries again.
- **Stampede protection:** when several requests for the same hour arrive together, only one calls Open-Meteo. The others wait for that fetch and get its result; if it fails, they all get the error and nothing is cached. A test sends 20 simultaneous requests and checks that Open-Meteo is called once.
- **Visible to clients:** every successful response has an RFC 9211 `Cache-Status` header, either `innovark-weather; hit` or `innovark-weather; fwd=miss`. Only the request that called Open-Meteo reports `fwd=miss`; requests that waited for its fetch report `hit`.
- **Several instances:** each instance has its own in-memory cache. Registering an `IDistributedCache`, such as Redis, adds a shared second level without code changes.

## Logging

Logs are **structured**: messages are declared with `[LoggerMessage]`, so their values (cache key, status code, elapsed time) become named fields that can be filtered, not just text. The source generator also creates the logging code at build time, and a message template that doesn't match its parameters fails the build.

| Level       | Message                                                                          | Where                    |
| ----------- | -------------------------------------------------------------------------------- | ------------------------ |
| Information | `Cache miss for {CacheKey}; fetched from Open-Meteo in {ElapsedMilliseconds} ms` | `WeatherHistoryService`  |
| Debug       | `Cache hit for {CacheKey}`                                                       | `WeatherHistoryService`  |
| Warning     | `Weather provider failure, returning {StatusCode}: {Reason}` (502/503)           | `GlobalExceptionHandler` |
| Error       | `Unhandled exception.`, with the full exception (500)                            | `GlobalExceptionHandler` |
| Debug       | `Request aborted by the client.`                                                 | `GlobalExceptionHandler` |

.NET also logs each Open-Meteo request and its duration (`IHttpClientFactory`) and each resilience attempt (retries, timeouts, circuit breaker).

### Per environment

|                                    | Development                                     | Other environments                                                                          |
| ---------------------------------- | ----------------------------------------------- | ------------------------------------------------------------------------------------------- |
| Format                             | Readable text with a local `HH:mm:ss` timestamp | JSON, one object per line, UTC ISO 8601 timestamps, with scopes, ready for a log aggregator |
| Project code (`Innovark.*`)        | Debug, so cache hits are visible                | Information; cache hits happen on every repeated request, so they aren't logged             |
| Framework (`Microsoft.AspNetCore`) | Warning                                         | Warning                                                                                     |

Both are set in `appsettings.json` and `appsettings.Development.json` (`Logging:LogLevel` and `Logging:Console`), not in code. Levels can be changed without a rebuild, for example `Logging__LogLevel__Innovark=Debug`.

## Testing

`./scripts/run-tests.sh` runs offline tests; `./scripts/run-tests-with-explicit.sh` adds the live Open-Meteo tests.

| Level       | Replaced                                                                                                                   |
| ----------- | -------------------------------------------------------------------------------------------------------------------------- |
| Unit        | Open-Meteo (a stub handler serving a real saved response), the clock (`FakeTimeProvider`), and the client in service tests |
| Integration | Only Open-Meteo's network and "now"; the real app runs in memory via `WebApplicationFactory`                               |
| Live        | Nothing. They're explicit, so they run only on request: a window across midnight, and one up to the current hour           |
| Web         | Storybook runs every story in Chromium with its play function and a11y checks                                              |

Integration tests freeze only "now", with `FixedNowTimeProvider`. The resilience pipeline takes the same `TimeProvider` for its timeouts and retry delays, which a fully fake clock would stop, so requests would hang. The tests also shorten the resilience settings to milliseconds.

Covered: validation boundaries (72 and 73 hours, midnight in UTC+7, extreme dates), the UTC dates sent to Open-Meteo (including just after midnight in UTC+7), the 10-hour window and newest-first order, upstream errors and bad data, the shipped resilience settings, which failures are retried (408, 429, 5xx and connection errors, but not 400 or 404), attempt and total timeouts, the circuit breaker, caching including 20 simultaneous requests, and every status code.

## Container

The `Dockerfile` builds a multi-stage image on `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`: about >100 MB, no shell, no package manager, running as a non-root user. `Dockerfile.web` builds the web app the same way; its build stage also has bun, since a Release publish runs Vite for the page's bundle.

`docker-compose.yml` runs both as the `innovark-weather` project, with read-only root filesystems, in production settings: only the web app is published, on host port 5048 (the same as `run-web.sh`), and the API has no published port; the web app reaches it at `http://api:8080` inside the compose network. `docker compose up` also merges `docker-compose.override.yml`, which publishes the API on host port 5122 in Development, for Scalar and the `.http` samples; `docker compose -f docker-compose.yml up` leaves it out. The web container always runs in Production, since Development loads the page's scripts from the Vite dev server. It logs that ASP.NET Data Protection keeps its keys in memory (the filesystem is read-only); the app uses no cookies or antiforgery tokens, so nothing depends on them.

Build and check locally:

```bash
docker buildx build --platform linux/amd64 -t innovark-weather-api --load .
docker run -d --name api -p 5122:8080 --read-only --memory=512m --cpus=0.25 innovark-weather-api

curl localhost:5122/health   # Healthy
docker exec api sh           # fails: the image has no shell
docker stop api              # graceful shutdown on SIGTERM
docker logs api | tail -1    # ..."Application is shutting down..."
docker rm api
```

## Code quality

Quality rules run in the build itself, so they apply the same way in every editor and on every machine, and will in CI too. Warnings are treated as errors (`TreatWarningsAsErrors` in `Directory.Build.props`), so the build fails on any warning.

| Check                    | What it covers                                                                                                               | Enforced by                                                   |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------- |
| .NET analyzers           | Correctness, performance, reliability and security rules (`CA*`), at `AnalysisLevel=latest-recommended`                      | `dotnet build`                                                |
| SonarQube rules          | SonarQube's C# rules (`S*`): bugs, code smells and security hotspots, via the `SonarAnalyzer.CSharp` package, with no server | `dotnet build`                                                |
| Nullable reference types | Possible null dereferences (`CS86xx`)                                                                                        | `dotnet build`                                                |
| Vulnerable packages      | Known vulnerabilities in NuGet dependencies (NuGet audit, e.g. `NU1903`)                                                     | `dotnet restore`                                              |
| Formatting               | Indentation, spacing, LF line endings and `using` order from `.editorconfig`; `.gitattributes` keeps LF in git               | VS Code on format, and `dotnet format whitespace` (see below) |
| Tests                    | Unit and integration tests                                                                                                   | `./scripts/run-tests.sh`                                      |

Rule exceptions are made in `.editorconfig`, one rule at a time, with the reason in a comment. Currently there is one: CA1707 is off for `tests/`, so test names can use `Method_Scenario_Expected`.

In the editor, **SonarQube for IDE** shows the same Sonar rules as you type, and C# Dev Kit shows the .NET analyzer warnings.

Check formatting before committing:

```bash
dotnet format whitespace --folder --verify-no-changes --exclude "**/bin/**" "**/obj/**"
```

A Copilot `postToolUse` hook (`.github/hooks/format-csharp.json`) applies the same formatting to C# files Copilot edits.

## Security

The API is public and read-only, with no user data, no database and no secrets, so the attack surface is small. What protects it today:

| Area                            | How it's protected                                                                                                                                                                                            |
| ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Input**                       | `date` and `hour` are strictly typed; `hour` must be 0–23; the requested time must be within the last 72 hours. Extreme dates return errors instead of throwing, and invalid requests never reach Open-Meteo. |
| **Open-Meteo usage is bounded** | The 72-hour window allows about 73 distinct requests per location. With the 30-minute cache and stampede protection, that means at most about 73 Open-Meteo calls per 30 minutes, whatever the traffic.       |
| **No SSRF or injection**        | Callers can't influence the outgoing request. The base URL and coordinates come from configuration, the call uses HTTPS, and every query value is escaped.                                                    |
| **Errors**                      | A 500 returns a generic ProblemDetails with no exception message, type or stack trace; the exception is only logged. A test checks this.                                                                      |
| **Development tools**           | `/scalar` and `/openapi/v1.json` exist only in Development.                                                                                                                                                   |
| **Container**                   | Chiseled image with no shell or package manager, a non-root user, a read-only root filesystem, and few packages.                                                                                              |
| **JSON**                        | Open-Meteo's responses are read with source-generated deserialization into fixed types; no polymorphic or dynamic deserialization.                                                                            |
| **CORS**                        | Not enabled, so browsers on other sites can't call the API.                                                                                                                                                   |

Dependency vulnerability audit and static analysis, including security rules, run in every build (see [Code quality](#code-quality)).

Deliberately left out:

- **Authentication:** the data is public. If usage ever needed controlling, API keys or JWT on `/api/v1` would be the next step.
- **HTTPS in the app:** TLS is expected to end at the load balancer or ingress in front of it, so the app serves HTTP only, with no HTTPS redirection or HSTS.

## Further considerations

**Security**

- **Rate limiting** with `AddRateLimiter`: a per-client limit returning 429. Open-Meteo is already protected by the cache, but the API itself isn't.
- **Automated dependency updates** for NuGet packages, CI tooling and Docker base images, so security updates arrive as pull requests.
- **Reproducible builds:** `packages.lock.json` (`RestorePackagesWithLockFile`), and base images pinned by digest instead of the moving `10.0` tag.
- **Less information in responses:** remove the `Server: Kestrel` header, and return a generic 502 message, keeping Open-Meteo's error text in the logs.
- **Production host configuration:** `AllowedHosts` set to the real host names, and forwarded headers limited to the known proxy network, so the logs and rate limiter see the real client IP.
- **`X-Content-Type-Options: nosniff`** on responses.
- **An SBOM** (software bill of materials) generated in CI.

**Delivery**

- **CI:** a pipeline that builds, runs the tests and checks formatting on every push. It would also build the image, scan it with Trivy (failing on critical and high findings), and run the live Open-Meteo tests nightly, without blocking pull requests.
- **SonarCloud** in CI, for quality-gate reports and test coverage on pull requests.
- **The same SDK everywhere:** pin the SDK feature band in `global.json`, CI and the Dockerfile. The image's newer SDK found an analyzer warning the local SDK didn't.

**Operations**

- **Metrics and tracing:** OpenTelemetry for request rates, latency, cache hit rate and Open-Meteo calls. A hit-rate metric is more useful than logging every cache hit.
- **A shared cache:** Redis as `HybridCache`'s second level when running several instances.
- **Build-time generated code:** options validation, configuration binding and JSON serialization, removing reflection from startup so the API could publish with Native AOT.

**Features**

- **Many locations:** coordinates as a request parameter, prefetching popular locations, and maybe Open-Meteo's FlatBuffers format for large ranges.

## Editor

This project is developed in [Visual Studio Code](https://code.visualstudio.com/). Recommended extensions:

| Extension                                                                                             | Used for                                                                                                      |
| ----------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)             | C# editing, the Solution Explorer, and running and debugging tests                                            |
| [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)                  | Sending the sample requests in `src/Innovark.Weather.Api/Innovark.Weather.Api.http` with **Send Request**     |
| [SonarQube for IDE](https://marketplace.visualstudio.com/items?itemName=SonarSource.sonarlint-vscode) | Shows SonarQube's rules while you type, the same rules the build enforces (see [Code quality](#code-quality)) |

`.vscode/extensions.json` lists them, so VS Code offers to install them when the repository is opened.

The shared workspace settings in `.vscode/settings.json` turn on Explorer file nesting, for example grouping `appsettings.*.json` under `appsettings.json`. Any editor that supports the .NET 10 SDK works; the build and tests run from the command line. Visual Studio and JetBrains Rider can send `.http` requests without an extension.

## Disclaimer

GitHub Copilot is used in this project.
