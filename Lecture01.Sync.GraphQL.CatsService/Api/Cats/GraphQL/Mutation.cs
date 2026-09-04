using HotChocolate;
using Lecture01.Sync.GraphQL.CatsService.DataAccess;
using Lecture01.Sync.GraphQL.CatsService.DataAccess.Entities;

namespace Lecture01.Sync.GraphQL.CatsService.Api.Cats.GraphQL;

public class Mutation
{
    public async Task<Cat> AddCat(CatInput input, [Service] CatsDbContext db)
    {
        var cat = new Cat
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Breed = input.Breed,
            AgeMonths = input.AgeMonths,
            WeightKg = input.WeightKg,
            IsVaccinated = input.IsVaccinated
        };

        db.Cats.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }
}
