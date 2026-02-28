using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ServiceTemplate.Infrastructure.AI;

/// <summary>
/// Thin client for GitHub Models (OpenAI-compatible inference endpoint).
/// Requires a GitHub token with "models" permission and a model name.
/// Configure via appsettings "GitHubModels" section or environment variables.
/// </summary>
public sealed class GitHubModelsClient(
    HttpClient http,
    IOptions<GitHubModelsOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Send a simple chat completion request and return the assistant message text.
    /// Returns null if the service is not configured (empty token).
    /// </summary>
    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.Token))
        {
            return null;
        }

        ConfigureClient(opts);

        var payload = new
        {
            model = opts.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            max_tokens = opts.MaxTokens,
            temperature = 0.7
        };

        using var response = await http.PostAsJsonAsync(
            "/chat/completions", payload, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }

    private void ConfigureClient(GitHubModelsOptions opts)
    {
        http.BaseAddress ??= new Uri(opts.Endpoint);

        if (!http.DefaultRequestHeaders.Contains("Authorization"))
        {
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", opts.Token);
        }
    }
}
