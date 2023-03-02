using DotNetCore.CAP.EntityFrameworkCore.Persistance.PostgreSQL.Configurations;

namespace DotNetCore.CAP.EntityFrameworkCore.Persistance.PostgreSQL;
public static class CapModelBuilderPostgreSqlExtensions
{
    public static CapModelBuilder UsePostgreSql(this CapModelBuilder builder, string? schema = "cap")
    {
        builder.UseEntityTypeConfigurations(builder =>
        {
            builder.ModelBuilder.ApplyConfiguration(new PublishedEventConfiguration(schema));
        });

        return builder;
    }
}
