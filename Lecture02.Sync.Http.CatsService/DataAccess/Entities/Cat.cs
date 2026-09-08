namespace Lecture02.Sync.Http.CatsService.DataAccess.Entities;

public class Cat
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public int AgeMonths { get; set; }
    public double WeightKg { get; set; }
    public bool IsVaccinated { get; set; }
}
