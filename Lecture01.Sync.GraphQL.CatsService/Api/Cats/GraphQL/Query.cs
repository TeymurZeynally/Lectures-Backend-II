using HotChocolate;
using HotChocolate.Data;
using Lecture01.Sync.GraphQL.CatsService.DataAccess;
using Lecture01.Sync.GraphQL.CatsService.DataAccess.Entities;

namespace Lecture01.Sync.GraphQL.CatsService.Api.Cats.GraphQL;

public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Cat> GetCats([Service] CatsDbContext db)
    {
        return db.Cats;
    }
}
