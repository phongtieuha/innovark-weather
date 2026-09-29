using System.Net;
using System.Text;

namespace Innovark.Weather.UnitTests.Infrastructure.OpenMeteo;

/// <summary>Returns a canned response and records every request, so tests never touch the network.</summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string body, string mediaType = "application/json")
    : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType),
        });
    }
}
