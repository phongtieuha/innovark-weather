# Innovark Weather API

A .NET 10 Web API that returns the last 10 hourly weather records for Ho Chi Minh City from Open-Meteo.

## Getting started

Requires the .NET 10 SDK.

```bash
# Run the API (Scalar UI at /scalar in Development)
dotnet run --project src/Innovark.Weather.Api

# Run the tests
dotnet test
```

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

| Layer | Responsibility | Must not contain |
|---|---|---|
| **Application** | Request validation (not in the future, not older than 72 hours, fixed UTC+7), the weather history use case (10-hour window, completeness check, °C→°F, newest first), response models, and the `IOpenMeteoClient` interface | `HttpClient`, `HttpContext`, Open-Meteo's JSON format |
| **Infrastructure** | The typed `HttpClient` for Open-Meteo: builds the query, reads the JSON response, parses upstream errors, options with startup validation, resilience policies | Business rules or HTTP request handling |
| **Api** | Minimal API endpoints, snake_case JSON, OpenAPI docs, exceptions mapped to ProblemDetails (400/502/503/500), dependency registration, health check, hosting settings for ECS | Business rules or Open-Meteo details; endpoints only bind input, call the service and return the result |
