using Lecture09.Background.Airflow.UploadService.Api.Videos.Services;
using Lecture09.Background.Airflow.UploadService.External.HttpClients;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddControllers();
builder.Services.AddScoped<IVideosService, VideosService>();
builder.Services.AddHttpClient<IAirflowClient, AirflowClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Airflow:Url"]!);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
