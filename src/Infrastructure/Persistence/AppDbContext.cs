using Microsoft.EntityFrameworkCore;
using ServiceTemplate.Application.Common.Events;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IDomainEventDispatcher domainEventDispatcher)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            entry.Entity.ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }
}
