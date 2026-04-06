using ServiceTemplate.Fake.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FakeStore>();

var app = builder.Build();

// Real contract — behaves like a working in-memory implementation
app.MapTodoEndpoints();

// Control API — lets consuming tests seed data, inspect calls, and reset state
app.MapFakeControlEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/ready",  () => Results.Ok(new { status = "ok" }));

await app.RunAsync();
