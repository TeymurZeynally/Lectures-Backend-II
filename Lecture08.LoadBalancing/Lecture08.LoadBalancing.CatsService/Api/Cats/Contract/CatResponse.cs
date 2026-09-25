namespace Lecture08.LoadBalancing.CatsService.Api.Cats.Contract;

public class CatResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
}
