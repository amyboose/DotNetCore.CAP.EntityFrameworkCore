using DotNetCore.CAP.EntityFrameworkCore.Persistance;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace DotNetCore.CAP.EntityFrameworkCore.Interceptors;

public class CapTransactionIntercepter : DbTransactionInterceptor
{
    private readonly IDispatcher _dispatcher;

    public CapTransactionIntercepter(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
    {
        return base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
    }

    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        if (eventData.Context is ICapDbContext capDbContext)
        {
            AddDomainEvents(capDbContext);
        }

        base.TransactionCommitted(transaction, eventData);
    }

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ICapDbContext capDbContext)
        {
            AddDomainEvents(capDbContext);
        }

        return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }

    private void AddDomainEvents(ICapDbContext capDbContext)
    {
        var storedMessages = capDbContext.StoredMessages;

        foreach (MediumMessage message in storedMessages)
        {
            _dispatcher.EnqueueToPublish(message);
        }
    }
}
