using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.EntityFrameworkCore.Persistance;
public readonly struct EntityTypeConfigurationContext
{
    public EntityTypeConfigurationContext(ModelBuilder modelBuilder)
    {
        ModelBuilder = modelBuilder;
    }

    public ModelBuilder ModelBuilder { get; }
}
