using Lecture01.Sync.GraphQL.CatsService.Api.Cats.GraphQL;
using Lecture01.Sync.GraphQL.CatsService.Api.Cats.Services;
using Lecture01.Sync.GraphQL.CatsService.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ICatsService, CatsService>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CatsDbContext>().Database.Migrate();
}

app.MapGraphQL();

app.Run();
