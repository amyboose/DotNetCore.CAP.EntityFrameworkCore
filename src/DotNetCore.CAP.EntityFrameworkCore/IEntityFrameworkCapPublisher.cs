using DotNetCore.CAP.EntityFrameworkCore.Persistance;

namespace DotNetCore.CAP.EntityFrameworkCore;

public interface IEntityFrameworkCapPublisher : ICapPublisher
{
    Task PublishDomainEvents<T>(ICapDbContext dbContext, string name, T? value, IDictionary<string, string?> headers,
        CancellationToken cancellationToken = default);
}
