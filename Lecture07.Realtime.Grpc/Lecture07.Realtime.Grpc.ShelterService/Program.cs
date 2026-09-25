using Lecture07.Realtime.Grpc.ShelterService.Api.Feeders.Grpc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<FeedersGrpcServiceHandler>();

app.Run();
