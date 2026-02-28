using System.Net;
using System.Net.Http.Json;
using Bogus;
using ServiceTemplate.Application.Todos;
using ServiceTemplate.IntegrationTests.Fixtures;

namespace ServiceTemplate.IntegrationTests.Todos;

[Collection(nameof(IntegrationTestCollection))]
public sealed class TodoIntegrationTests(IntegrationTestFixture fixture) : IAsyncLifetime
{
    private static readonly Faker Faker = new();

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateTodo_ValidRequest_Returns201WithTodo()
    {
        // Arrange
        var request = new { Title = Faker.Lorem.Sentence(3), Description = Faker.Lorem.Paragraph() };

        // Act
        var response = await fixture.Client.PostAsJsonAsync("/api/todos", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>();
        todo.Should().NotBeNull();
        todo!.Title.Should().Be(request.Title);
    }

    [Fact]
    public async Task GetTodo_ExistingTodo_Returns200()
    {
        // Arrange
        var created = await CreateTodoAsync();

        // Act
        var response = await fixture.Client.GetAsync($"/api/todos/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>();
        todo!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetTodo_NonExistentId_Returns404()
    {
        var response = await fixture.Client.GetAsync($"/api/todos/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTodo_ExistingTodo_Returns204()
    {
        // Arrange
        var created = await CreateTodoAsync();

        // Act
        var response = await fixture.Client.DeleteAsync($"/api/todos/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateTodo_EmptyTitle_Returns422()
    {
        var request = new { Title = "" };
        var response = await fixture.Client.PostAsJsonAsync("/api/todos", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<TodoResponse?> CreateTodoAsync()
    {
        var request = new { Title = Faker.Lorem.Sentence(3) };
        var response = await fixture.Client.PostAsJsonAsync("/api/todos", request);
        return await response.Content.ReadFromJsonAsync<TodoResponse>();
    }
}
