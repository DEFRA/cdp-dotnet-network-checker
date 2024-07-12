using CdpDotnetNetworkChecker.Services;

namespace CdpDotnetNetworkChecker.Endpoints;

public static class ProxyEndpoint
{

    public static void UseProxyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/proxy", CallViaProxy);

        app.MapGet("/direct", CallDirect);
    }

    private static async Task<IResult> CallViaProxy(IProxyService proxyService, string uri, string? proxy)
    {
        try
        {
            var res = await proxyService.CallProxy(uri, proxy);
            return Results.Ok(res);
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message);
        }
    }

    private static async Task<IResult> CallDirect(IProxyService proxyService, string uri)
    {
        var res = await proxyService.CallDirect(uri);
        return Results.Ok(res);
    }
}