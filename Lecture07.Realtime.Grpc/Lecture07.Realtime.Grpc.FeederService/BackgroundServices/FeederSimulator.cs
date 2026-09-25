using Grpc.Core;
using Lecture07.Realtime.Grpc.FeederService.Options;
using Lecture07.Realtime.Grpc.ShelterService.Grpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lecture07.Realtime.Grpc.FeederService.BackgroundServices;

public sealed class FeederSimulator(
    FeedersGrpcService.FeedersGrpcServiceClient client,
    IOptions<FeederOptions> options,
    ILogger<FeederSimulator> logger) : BackgroundService
{
    private readonly Lock _lock = new();
    private int _foodGrams;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        _foodGrams = settings.CapacityGrams;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(settings, stoppingToken);
            }
            catch (RpcException ex)
            {
                logger.LogWarning("[{FeederId}] Stream closed ({Status}), reconnecting in 2 s", settings.FeederId, ex.StatusCode);
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task RunAsync(FeederOptions settings, CancellationToken stoppingToken)
    {
        using var call = client.Connect();

        logger.LogInformation("[{FeederId}] Opening a stream to the shelter", settings.FeederId);

        var receiving = ReceiveCommandsAsync(call.ResponseStream, settings);

        var random = new Random();
        long sequenceNumber = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            int eatenGrams;
            int foodGrams;
            lock (_lock)
            {
                eatenGrams = Math.Min(_foodGrams, random.Next(5, 25));
                _foodGrams -= eatenGrams;
                foodGrams = _foodGrams;
            }

            var telemetry = new FeederTelemetry
            {
                FeederId = settings.FeederId,
                CatName = settings.CatName,
                SequenceNumber = ++sequenceNumber,
                EatenGrams = eatenGrams,
                FoodGrams = foodGrams,
                CapacityGrams = settings.CapacityGrams
            };

            await call.RequestStream.WriteAsync(telemetry);

            logger.LogInformation(
                "[{FeederId}] Sent #{Sequence}: {CatName} ate {EatenGrams} g, {FoodGrams} g left",
                settings.FeederId, telemetry.SequenceNumber, settings.CatName, eatenGrams, foodGrams);

            await Task.Delay(1000);
        }

        await call.RequestStream.CompleteAsync();
        await receiving;
    }

    private async Task ReceiveCommandsAsync(IAsyncStreamReader<FeederCommand> responseStream, FeederOptions settings)
    {
        await foreach (var command in responseStream.ReadAllAsync())
        {
            int foodGrams;
            lock (_lock)
            {
                _foodGrams = Math.Min(settings.CapacityGrams, _foodGrams + command.Grams);
                foodGrams = _foodGrams;
            }

            logger.LogInformation(
                "[{FeederId}] Received #{Sequence}: dispense {Grams} g ({Reason}), {FoodGrams} g now",
                settings.FeederId, command.SequenceNumber, command.Grams, command.Reason, foodGrams);
        }
    }
}
