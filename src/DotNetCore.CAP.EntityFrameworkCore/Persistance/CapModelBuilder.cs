namespace DotNetCore.CAP.EntityFrameworkCore.Persistance;
public abstract class CapModelBuilder
{
    private readonly CapModel _model;

    public CapModelBuilder(CapModel model)
    {
        _model = model;
    }

    public CapModelBuilder UseEntityTypeConfigurations(
      Action<EntityTypeConfigurationContext> entityTypeConfigurations)
    {
        _model.EntityTypeConfigurations = entityTypeConfigurations;

        return this;
    }
}
