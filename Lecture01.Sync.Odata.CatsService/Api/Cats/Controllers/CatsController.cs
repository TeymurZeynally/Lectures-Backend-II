using Lecture01.Sync.Odata.CatsService.Api.Cats.Contract;
using Lecture01.Sync.Odata.CatsService.Api.Cats.Services;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Lecture01.Sync.Odata.CatsService.Api.Cats.Controllers;

public class CatsController : ODataController
{
    private readonly ICatsService _catsService;

    public CatsController(ICatsService catsService)
    {
        _catsService = catsService;
    }

    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.Supported | AllowedQueryOptions.Apply)]
    public IQueryable<CatResponse> Get()
    {
        return _catsService.GetCatsQueryable().Select(cat => new CatResponse
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
