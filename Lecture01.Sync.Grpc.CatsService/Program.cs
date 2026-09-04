using Lecture01.Sync.Grpc.CatsService.Api.Cats.Grpc;
using Lecture01.Sync.Grpc.CatsService.Api.Cats.Services;
using Lecture01.Sync.Grpc.CatsService.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddDbContext<CatsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<ICatsService, CatsService>();
builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CatsDbContext>().Database.Migrate();
}

app.MapGrpcService<CatsGrpcServiceHandler>();

app.Run();
