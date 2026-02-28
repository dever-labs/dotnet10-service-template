using MediatR;
using Microsoft.AspNetCore.Mvc;
using ServiceTemplate.Application.Todos;
using ServiceTemplate.Application.Todos.Commands.CreateTodo;
using ServiceTemplate.Application.Todos.Commands.DeleteTodo;
using ServiceTemplate.Application.Todos.Commands.UpdateTodo;
using ServiceTemplate.Application.Todos.Queries.GetTodo;
using ServiceTemplate.Application.Todos.Queries.GetTodos;

namespace ServiceTemplate.Api.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos")
            .WithTags("Todos");

        group.MapGet("/", GetTodosAsync)
            .WithName("GetTodos")
            .WithSummary("Get a paged list of todos");

        group.MapGet("/{id:guid}", GetTodoAsync)
            .WithName("GetTodo")
            .WithSummary("Get a todo by ID")
            .Produces<TodoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateTodoAsync)
            .WithName("CreateTodo")
            .WithSummary("Create a new todo")
            .Produces<TodoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", UpdateTodoAsync)
            .WithName("UpdateTodo")
            .WithSummary("Update an existing todo")
            .Produces<TodoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", DeleteTodoAsync)
            .WithName("DeleteTodo")
            .WithSummary("Delete a todo")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetTodosAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTodosQuery(page, pageSize), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTodoAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTodoQuery(id), cancellationToken);

        return result.Match(
            todo => Results.Ok(todo),
            error => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status404NotFound));
    }

    private static async Task<IResult> CreateTodoAsync(
        CreateTodoCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Match(
            todo => Results.CreatedAtRoute("GetTodo", new { id = todo.Id }, todo),
            error => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<IResult> UpdateTodoAsync(
        Guid id,
        [FromBody] UpdateTodoRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateTodoCommand(id, request.Title, request.Description, request.DueDate),
            cancellationToken);

        return result.Match(
            todo => Results.Ok(todo),
            error => error.Code.Contains("NotFound")
                ? Results.Problem(detail: error.Description, statusCode: StatusCodes.Status404NotFound)
                : Results.Problem(detail: error.Description, statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<IResult> DeleteTodoAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteTodoCommand(id), cancellationToken);

        return result.Match(
            _ => Results.NoContent(),
            error => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status404NotFound));
    }
}

public sealed record UpdateTodoRequest(string Title, string? Description, DateTimeOffset? DueDate);
