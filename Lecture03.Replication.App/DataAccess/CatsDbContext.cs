using Lecture03.Replication.App.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lecture03.Replication.App.DataAccess;

public sealed class CatsDbContext(DbContextOptions<CatsDbContext> options) : DbContext(options)
{
    public DbSet<Cat> Cats => Set<Cat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cat>().HasData(
            new Cat { Id = 1, Name = "Whiskers", Breed = "Maine Coon" },
            new Cat { Id = 2, Name = "Mittens", Breed = "British Shorthair" },
            new Cat { Id = 3, Name = "Shadow", Breed = "Bombay" },
            new Cat { Id = 4, Name = "Luna", Breed = "Siamese" },
            new Cat { Id = 5, Name = "Oliver", Breed = "Scottish Fold" },
            new Cat { Id = 6, Name = "Simba", Breed = "Abyssinian" },
            new Cat { Id = 7, Name = "Nala", Breed = "Ragdoll" },
            new Cat { Id = 8, Name = "Milo", Breed = "Sphynx" },
            new Cat { Id = 9, Name = "Leo", Breed = "Bengal" },
            new Cat { Id = 10, Name = "Bella", Breed = "Persian" },
            new Cat { Id = 11, Name = "Tom", Breed = "Norwegian Forest" },
            new Cat { Id = 12, Name = "Cleo", Breed = "Devon Rex" },
            new Cat { Id = 13, Name = "Jasper", Breed = "Russian Blue" },
            new Cat { Id = 14, Name = "Chloe", Breed = "Turkish Angora" },
            new Cat { Id = 15, Name = "Felix", Breed = "American Shorthair" },
            new Cat { Id = 16, Name = "Sasha", Breed = "Birman" },
            new Cat { Id = 17, Name = "Oscar", Breed = "Manx" },
            new Cat { Id = 18, Name = "Ginger", Breed = "Somali" },
            new Cat { Id = 19, Name = "Smokey", Breed = "Chartreux" },
            new Cat { Id = 20, Name = "Pepper", Breed = "Egyptian Mau" });
    }
}
