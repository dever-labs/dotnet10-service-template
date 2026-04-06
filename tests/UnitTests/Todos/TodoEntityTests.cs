using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.UnitTests.Todos;

/// <summary>Unit tests for the Todo domain entity — validate business rules without the application layer.</summary>
public sealed class TodoEntityTests
{
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    public TodoEntityTests() => _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

    [Fact]
    public void Create_ValidTitle_ReturnsSuccess()
    {
        var result = Todo.Create("Buy milk", null, null, _timeProvider);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Buy milk");
        result.Value.Status.Should().Be(TodoStatus.Open);
    }

    [Fact]
    public void Create_TrimsTitle()
    {
        var result = Todo.Create("  Trimmed  ", null, null, _timeProvider);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Trimmed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTitle_ReturnsValidationError(string title)
    {
        var result = Todo.Create(title, null, null, _timeProvider);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Todo.TitleRequired");
        result.Error!.Value.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_TitleExceeds200Chars_ReturnsValidationError()
    {
        var result = Todo.Create(new string('x', 201), null, null, _timeProvider);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Todo.TitleTooLong");
    }

    [Fact]
    public void Create_RaisesTodoCreatedEvent()
    {
        var result = Todo.Create("Walk dog", null, null, _timeProvider);

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TodoCreatedEvent>();
    }

    [Fact]
    public void Update_ValidTitle_UpdatesFields()
    {
        var todo = Todo.Create("Original", null, null, _timeProvider).Value;
        var newDue = DateTimeOffset.UtcNow.AddDays(3);

        var result = todo.Update("Updated", "desc", newDue, _timeProvider);

        result.IsSuccess.Should().BeTrue();
        todo.Title.Should().Be("Updated");
        todo.Description.Should().Be("desc");
        todo.DueDate.Should().Be(newDue);
    }

    [Fact]
    public void Update_EmptyTitle_ReturnsValidationError()
    {
        var todo = Todo.Create("Original", null, null, _timeProvider).Value;

        var result = todo.Update("", null, null, _timeProvider);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Todo.TitleRequired");
    }

    [Fact]
    public void Complete_OpenTodo_SetsStatusDone()
    {
        var todo = Todo.Create("Finish report", null, null, _timeProvider).Value;

        var result = todo.Complete(_timeProvider);

        result.IsSuccess.Should().BeTrue();
        todo.Status.Should().Be(TodoStatus.Done);
    }

    [Fact]
    public void Complete_AlreadyCompleted_ReturnsConflictError()
    {
        var todo = Todo.Create("Already done", null, null, _timeProvider).Value;
        todo.Complete(_timeProvider);

        var result = todo.Complete(_timeProvider);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Todo.AlreadyCompleted");
        result.Error!.Value.Type.Should().Be(ErrorType.Conflict);
    }
}
