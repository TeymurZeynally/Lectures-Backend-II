namespace Lecture03.Replication.App.BackgroundServices;

public sealed class LoadGeneratorOptions
{
    public int ParallelWorkers { get; init; } = 8;
    public int UpdateDelayMs { get; init; } = 200;
}
