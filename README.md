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

## Editor

This project is developed in [Visual Studio Code](https://code.visualstudio.com/). Required extensions:

| Extension                                                                                 | Used for                                                                                                  |
| ----------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) | C# editing, the Solution Explorer, and running and debugging tests                                        |
| [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)      | Sending the sample requests in `src/Innovark.Weather.Api/Innovark.Weather.Api.http` with **Send Request** |

The shared workspace settings in `.vscode/settings.json` turn on Explorer file nesting, for example grouping `appsettings.*.json` under `appsettings.json`. Any editor that supports the .NET 10 SDK works; the build and tests run from the command line. Visual Studio and JetBrains Rider can send `.http` requests without an extension.

## Disclaimer

GitHub Copilot is used in this project.
