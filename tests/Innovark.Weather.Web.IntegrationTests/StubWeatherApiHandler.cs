using System.Net;
using System.Text;

namespace Innovark.Weather.Web.IntegrationTests;

/// <summary>Stands in for Innovark.Weather.Api: records the request and returns a canned response.</summary>
internal sealed class StubWeatherApiHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public static StubWeatherApiHandler Json(HttpStatusCode status, string json, string mediaType = "application/json") =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, mediaType) });

    public static StubWeatherApiHandler Throws(Exception exception) => new(_ => throw exception);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(respond(request));
    }
}
