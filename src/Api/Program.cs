using Api.Modules;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.SetupServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapControllers();

await app.InitialiseDatabaseAsync();

app.Run();

public partial class Program { }
