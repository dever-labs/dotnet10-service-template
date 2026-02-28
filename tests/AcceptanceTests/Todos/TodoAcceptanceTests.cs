using System.Net;
using System.Net.Http.Json;
using ServiceTemplate.AcceptanceTests.Fixtures;
using ServiceTemplate.Application.Todos;

namespace ServiceTemplate.AcceptanceTests.Todos;

/// <summary>
/// Acceptance tests for the Todos API.
/// These tests describe business behaviour from a user perspective.
/// </summary>
[Collection(nameof(AcceptanceTestCollection))]
public sealed class TodoAcceptanceTests(AcceptanceTestFixture fixture)
{
    // ── Feature: Manage Todos ─────────────────────────────────────────────────

    [Fact(DisplayName = "Given a valid todo, when it is created, then it should be retrievable")]
    public async Task CreateAndRetrieveTodo()
    {
        // Given
        var title = "Buy groceries";
        var description = "Milk, eggs, bread";

        // When
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/todos", new { Title = title, Description = description });
        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // Then
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Should().NotBeNull();

        var getResponse = await fixture.Client.GetAsync($"/api/todos/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrieved = await getResponse.Content.ReadFromJsonAsync<TodoResponse>();
        retrieved!.Title.Should().Be(title);
        retrieved.Description.Should().Be(description);
        retrieved.Status.Should().Be("Open");
    }

    [Fact(DisplayName = "Given an existing todo, when it is updated, then the changes should be persisted")]
    public async Task UpdateTodo_PersistsChanges()
    {
        // Given
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/todos", new { Title = "Original title" });
        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // When
        var updateResponse = await fixture.Client.PutAsJsonAsync($"/api/todos/{created!.Id}", new
        {
            Title = "Updated title",
            Description = "Now with description",
            DueDate = (DateTimeOffset?)null,
        });

        // Then
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TodoResponse>();
        updated!.Title.Should().Be("Updated title");
    }

    [Fact(DisplayName = "Given an existing todo, when it is deleted, then it should no longer be findable")]
    public async Task DeleteTodo_RemovesItFromTheSystem()
    {
        // Given
        var createResponse = await fixture.Client.PostAsJsonAsync("/api/todos", new { Title = "To be deleted" });
        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // When
        var deleteResponse = await fixture.Client.DeleteAsync($"/api/todos/{created!.Id}");

        // Then
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await fixture.Client.GetAsync($"/api/todos/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Given an empty title, when creating a todo, then validation should fail")]
    public async Task CreateTodo_EmptyTitle_ReturnsValidationError()
    {
        // When
        var response = await fixture.Client.PostAsJsonAsync("/api/todos", new { Title = "" });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact(DisplayName = "Given health check endpoint, when called, then it should return healthy")]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await fixture.Client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
