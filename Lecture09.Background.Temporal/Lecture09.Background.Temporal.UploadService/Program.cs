using Lecture09.Background.Temporal.UploadService.Api.Videos.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddControllers();
builder.Services.AddScoped<IVideosService, VideosService>();
builder.Services.AddTemporalClient(builder.Configuration["Temporal:Address"]!, "default");

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
