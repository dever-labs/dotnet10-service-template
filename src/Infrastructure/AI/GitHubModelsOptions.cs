namespace ServiceTemplate.Infrastructure.AI;

/// <summary>
/// Configuration for the GitHub Models AI inference endpoint.
/// Bind from appsettings: "GitHubModels" section.
/// </summary>
public sealed class GitHubModelsOptions
{
    public const string SectionName = "GitHubModels";

    /// <summary>GitHub personal access token with "models" permission.</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>Model name. Defaults to gpt-4o.</summary>
    public string Model { get; init; } = "gpt-4o";

    /// <summary>GitHub Models inference endpoint.</summary>
    public string Endpoint { get; init; } = "https://models.inference.ai.azure.com";

    /// <summary>Max tokens to generate per request.</summary>
    public int MaxTokens { get; init; } = 500;
}
