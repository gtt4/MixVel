using Microsoft.Extensions.Configuration;
using MixVel.Service;
using MixVel.Settings;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ProviderSettings>(configuration.GetSection("Providers"));
builder.Services.AddSingleton<IProviderUriResolver, ProviderUriResolver>();


builder.Services.AddCustomServices(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.logg
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
