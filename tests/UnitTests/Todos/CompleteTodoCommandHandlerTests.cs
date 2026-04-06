using NSubstitute;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Telemetry;
using ServiceTemplate.Application.Todos.Commands.CompleteTodo;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.UnitTests.Todos;

public sealed class CompleteTodoCommandHandlerTests
{
    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ITodoMetrics _metrics = Substitute.For<ITodoMetrics>();
    private readonly CompleteTodoCommandHandler _sut;

    public CompleteTodoCommandHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _sut = new CompleteTodoCommandHandler(_repository, _unitOfWork, _timeProvider, _metrics);
    }

    [Fact]
    public async Task Handle_OpenTodo_ReturnsCompletedResponseAsync()
    {
        var todo = Todo.Create("Finish task", null, null, _timeProvider).Value;
        _repository.GetByIdAsync(todo.Id, Arg.Any<CancellationToken>()).Returns(todo);

        var result = await _sut.HandleAsync(new CompleteTodoCommand(todo.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TodoStatus.Done.ToString());
    }

    [Fact]
    public async Task Handle_NonExistentTodo_ReturnsNotFoundErrorAsync()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Todo?)null);

        var result = await _sut.HandleAsync(new CompleteTodoCommand(Guid.CreateVersion7()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_AlreadyCompletedTodo_ReturnsConflictErrorAsync()
    {
        var todo = Todo.Create("Already done", null, null, _timeProvider).Value;
        todo.Complete(_timeProvider);
        _repository.GetByIdAsync(todo.Id, Arg.Any<CancellationToken>()).Returns(todo);

        var result = await _sut.HandleAsync(new CompleteTodoCommand(todo.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_OpenTodo_SavesAndRecordsMetricAsync()
    {
        var todo = Todo.Create("Task to complete", null, null, _timeProvider).Value;
        _repository.GetByIdAsync(todo.Id, Arg.Any<CancellationToken>()).Returns(todo);

        await _sut.HandleAsync(new CompleteTodoCommand(todo.Id), CancellationToken.None);

        await _repository.Received(1).UpdateAsync(todo, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _metrics.Received(1).RecordCompleted();
    }
}
