namespace Lecture08.ServiceDiscovery.Consul.CatsService.Api.Cats.Contract;

public class CatResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
}
