using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using ServiceTemplate.McpServer.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddHttpClient<TodoTools>(client =>
    {
        var baseUrl = builder.Configuration["SERVICE_TEMPLATE_BASE_URL"]
            ?? "http://localhost:5000";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<TodoTools>();

await builder.Build().RunAsync();
