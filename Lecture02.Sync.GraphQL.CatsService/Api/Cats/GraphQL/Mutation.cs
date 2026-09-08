using HotChocolate;
using Lecture02.Sync.GraphQL.CatsService.Api.Cats.Contract;
using Lecture02.Sync.GraphQL.CatsService.Api.Cats.Services;
using Lecture02.Sync.GraphQL.CatsService.DataAccess.Entities;

namespace Lecture02.Sync.GraphQL.CatsService.Api.Cats.GraphQL;

public class Mutation
{
    public async Task<CatResponse> AddCat(CatRequest input, [Service] ICatsService catsService)
    {
        var cat = new Cat
        {
            Name = input.Name,
            Breed = input.Breed,
            AgeMonths = input.AgeMonths,
            WeightKg = input.WeightKg,
            IsVaccinated = input.IsVaccinated
        };

        var created = await catsService.CreateAsync(cat);

        return new CatResponse
        {
            Id = created.Id,
            Name = created.Name,
            Breed = created.Breed,
            AgeMonths = created.AgeMonths,
            WeightKg = created.WeightKg,
            IsVaccinated = created.IsVaccinated
        };
    }
}
