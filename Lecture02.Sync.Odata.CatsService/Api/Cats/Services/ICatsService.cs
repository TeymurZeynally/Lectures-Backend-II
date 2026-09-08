using Lecture02.Sync.Odata.CatsService.DataAccess.Entities;

namespace Lecture02.Sync.Odata.CatsService.Api.Cats.Services;

public interface ICatsService
{
    IQueryable<Cat> GetCatsQueryable();
}
