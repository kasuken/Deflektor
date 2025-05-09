using Deflektor.Func;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

var client = new HttpClient(new MyHttpMessageHandler());

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("fake-model-name", "fake-api-key", httpClient: client)
    .Build();

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddLogging();
builder.Services.AddSingleton<GraphService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var clientID = config["ClientID"];
    var clientSecret = config["ClientSecret"];
    var tenantID = config["TenantID"];
    return new GraphService(clientID, clientSecret, tenantID);
});

builder.Services.AddSingleton<DeflektorEngineService>();

builder.Build().Run();
