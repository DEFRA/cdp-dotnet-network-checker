using CdpDotnetNetworkChecker.Config;
using CdpDotnetNetworkChecker.Data;
using CdpDotnetNetworkChecker.Endpoints;
using CdpDotnetNetworkChecker.Services;
using CdpDotnetNetworkChecker.Utils;
using FluentValidation;
using Serilog;

//-------- Configure the WebApplication builder------------------//

var builder = WebApplication.CreateBuilder(args);

// Grab environment variables
builder.Configuration.AddEnvironmentVariables("CDP");
builder.Configuration.AddEnvironmentVariables();

// Serilog
builder.Services.AddHttpContextAccessor();
builder.Logging.ClearProviders();
var tracingHeader = builder.Configuration.GetValue<string>("Tracing:Header");
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With<LogLevelMapper>()
    .Enrich.WithRequestHeader(tracingHeader, tracingHeader)
    .CreateLogger();
builder.Logging.AddSerilog(logger);

//tracing
builder.Services.AddHttpClient("DefaultClient")
    .AddHeaderPropagation();

builder.Services.AddHeaderPropagation(options =>
{
    var tracingEnabled = builder.Configuration.GetValue<bool>("Tracing:Enabled");
    if (tracingEnabled && string.IsNullOrEmpty(tracingHeader) == false)
    {
        options.Headers.Add(tracingHeader);    
    }
});

logger.Information("Starting application");

// Load certificates into Trust Store - Note must happen before Mongo and Http client connections 
TrustStore.SetupTrustStore(logger);

// Mongo
builder.Services.AddSingleton<IMongoDbClientFactory>(_ =>
    new MongoDbClientFactory(builder.Configuration.GetValue<string>("Mongo:DatabaseUri")!,
        builder.Configuration.GetValue<string>("Mongo:DatabaseName")!));

// our service
builder.Services.AddSingleton<IProxyService, ProxyService>();

// health checks
builder.Services.AddHealthChecks();

// swagger endpoints
if (builder.IsSwaggerEnabled())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.UseHeaderPropagation();
app.UseRouting();
app.UseProxyEndpoints();
app.MapHealthChecks("/health");

app.Run();