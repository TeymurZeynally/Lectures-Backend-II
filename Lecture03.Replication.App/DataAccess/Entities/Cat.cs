namespace Lecture03.Replication.App.DataAccess.Entities;

public sealed class Cat
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Breed { get; set; }
    public int PetsCount { get; set; }
    public DateTimeOffset? LastPetAt { get; set; }
}
