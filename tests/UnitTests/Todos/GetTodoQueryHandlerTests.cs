using NSubstitute;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Todos.Queries.GetTodo;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.UnitTests.Todos;

public sealed class GetTodoQueryHandlerTests
{
    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly GetTodoQueryHandler _sut;

    public GetTodoQueryHandlerTests() => _sut = new GetTodoQueryHandler(_repository);

    [Fact]
    public async Task Handle_ExistingTodo_ReturnsTodo()
    {
        // Arrange
        var todo = Todo.Create("Test todo", null, null, TimeProvider.System).Value;
        _repository.GetByIdAsync(todo.Id, Arg.Any<CancellationToken>()).Returns(todo);

        // Act
        var result = await _sut.HandleAsync(new GetTodoQuery(todo.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(todo.Id);
    }

    [Fact]
    public async Task Handle_NonExistingTodo_ReturnsNotFoundError()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Todo?)null);

        // Act
        var result = await _sut.HandleAsync(new GetTodoQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Todo.NotFound");
    }
}
