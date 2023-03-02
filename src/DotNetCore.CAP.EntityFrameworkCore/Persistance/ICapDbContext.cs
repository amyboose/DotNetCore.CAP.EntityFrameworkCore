using DotNetCore.CAP.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.EntityFrameworkCore.Persistance;

public interface ICapDbContext
{
    public DbSet<PublishedOutbox> PublishedOutboxes { get; }
    IReadOnlyCollection<MediumMessage> StoredMessages { get; }
    bool AddStoredMessage(MediumMessage mediumMessage);
}
