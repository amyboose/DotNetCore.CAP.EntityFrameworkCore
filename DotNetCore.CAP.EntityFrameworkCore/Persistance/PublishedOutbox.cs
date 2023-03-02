namespace DotNetCore.CAP.EntityFrameworkCore.Persistance;

public class PublishedOutbox
{
    public const int MaxVersionPropertyLegth = 20;
    public const int MaxNamePropertyLegth = 200;
    public const int MaxStatusNamePropertyLength = 50;

#pragma warning disable CS8618
    private PublishedOutbox() { }
#pragma warning restore CS8618

    public PublishedOutbox(Initializer initializer)
    {
        Id = initializer.Id;
        Version = initializer.Version;
        Name = initializer.Name;
        Content = initializer.Content;
        Retries = initializer.Retries;
        Added = initializer.Added;
        ExpiresAt = initializer.ExpiresAt;
        StatusName = initializer.StatusName;
    }

    public long Id { get; private set; }
    public string Version { get; private set; }
    public string Name { get; private set; }
    public string? Content { get; private set; }
    public int Retries { get; private set; }
    public DateTime Added { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string StatusName { get; private set; }

    public class Initializer
    {
        public required long Id { get; set; }
        public required string Version { get; set; }
        public required string Name { get; set; }
        public required string? Content { get; set; }
        public required int Retries { get; set; }
        public required DateTime Added { get; set; }
        public required DateTime? ExpiresAt { get; set; }
        public required string StatusName { get; set; }
    }
}
