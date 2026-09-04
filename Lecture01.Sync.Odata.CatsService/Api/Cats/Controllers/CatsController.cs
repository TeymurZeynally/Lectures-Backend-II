using Lecture01.Sync.Odata.CatsService.DataAccess;
using Lecture01.Sync.Odata.CatsService.DataAccess.Entities;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Lecture01.Sync.Odata.CatsService.Api.Cats.Controllers;

public class CatsController : ODataController
{
    private readonly CatsDbContext _db;

    public CatsController(CatsDbContext db)
    {
        _db = db;
    }

    [EnableQuery]
    public IQueryable<Cat> Get()
    {
        return _db.Cats;
    }
}
