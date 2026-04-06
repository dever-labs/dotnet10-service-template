using NSubstitute;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Telemetry;
using ServiceTemplate.Application.Todos.Commands.DeleteTodo;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.UnitTests.Todos;

public sealed class DeleteTodoCommandHandlerTests
{
    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITodoMetrics _metrics = Substitute.For<ITodoMetrics>();
    private readonly DeleteTodoCommandHandler _sut;

    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    public DeleteTodoCommandHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _sut = new DeleteTodoCommandHandler(_repository, _unitOfWork, _metrics);
    }

    [Fact]
    public async Task Handle_ExistingTodo_ReturnsSuccessAsync()
    {
        var todo = Todo.Create("To delete", null, null, _timeProvider).Value;
        _repository.GetByIdAsync(todo.Id, Arg.Any<CancellationToken>()).Returns(todo);

        var result = await _sut.HandleAsync(new DeleteTodoCommand(todo.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentTodo_ReturnsNotFoundErrorAsync()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Todo?)null);

        var result = await _sut.HandleAsync(new DeleteTodoCommand(Guid.CreateVersion7()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ExistingTodo_DeletesAndRecordsMetricAsync()
    {
        var todo = Todo.Create("To delete", null, null, _timeProvider).Value;
        _repository.GetByIdAsync(todo.Id, Arg.Any<CancellationToken>()).Returns(todo);

        await _sut.HandleAsync(new DeleteTodoCommand(todo.Id), CancellationToken.None);

        await _repository.Received(1).DeleteAsync(todo, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _metrics.Received(1).RecordDeleted();
    }
}
