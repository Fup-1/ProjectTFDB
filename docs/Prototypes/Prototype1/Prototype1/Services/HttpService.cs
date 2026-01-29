using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectTFDB.Services;

public sealed class HttpService
{
    private readonly HttpClient _httpClient;

    public HttpService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ProjectTFDB", "0.1"));
    }

    public async Task<(int StatusCode, string Body)> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var body = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ((int)res.StatusCode, body);
    }
}

