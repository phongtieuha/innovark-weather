using System.Net;
using System.Text;

namespace Innovark.Weather.IntegrationTests;

/// <summary>
/// Replaces the network for the Open-Meteo typed client: returns a canned response, throws, or never
/// responds. Tests never touch the real API.
/// </summary>
internal sealed class StubOpenMeteoHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    private int _requestCount;

    /// <summary>How many requests reached Open-Meteo; used where the response cannot show it.</summary>
    public int RequestCount => _requestCount;

    public static StubOpenMeteoHandler Json(HttpStatusCode statusCode, string body) =>
        new(_ => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        }));

    public static StubOpenMeteoHandler Fixture() =>
        Json(HttpStatusCode.OK, File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "open-meteo-archive-2026-09-27_28.json")));

    public static StubOpenMeteoHandler Throws(Exception exception) =>
        new(_ => Task.FromException<HttpResponseMessage>(exception));

    /// <summary>Waits until the request is cancelled, so HttpClient's timeout fires.</summary>
    public static StubOpenMeteoHandler NeverResponds() =>
        new(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("Unreachable.");
        });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requestCount);
        return respond(cancellationToken);
    }
}
