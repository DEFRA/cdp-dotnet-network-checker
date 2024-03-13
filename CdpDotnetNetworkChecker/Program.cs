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
builder.Logging.ClearProviders();
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Logging.AddSerilog(logger);

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

app.UseRouting();
app.UseProxyEndpoints();
app.MapHealthChecks("/health");

app.Run();