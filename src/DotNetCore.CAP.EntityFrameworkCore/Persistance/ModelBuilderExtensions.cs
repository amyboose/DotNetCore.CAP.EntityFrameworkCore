using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.EntityFrameworkCore.Persistance;
public static class ModelBuilderExtensions
{
    public static ModelBuilder AddCap(this ModelBuilder modelBuilder, Action<CapModelBuilder>? configure)
    {
        CapModel capModel = new();
        configure?.Invoke(new DefaultModelBuilder(capModel));
        if (capModel.EntityTypeConfigurations == null)
        {
            throw new InvalidOperationException("No database provider");
        }

        capModel.EntityTypeConfigurations!(new EntityTypeConfigurationContext(modelBuilder));
        return modelBuilder;
    }
}
