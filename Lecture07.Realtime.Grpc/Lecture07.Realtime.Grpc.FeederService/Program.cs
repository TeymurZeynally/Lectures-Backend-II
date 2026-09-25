using Lecture07.Realtime.Grpc.FeederService.BackgroundServices;
using Lecture07.Realtime.Grpc.FeederService.Options;
using Lecture07.Realtime.Grpc.ShelterService.Grpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<FeederOptions>(builder.Configuration.GetSection("Feeder"));

builder.Services.AddGrpcClient<FeedersGrpcService.FeedersGrpcServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["ShelterService:BaseUrl"]!);
});

builder.Services.AddHostedService<FeederSimulator>();

builder.Build().Run();
