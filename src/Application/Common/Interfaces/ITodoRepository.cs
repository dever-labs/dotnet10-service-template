using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Common.Interfaces;

public interface ITodoRepository
{
    Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Todo> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, TodoStatus? status = null, CancellationToken cancellationToken = default);
    Task AddAsync(Todo todo, CancellationToken cancellationToken = default);
    Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default);
    Task DeleteAsync(Todo todo, CancellationToken cancellationToken = default);
}
