using Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Services;
using Lecture07.Realtime.Sse.FeedingService.BackgroundServices;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IFeedersService, FeedersService>();
builder.Services.AddHostedService<FeedingSimulator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
