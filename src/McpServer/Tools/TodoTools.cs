using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ServiceTemplate.McpServer.Tools;

[McpServerToolType]
#pragma warning disable CA1812 // Instantiated by the MCP framework via dependency injection
internal sealed class TodoTools(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "get_todos"), Description("List all todos. Optionally filter by status: Pending, InProgress, or Done.")]
    public async Task<string> GetTodosAsync(
        [Description("Optional status filter (Pending, InProgress, Done)")] string? status = null,
        CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(status)
            ? "/api/todos"
            : $"/api/todos?status={Uri.EscapeDataString(status)}";

        var response = await http.GetAsync(new Uri(url, UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return response.IsSuccessStatusCode ? body : $"Error {(int)response.StatusCode}: {body}";
    }

    [McpServerTool(Name = "get_todo"), Description("Get a single todo by its ID.")]
    public async Task<string> GetTodoAsync(
        [Description("The todo GUID")] string id,
        CancellationToken ct = default)
    {
        var response = await http.GetAsync(new Uri($"/api/todos/{id}", UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return response.IsSuccessStatusCode ? body : $"Error {(int)response.StatusCode}: {body}";
    }

    [McpServerTool(Name = "create_todo"), Description("Create a new todo item.")]
    public async Task<string> CreateTodoAsync(
        [Description("Short title for the todo")] string title,
        [Description("Optional longer description")] string? description = null,
        [Description("Optional due date as ISO-8601 string (e.g. 2025-12-31T00:00:00Z)")] string? dueDate = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            title,
            description,
            dueDate = dueDate is null ? (DateTimeOffset?)null : DateTimeOffset.Parse(dueDate, CultureInfo.InvariantCulture)
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await http.PostAsync(new Uri("/api/todos", UriKind.Relative), content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return response.StatusCode == HttpStatusCode.Created ? body : $"Error {(int)response.StatusCode}: {body}";
    }

    [McpServerTool(Name = "complete_todo"), Description("Mark a todo as completed.")]
    public async Task<string> CompleteTodoAsync(
        [Description("The todo GUID to mark complete")] string id,
        CancellationToken ct = default)
    {
        var response = await http.PutAsync(new Uri($"/api/todos/{id}/complete", UriKind.Relative), null, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return response.IsSuccessStatusCode ? $"Todo {id} marked as complete." : $"Error {(int)response.StatusCode}: {body}";
    }

    [McpServerTool(Name = "delete_todo"), Description("Permanently delete a todo.")]
    public async Task<string> DeleteTodoAsync(
        [Description("The todo GUID to delete")] string id,
        CancellationToken ct = default)
    {
        var response = await http.DeleteAsync(new Uri($"/api/todos/{id}", UriKind.Relative), ct);
        return response.IsSuccessStatusCode
            ? $"Todo {id} deleted."
            : $"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}";
    }

    [McpServerTool(Name = "get_health"), Description("Check the health of the running Service Template API.")]
    public async Task<string> GetHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(new Uri("/health", UriKind.Relative), ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            return response.IsSuccessStatusCode ? body : $"Unhealthy ({(int)response.StatusCode}): {body}";
        }
        catch (HttpRequestException ex)
        {
            return $"API unreachable: {ex.Message}. Ensure `make infra-up && make run` is running.";
        }
    }
}
