using Lecture01.Sync.Http.CatsService.DataAccess.Entities;

namespace Lecture01.Sync.Http.CatsService.Api.Cats.Services;

public interface ICatsService
{
    Task<Cat> CreateAsync(Cat cat);
    Task<List<Cat>> GetAllAsync();
    Task<Cat?> GetByIdAsync(Guid id);
    Task<Cat?> UpdateAsync(Guid id, Cat cat);
    Task<bool> DeleteAsync(Guid id);
}
