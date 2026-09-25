namespace Lecture08.ServiceDiscovery.Consul.CatsService.State;

public class InstanceState
{
    private int _requestCount;

    public bool IsHealthy { get; set; } = true;

    public void CountRequest()
    {
        Interlocked.Increment(ref _requestCount);
    }

    public int ResetRequestCount()
    {
        return Interlocked.Exchange(ref _requestCount, 0);
    }
}
