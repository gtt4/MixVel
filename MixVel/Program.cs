using MixVel.Service;
using MixVel.Settings;
using Prometheus;
using MixVel.Cache;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// Add services to the container.
var services = builder.Services;
builder.WebHost.UseUrls("https://localhost:7241"); // 
services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.Configure<ProviderSettings>(configuration.GetSection("Providers"));
services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));

services.AddSingleton<IProviderUriResolver, ProviderUriResolver>();
services.AddSingleton<IMetricsService, PrometheusMetricsService>();


builder.Services.AddCustomServices(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.logg
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapMetrics();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
