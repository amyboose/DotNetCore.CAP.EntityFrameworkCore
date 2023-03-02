namespace DotNetCore.CAP.EntityFrameworkCore.Persistance;
public class CapModel
{
    public string? Schema { get; set; }
    public Action<EntityTypeConfigurationContext>? EntityTypeConfigurations { get; set; }
}
