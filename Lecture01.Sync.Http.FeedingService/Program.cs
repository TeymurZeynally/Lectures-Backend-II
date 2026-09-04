using Lecture01.Sync.Http.FeedingService.Api.Feeding.Services;
using Lecture01.Sync.Http.FeedingService.External.HttpClients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IFeedingService, FeedingService>();
builder.Services.AddHttpClient<ICatsClient, CatsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CatsService:BaseUrl"]!);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
