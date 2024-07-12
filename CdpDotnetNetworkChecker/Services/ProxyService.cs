using System.Net;

namespace CdpDotnetNetworkChecker.Services;

public sealed class ProxyResult
{
    public string Uri       { get; init; } = default!;
    public int    Code      { get; init; }  = default!;
    public string? Error    { get; init; } = default;
    public long BodyLength  { get; init; } = default!;
    public string? ProxyUrl { get; init; } = default!;
    public bool ViaProxy    { get; init; } = default!;
    
    
    
}

public interface IProxyService
{
    Task<ProxyResult> CallProxy(string uri, string? proxyFromParam);
    Task<ProxyResult> CallDirect(string uri);
}

public class ProxyService : IProxyService
{
    private readonly HttpClient _directClient;
    private readonly ILogger _logger;

    public ProxyService(ILogger<ProxyService> logger)
    {
        _logger = logger;
        _directClient = new HttpClient(new HttpClientHandler { UseProxy = false });
    }

    public async Task<ProxyResult> CallProxy(string uri, string? proxyFromParam)
    {
        var proxyString = proxyFromParam ?? Environment.GetEnvironmentVariable("CDP_HTTP_PROXY");

        if (proxyString == null)
        {
            throw new Exception("Not proxy settings were found. Check CDP_HTTP_PROXY is set");
        }

        var proxyUri = new Uri(proxyString);
        _logger.LogInformation("Setting up the proxy using {proxyUri}", RedactUriCredentials(proxyUri));
        try
        {
            var proxy = new WebProxy(proxyUri);

            GetCredentialsFromUriOrEnv(proxyUri, out var proxyUsername, out var proxyPassword);
            
            if (proxyUsername != null && proxyPassword != null)
            {
                _logger.LogInformation("Connecting to proxy using squid credentials");
                proxy.Credentials = new NetworkCredential(proxyUsername, proxyPassword);
            }
            
            var proxyClient = new HttpClient(new HttpClientHandler { Proxy = proxy, UseProxy = true });
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
                ProxyUrl = RedactUriCredentials(proxyUri)
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
                ProxyUrl = RedactUriCredentials(proxyUri)
            };

            return proxyResult;
        }

    }

    public async Task<ProxyResult> CallDirect(string uri)
    {
        _logger.LogInformation("Calling {uri} direct", uri);

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

    private static string RedactUriCredentials(Uri uri)
    {
        var uriBuilder = new UriBuilder(uri);
        if (!string.IsNullOrEmpty(uriBuilder.Password))
        {
            uriBuilder.Password = "*****";
        }
        return uriBuilder.Uri.ToString();
    }
    
    private void GetCredentialsFromUriOrEnv(Uri uri, out string? username, out string? password)
    {
        
        var split = uri.UserInfo.Split(':');
        if (split.Length == 2)
        {
            _logger.LogInformation("Getting credentials from URI");
            username = uri.UserInfo.Split(':')[0];
            password = uri.UserInfo.Split(':')[1];
        }
        else
        {
            _logger.LogInformation("Getting credentials from SQUID ENVs");
            username = Environment.GetEnvironmentVariable("SQUID_USERNAME");
            password = Environment.GetEnvironmentVariable("SQUID_PASSWORD");
        }
    }
}