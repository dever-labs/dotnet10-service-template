using Bogus;
using NSubstitute;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Telemetry;
using ServiceTemplate.Application.Todos;
using ServiceTemplate.Application.Todos.Commands.UpdateTodo;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.UnitTests.Todos;

public sealed class UpdateTodoCommandHandlerTests
{
    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ITodoMetrics _metrics = Substitute.For<ITodoMetrics>();
    private readonly UpdateTodoCommandHandler _sut;

    private static readonly Faker Faker = new();

    public UpdateTodoCommandHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _sut = new UpdateTodoCommandHandler(_repository, _unitOfWork, _timeProvider, _metrics);
    }

    [Fact]
    public async Task Handle_ExistingTodo_ReturnsUpdatedResponseAsync()
    {
        var todo = Todo.Create("Original", null, null, _timeProvider).Value;
        _repository.GetByIdAsync(todo.Id, Arg.Any<CancellationToken>()).Returns(todo);

        var command = new UpdateTodoCommand(todo.Id, "Updated title", "New desc", null);
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Updated title");
    }

    [Fact]
    public async Task Handle_NonExistentTodo_ReturnsNotFoundErrorAsync()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Todo?)null);

        var result = await _sut.HandleAsync(
            new UpdateTodoCommand(Guid.CreateVersion7(), Faker.Lorem.Sentence(3), null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ValidUpdate_SavesAndRecordsMetricAsync()
    {
        var todo = Todo.Create("Original", null, null, _timeProvider).Value;
        _repository.GetByIdAsync(todo.Id, Arg.Any<CancellationToken>()).Returns(todo);

        await _sut.HandleAsync(new UpdateTodoCommand(todo.Id, "New title", null, null), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _metrics.Received(1).RecordUpdated();
    }
}
