using HotChocolate;
using HotChocolate.Data;
using Lecture01.Sync.GraphQL.CatsService.Api.Cats.Contract;
using Lecture01.Sync.GraphQL.CatsService.Api.Cats.Services;

namespace Lecture01.Sync.GraphQL.CatsService.Api.Cats.GraphQL;

public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<CatResponse> GetCats([Service] ICatsService catsService)
    {
        return catsService.GetCatsQueryable().Select(cat => new CatResponse
        {
            Id = cat.Id,
            Name = cat.Name,
            Breed = cat.Breed,
            AgeMonths = cat.AgeMonths,
            WeightKg = cat.WeightKg,
            IsVaccinated = cat.IsVaccinated
        });
    }
}
