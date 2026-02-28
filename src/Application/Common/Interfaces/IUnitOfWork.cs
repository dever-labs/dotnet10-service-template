namespace ServiceTemplate.Application.Common.Interfaces;

/// <summary>Abstraction over the Unit of Work pattern (wraps DbContext.SaveChangesAsync).</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
