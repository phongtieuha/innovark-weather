using System.Text.Json;
using Innovark.Weather.Api.Endpoints;
using Innovark.Weather.Api.ErrorHandling;
using Innovark.Weather.Application;
using Innovark.Weather.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidation();   // enforces attributes such as [Range] on endpoint parameters

// Missing or malformed parameters throw BadHttpRequestException in every environment (by default only
// in Development), so GlobalExceptionHandler returns the same ProblemDetails with a detail everywhere.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();   // bodiless error responses (e.g. 404) get a ProblemDetails body

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // UI at /scalar
}

app.MapHealthChecks("/health");
app.MapWeatherEndpoints();

app.Run();
