using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using ServiceTemplate.Api.Endpoints;
using ServiceTemplate.Api.Middleware;
using ServiceTemplate.Application;
using ServiceTemplate.Application.Common.Telemetry;
using ServiceTemplate.Infrastructure;
using ServiceTemplate.Infrastructure.Persistence;

try
{
    var builder = WebApplication.CreateBuilder(args);

    var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "service-template";
    var otlpEndpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");

    // ── OpenTelemetry — traces, metrics, AND logs all in one pipeline ─────────
    // Logs export via OTLP alongside traces/metrics for full correlation.
    // Console output format is controlled by appsettings Logging:Console:FormatterName.
    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(serviceName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = otlpEndpoint))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()           // GC, thread pool, allocations, JIT
            .AddMeter(TodoMetrics.MeterName)       // custom domain metrics
            .AddOtlpExporter(o => o.Endpoint = otlpEndpoint))
        .WithLogging(logging => logging
            .AddOtlpExporter(o => o.Endpoint = otlpEndpoint));

    // Include scopes and formatted messages in OTel log records
    builder.Logging.AddOpenTelemetry(o =>
    {
        o.IncludeScopes = true;
        o.IncludeFormattedMessage = true;
    });

    // ── Application layers ────────────────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── API ───────────────────────────────────────────────────────────────────
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddHealthChecks()
        .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!);

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    if (!app.Environment.IsEnvironment("Test"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/scalar");
    }

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/ready");
    app.MapTodoEndpoints();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    await Console.Error.WriteLineAsync($"Application startup failed: {ex}");
    throw;
}

// Required for WebApplicationFactory in integration/acceptance tests
#pragma warning disable CA1515 // Must be public — WebApplicationFactory<Program> in test assemblies requires it
public partial class Program;
#pragma warning restore CA1515
