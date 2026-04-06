using NSubstitute;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Models;
using ServiceTemplate.Application.Todos;
using ServiceTemplate.Application.Todos.Queries.GetTodos;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.UnitTests.Todos;

public sealed class GetTodosQueryHandlerTests
{
    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly GetTodosQueryHandler _sut;

    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    public GetTodosQueryHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _sut = new GetTodosQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_DefaultQuery_ReturnsPagedResultAsync()
    {
        var todos = new List<Todo>
        {
            Todo.Create("First", null, null, _timeProvider).Value,
            Todo.Create("Second", null, null, _timeProvider).Value,
        };
        _repository.GetPagedAsync(1, 20, null, Arg.Any<CancellationToken>())
            .Returns((todos.AsReadOnly() as IReadOnlyList<Todo>, 2));

        var result = await _sut.HandleAsync(new GetTodosQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_StatusFilter_PassesStatusToRepositoryAsync()
    {
        _repository.GetPagedAsync(1, 20, TodoStatus.Done, Arg.Any<CancellationToken>())
            .Returns((new List<Todo>() as IReadOnlyList<Todo>, 0));

        await _sut.HandleAsync(new GetTodosQuery(Status: TodoStatus.Done), CancellationToken.None);

        await _repository.Received(1).GetPagedAsync(1, 20, TodoStatus.Done, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyPageAsync()
    {
        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<TodoStatus?>(), Arg.Any<CancellationToken>())
            .Returns((new List<Todo>() as IReadOnlyList<Todo>, 0));

        var result = await _sut.HandleAsync(new GetTodosQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}
