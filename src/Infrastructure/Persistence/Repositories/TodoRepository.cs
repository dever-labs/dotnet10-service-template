using Microsoft.EntityFrameworkCore;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Infrastructure.Persistence.Repositories;

public sealed class TodoRepository(AppDbContext dbContext) : ITodoRepository
{
    public Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Todo> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Todos.AsNoTracking().OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Todo todo, CancellationToken cancellationToken = default) =>
        await dbContext.Todos.AddAsync(todo, cancellationToken);

    public Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        dbContext.Todos.Update(todo);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        dbContext.Todos.Remove(todo);
        return Task.CompletedTask;
    }
}
