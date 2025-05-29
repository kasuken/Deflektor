using Deflektor.Func.Interfaces;
using Deflektor.Func.Models;
using Deflektor.Func.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Extensions.Http;
using Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService();

// Configure strongly-typed settings
builder.Services.Configure<AppSettings>(builder.Configuration);

// Add HTTP client factory with retry policies
builder.Services.AddHttpClient("SemanticKernel", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<Deflektor.Func.MyHttpMessageHandler>()
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient("GraphApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy());

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddLogging();
builder.Services.AddSingleton<IGraphService, Deflektor.Func.Services.GraphService>();
builder.Services.AddSingleton<IDeflektorEngineService, Deflektor.Func.Services.DeflektorEngineService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<Deflektor.Func.MyHttpMessageHandler>();

builder.Build().Run();

// HTTP retry policy
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
