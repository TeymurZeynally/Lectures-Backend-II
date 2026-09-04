using Lecture01.Sync.Grpc.CatsService.DataAccess;
using Lecture01.Sync.Grpc.CatsService.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lecture01.Sync.Grpc.CatsService.Api.Cats.Services;

public class CatsService : ICatsService
{
    private readonly CatsDbContext _db;

    public CatsService(CatsDbContext db)
    {
        _db = db;
    }

    public async Task<Cat> CreateAsync(Cat cat)
    {
        cat.Id = Guid.NewGuid();
        _db.Cats.Add(cat);
        await _db.SaveChangesAsync();
        return cat;
    }

    public Task<List<Cat>> GetAllAsync()
    {
        return _db.Cats.ToListAsync();
    }

    public Task<Cat?> GetByIdAsync(Guid id)
    {
        return _db.Cats.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cat?> UpdateAsync(Guid id, Cat cat)
    {
        var existing = await _db.Cats.FirstOrDefaultAsync(c => c.Id == id);
        if (existing is null)
        {
            return null;
        }

        existing.Name = cat.Name;
        existing.Breed = cat.Breed;
        existing.AgeMonths = cat.AgeMonths;
        existing.WeightKg = cat.WeightKg;
        existing.IsVaccinated = cat.IsVaccinated;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _db.Cats.FirstOrDefaultAsync(c => c.Id == id);
        if (existing is null)
        {
            return false;
        }

        _db.Cats.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
