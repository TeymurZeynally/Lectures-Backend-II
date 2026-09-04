namespace Lecture01.Sync.GraphQL.CatsService.Api.Cats.GraphQL;

public record CatInput(string Name, string Breed, int AgeMonths, double WeightKg, bool IsVaccinated);
