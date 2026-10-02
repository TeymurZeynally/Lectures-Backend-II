using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Lecture09.Background.Rws.VideoService.BackgroundServices.VideoProcessing.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Distributed.EtcdPersistence.Registration;
using RecurrentWorkerService.Distributed.Registration;
using RecurrentWorkerService.Workers.Models;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

var etcdAddresses = builder.Configuration.GetSection("Etcd:Endpoints").Get<string[]>()!
    .Select(endpoint => endpoint.Split(':'))
    .Select(parts => new BalancerAddress(parts[0], int.Parse(parts[1])))
    .ToArray();

var etcdChannel = GrpcChannel.ForAddress("static://", new GrpcChannelOptions
{
    Credentials = ChannelCredentials.Insecure,
    ServiceProvider = new ServiceCollection()
        .AddSingleton<ResolverFactory>(new StaticResolverFactory(_ => etcdAddresses))
        .BuildServiceProvider(),
    ServiceConfig = new ServiceConfig
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 5,
                    InitialBackoff = TimeSpan.FromSeconds(1),
                    MaxBackoff = TimeSpan.FromSeconds(5),
                    BackoffMultiplier = 1.5,
                    RetryableStatusCodes = { StatusCode.Unavailable }
                }
            }
        }
    }
});

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

var workers = builder.Services
    .AddDistributedWorkers("video-service", w => w

        .AddDistributedRecurrentWorker<UploadsPollingWorker>("uploads-polling", s => s
            .SetPeriod(TimeSpan.FromSeconds(3))
            .SetRetryOnFailDelay(TimeSpan.FromSeconds(1)),
            o => o.SetMultiIterationOnNodeMaxDuration(TimeSpan.FromSeconds(1)))

        .AddDistributedCronWorker<TrendingWorker>("trending", s => s
            .SetCronExpression("* * * * *")
            .SetRetryOnFailDelay(TimeSpan.FromSeconds(5)))
            
        .AddDistributedWorkloadWorker<TranscodingQueueWorker>("transcoding-queue", s => s
            .SetRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(16))
            .SetStrategies(c => c
                .Multiply(Workload.Zero, 2)
                .Subtract(Workload.FromPercent(25), TimeSpan.FromSeconds(4))
                .Divide(Workload.FromPercent(75), 2)
                .Set(Workload.Full, TimeSpan.FromSeconds(1)))
            .SetRetryOnFailDelay(TimeSpan.FromSeconds(1))))
    .AddEtcdPersistence(etcdChannel);

var host = builder.Build();

host.Services.GetRequiredService<ILogger<Program>>().LogInformation(
    "Node {NodeId} started, pid {Pid}", workers.NodeId, Environment.ProcessId);

host.Run();
