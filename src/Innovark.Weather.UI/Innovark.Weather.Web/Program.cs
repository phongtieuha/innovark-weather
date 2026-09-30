using Innovark.Weather.Web.Endpoints;
using Innovark.Weather.Web.ApiClients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddProblemDetails();
builder.Services.AddWeatherApiClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseStatusCodePages();   // bodiless error responses (e.g. 404) get a ProblemDetails body

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapWeatherEndpoints();

await app.RunAsync();
