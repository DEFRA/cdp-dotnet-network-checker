using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace CdpDotnetNetworkChecker.Services;

public sealed class ProxyResult
{
    public string Uri  { get; init; } = default!;
    public int    Code { get; init; }  = default!;
    public string? Error { get; init; } = default;
    public long BodyLength { get; init; } = default!;
    public string? ProxyUrl{ get; init; } = default!;
    public bool ViaProxy { get; init; } = default!;
    
    
    
}

public interface IProxyService
{
    Task<ProxyResult> CallProxy(string uri, string? proxyUri);
    Task<ProxyResult> CallDirect(string uri);
}

public class ProxyService : IProxyService
{
    private readonly HttpClient _directClient;
    private readonly ILogger _logger;

    public ProxyService(ILogger<ProxyService> logger)
    {
        _logger = logger;
        var proxyString = Environment.GetEnvironmentVariable("HTTP_PROXY");
        _logger.LogInformation("Setting up the proxy using {proxyString}", proxyString);
        _directClient = new HttpClient(new HttpClientHandler { UseProxy = false });
    }

    public async Task<ProxyResult> CallProxy(string uri, string? proxyUri)
    {
        var proxyString = Environment.GetEnvironmentVariable("HTTP_PROXY");
        if (proxyUri != null)
        {
            proxyString = proxyUri;
        }
        _logger.LogInformation("Setting up the proxy using {proxyString}", proxyString);
        try
        {
            var proxy = new WebProxy(proxyString);
            var proxyClient = new HttpClient(new HttpClientHandler { Proxy = proxy, UseProxy = true});
            _logger.LogInformation("Calling {uri} via the proxy", uri);

            var response = await proxyClient.GetAsync(uri);
            var content = "";
            if (!response.IsSuccessStatusCode)
            {
                content = await response.Content.ReadAsStringAsync();
            }
            var proxyResult = new ProxyResult
            {
                Uri = uri, 
                Code = (int)response.StatusCode, 
                Error = content, 
                BodyLength = content.Length,
                ViaProxy = true,
                ProxyUrl = proxy.Address?.ToString()
            };

            return proxyResult;
        }
        catch (Exception ex)
        {
            var proxyResult = new ProxyResult
            {
                Uri = uri, 
                Code = 500, 
                Error = ex.Message, 
                BodyLength = 0,
                ViaProxy = true,
                ProxyUrl = proxyString
            };

            return proxyResult;
        }
        
    }

    public async Task<ProxyResult> CallDirect(string uri)
    {
        _logger.LogInformation("Calling {uri} via the proxy", uri);
        
        var response = await _directClient.GetAsync(uri);
        var content = "";
        if (!response.IsSuccessStatusCode)
        {
            content = await response.Content.ReadAsStringAsync();
        }
        
        
        var proxyResult = new ProxyResult
        {
            Uri = uri, 
            Code = (int)response.StatusCode, 
            Error = content, 
            BodyLength = content.Length,
            ViaProxy = false
        };

        return proxyResult;
    }
}