using Lecture01.Sync.Odata.CatsService.DataAccess;
using Lecture01.Sync.Odata.CatsService.DataAccess.Entities;

namespace Lecture01.Sync.Odata.CatsService.Api.Cats.Services;

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
}
