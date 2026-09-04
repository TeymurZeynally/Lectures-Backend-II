using Lecture01.Sync.Grpc.CatsService.Grpc;
using Lecture01.Sync.Grpc.FeedingService.Api.Feeding.Services;
using Lecture01.Sync.Grpc.FeedingService.External.GrpcClients;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IFeedingService, FeedingService>();
builder.Services.AddScoped<ICatsClient, CatsClient>();
builder.Services.AddGrpcClient<CatsGrpcService.CatsGrpcServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["CatsService:BaseUrl"]!);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
