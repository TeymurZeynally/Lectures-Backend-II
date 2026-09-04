using Lecture01.Sync.GraphQL.CatsService.DataAccess.Entities;

namespace Lecture01.Sync.GraphQL.CatsService.Api.Cats.Services;

public interface ICatsService
{
    IQueryable<Cat> GetCatsQueryable();
    Task<Cat> CreateAsync(Cat cat);
}
