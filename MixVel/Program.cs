using Prometheus.HttpClientMetrics;
using MixVel.Service;
using MixVel.Settings;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
var services = builder.Services;
services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.Configure<ProviderSettings>(configuration.GetSection("Providers"));
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
