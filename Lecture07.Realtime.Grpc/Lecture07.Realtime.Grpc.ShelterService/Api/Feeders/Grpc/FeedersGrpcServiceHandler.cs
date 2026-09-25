using global::Grpc.Core;
using Lecture07.Realtime.Grpc.ShelterService.Grpc;

namespace Lecture07.Realtime.Grpc.ShelterService.Api.Feeders.Grpc;

public class FeedersGrpcServiceHandler : FeedersGrpcService.FeedersGrpcServiceBase
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<FeedersGrpcServiceHandler> _logger;

    public FeedersGrpcServiceHandler(IHostApplicationLifetime lifetime, ILogger<FeedersGrpcServiceHandler> logger)
    {
        _lifetime = lifetime;
        _logger = logger;
    }

    public override async Task Connect(
        IAsyncStreamReader<FeederTelemetry> requestStream,
        IServerStreamWriter<FeederCommand> responseStream,
        ServerCallContext context)
    {
        FeederTelemetry? lastTelemetry = null;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, _lifetime.ApplicationStopping);

        async Task SendCommandsAsync()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            long sequenceNumber = 0;

            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    if (lastTelemetry is null)
                    {
                        continue;
                    }

                    var command = lastTelemetry.FoodGrams < 60
                        ? new FeederCommand { Grams = lastTelemetry.CapacityGrams - lastTelemetry.FoodGrams, Reason = "refill" }
                        : new FeederCommand { Grams = 10, Reason = "treat" };
                    command.SequenceNumber = ++sequenceNumber;

                    await responseStream.WriteAsync(command, cts.Token);

                    _logger.LogInformation(
                        "Sent #{Sequence} to {FeederId}: dispense {Grams} g ({Reason})",
                        command.SequenceNumber, lastTelemetry.FeederId, command.Grams, command.Reason);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        var sending = SendCommandsAsync();

        try
        {
            await foreach (var telemetry in requestStream.ReadAllAsync(cts.Token))
            {
                lastTelemetry = telemetry;

                _logger.LogInformation(
                    "Received #{Sequence} from {FeederId}: {CatName} ate {EatenGrams} g, {FoodGrams} g left",
                    telemetry.SequenceNumber, telemetry.FeederId, telemetry.CatName, telemetry.EatenGrams, telemetry.FoodGrams);
            }

            _logger.LogInformation("{FeederId} closed its stream", lastTelemetry?.FeederId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{FeederId} disconnected", lastTelemetry?.FeederId);
        }

        await cts.CancelAsync();
        await sending;
    }
}
