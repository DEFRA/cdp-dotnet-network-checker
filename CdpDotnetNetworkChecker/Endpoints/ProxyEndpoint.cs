using CdpDotnetNetworkChecker.Services;

namespace CdpDotnetNetworkChecker.Endpoints;

public static class ProxyEndpoint
{

    public static void UseProxyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/proxy", CallViaProxy);

        app.MapGet("/direct", CallDirect);
    }

    private static async Task<IResult> CallViaProxy(IProxyService proxyService, string uri)
    {
        Console.WriteLine(uri);
        var res = await proxyService.CallProxy(uri);
        return Results.Ok(res);    }

    private static async Task<IResult> CallDirect(IProxyService proxyService, string uri)
    {
        var res = await proxyService.CallDirect(uri);
        return Results.Ok(res);
    }
} 