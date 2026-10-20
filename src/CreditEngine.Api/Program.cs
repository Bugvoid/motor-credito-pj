using System.Text.Json.Serialization;
using CreditEngine.Api;
using CreditEngine.Api.Endpoints;
using CreditEngine.Infrastructure.Enrichment;
using CreditEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddJsonConsole();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<CreditOptions>(builder.Configuration.GetSection("Credit"));
builder.Services.AddDbContext<CreditDbContext>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddHttpClient<ICompanyRegistry, BrasilApiCompanyRegistry>(c =>
{
    c.BaseAddress = new Uri("https://brasilapi.com.br/");
    c.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddSingleton<ICreditBureau, SimulatedCreditBureau>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<CreditDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
        await scope.ServiceProvider.GetRequiredService<CreditDbContext>().Database.MigrateAsync();
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Motor de Crédito PJ"));
}

app.UseExceptionHandler();
app.MapCreditRequestEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
