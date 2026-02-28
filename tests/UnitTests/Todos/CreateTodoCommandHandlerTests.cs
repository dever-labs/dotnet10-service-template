using Bogus;
using NSubstitute;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Telemetry;
using ServiceTemplate.Application.Todos;
using ServiceTemplate.Application.Todos.Commands.CreateTodo;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.UnitTests.Todos;

public sealed class CreateTodoCommandHandlerTests
{
    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ITodoMetrics _metrics = Substitute.For<ITodoMetrics>();
    private readonly CreateTodoCommandHandler _sut;

    private static readonly Faker Faker = new();

    public CreateTodoCommandHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _sut = new CreateTodoCommandHandler(_repository, _unitOfWork, _timeProvider, _metrics);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTodoResponse()
    {
        // Arrange
        var command = new CreateTodoCommand(
            Title: Faker.Lorem.Sentence(3),
            Description: Faker.Lorem.Paragraph(),
            DueDate: DateTimeOffset.UtcNow.AddDays(7));

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be(command.Title.Trim());
        result.Value.Status.Should().Be(TodoStatus.Open.ToString());
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsTodo()
    {
        // Arrange
        var command = new CreateTodoCommand(Faker.Lorem.Sentence(3), null, null);

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(Arg.Any<Todo>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyTitle_ReturnsFailure(string title)
    {
        // Arrange
        var command = new CreateTodoCommand(title, null, null);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_TitleExceeds200Chars_ReturnsFailure()
    {
        // Arrange
        var command = new CreateTodoCommand(new string('x', 201), null, null);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}
