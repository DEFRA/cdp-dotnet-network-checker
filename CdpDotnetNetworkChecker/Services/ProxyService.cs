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
    Task<ProxyResult> CallProxy(string uri);
    Task<ProxyResult> CallDirect(string uri);
}

public class ProxyService : IProxyService
{
    private readonly WebProxy _proxy;
    private readonly HttpClient _proxyClient;
    private readonly HttpClient _directClient;
    private readonly ILogger _logger;

    public ProxyService(ILogger<ProxyService> logger)
    {
        _logger = logger;
        var proxyString = Environment.GetEnvironmentVariable("HTTPS_PROXY");
        _logger.LogInformation("Setting up the proxy using {}", proxyString);
        _proxy = new WebProxy(proxyString);
        _proxyClient = new HttpClient(new HttpClientHandler { Proxy = _proxy, UseProxy = true});
        _directClient = new HttpClient(new HttpClientHandler { UseProxy = false });
    }

    public async Task<ProxyResult> CallProxy(string uri)
    {
  
        _logger.LogInformation("Calling {uri} via the proxy", uri);
        
        var response = await _proxyClient.GetAsync(uri);
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
            ProxyUrl = _proxy.Address?.ToString()
        };

        return proxyResult;
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