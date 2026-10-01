using Innovark.Weather.Web.WeatherApi;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Innovark.Weather.Web.ApiClients;

/// <summary>
/// Creates the Kiota-generated <see cref="WeatherApiClient"/> on a typed HttpClient from
/// IHttpClientFactory, so handlers and connection pooling are managed by the factory.
/// </summary>
public sealed class WeatherApiClientFactory(HttpClient httpClient)
{
    public WeatherApiClient Create() =>
        new(new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient));
}
