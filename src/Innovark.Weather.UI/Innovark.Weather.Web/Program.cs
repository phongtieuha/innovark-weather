using System.Text.Json.Serialization;
using Innovark.Weather.Web.ApiClients;
using Innovark.Weather.Web.Endpoints;
using Innovark.Weather.Web.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddOpenApi();

// Numbers must be JSON numbers, not strings like "14". ASP.NET's web defaults accept both, which would
// make every number `number | string` in the OpenAPI document and the client Orval generates from it.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddWeatherApiClient();

var app = builder.Build();

// In every environment: GlobalExceptionHandler turns the weather API's errors into the responses
// the page shows, so they must not become Development's exception page.
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStatusCodePages();   // bodiless error responses (e.g. 404) get a ProblemDetails body

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();   // /openapi/v1.json; also written to openapi.json on each Debug build
}

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapWeatherEndpoints();

await app.RunAsync();
