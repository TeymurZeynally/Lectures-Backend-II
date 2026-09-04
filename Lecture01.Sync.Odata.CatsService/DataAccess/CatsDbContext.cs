using Lecture01.Sync.Odata.CatsService.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lecture01.Sync.Odata.CatsService.DataAccess;

public class CatsDbContext : DbContext
{
    public CatsDbContext(DbContextOptions<CatsDbContext> options) : base(options)
    {
    }

    public DbSet<Cat> Cats => Set<Cat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cat>().HasData(
            new Cat
            {
                Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                Name = "Барсик",
                Breed = "Британская короткошёрстная",
                AgeMonths = 24,
                WeightKg = 5.2,
                IsVaccinated = true
            },
            new Cat
            {
                Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
                Name = "Мурка",
                Breed = "Сибирская",
                AgeMonths = 36,
                WeightKg = 4.8,
                IsVaccinated = true
            },
            new Cat
            {
                Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"),
                Name = "Васька",
                Breed = "Дворовая",
                AgeMonths = 12,
                WeightKg = 3.9,
                IsVaccinated = false
            },
            new Cat
            {
                Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"),
                Name = "Рыжик",
                Breed = "Мейн-кун",
                AgeMonths = 8,
                WeightKg = 3.1,
                IsVaccinated = true
            },
            new Cat
            {
                Id = Guid.Parse("a5555555-5555-5555-5555-555555555555"),
                Name = "Кузя",
                Breed = "Шотландская вислоухая",
                AgeMonths = 18,
                WeightKg = 4.0,
                IsVaccinated = false
            }
        );
    }
}
