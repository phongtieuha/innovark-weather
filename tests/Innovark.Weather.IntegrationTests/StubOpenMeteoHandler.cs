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

    public static StubOpenMeteoHandler Fixture() => Json(HttpStatusCode.OK, ReadFixture());

    /// <summary>Serves the fixture after a real delay, so concurrent requests overlap with the fetch.</summary>
    public static StubOpenMeteoHandler SlowFixture(TimeSpan delay)
    {
        var fixture = ReadFixture();
        return new(async ct =>
        {
            await Task.Delay(delay, ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fixture, Encoding.UTF8, "application/json"),
            };
        });
    }

    /// <summary>Returns each status in turn (the last one repeats), with the fixture as the 200 body.</summary>
    public static StubOpenMeteoHandler Sequence(params HttpStatusCode[] statusCodes)
    {
        var fixture = ReadFixture();
        var next = 0;
        return new(_ =>
        {
            var status = statusCodes[Math.Min(Interlocked.Increment(ref next) - 1, statusCodes.Length - 1)];
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(
                    status == HttpStatusCode.OK ? fixture : """{ "error": true, "reason": "Unavailable" }""",
                    Encoding.UTF8,
                    "application/json"),
            });
        });
    }

    public static StubOpenMeteoHandler Throws(Exception exception) =>
        new(_ => Task.FromException<HttpResponseMessage>(exception));

    /// <summary>Waits until the request is cancelled, so the resilience pipeline's timeouts fire.</summary>
    public static StubOpenMeteoHandler NeverResponds() =>
        new(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("Unreachable.");
        });

    private static string ReadFixture() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "open-meteo-archive-2026-09-27_28.json"));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requestCount);
        return respond(cancellationToken);
    }
}
