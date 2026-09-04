using Lecture01.Sync.GraphQL.CatsService.DataAccess;
using Lecture01.Sync.GraphQL.CatsService.DataAccess.Entities;

namespace Lecture01.Sync.GraphQL.CatsService.Api.Cats.Services;

public class CatsService : ICatsService
{
    private readonly CatsDbContext _db;

    public CatsService(CatsDbContext db)
    {
        _db = db;
    }

    public IQueryable<Cat> GetCatsQueryable()
    {
        return _db.Cats;
    }

    public async Task<Cat> CreateAsync(Cat cat)
    {
        cat.Id = Guid.NewGuid();
        _db.Cats.Add(cat);
        await _db.SaveChangesAsync();
        return cat;
    }
}
