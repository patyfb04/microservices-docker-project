using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Catalog.Service;
using Play.Catalog.Service.Entities;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.Repositories;
using Play.Common.Settings;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
const string AllowedOriginSetting = "AllowedOrigin";

BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));

Log.Logger = new LoggerConfiguration()
             .WriteTo.Console()
             .WriteTo.File("logs/log.txt")
             .MinimumLevel.Information()
             .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(nameof(MongoDbSettings)));

builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection(nameof(ServiceSettings)));

builder.Services.Configure<MassTransitSettings>(
    builder.Configuration.GetSection(nameof(MassTransitSettings)));

builder.Services.AddMongoDb()
    .AddMongoRepository<Item>("items")
    .AddMassTransitWithRabbitMq()
    .AddJwtBearerAuthentication();

var serviceSettings = builder.Configuration
    .GetSection(nameof(ServiceSettings))
    .Get<ServiceSettings>();

var clientServicesSettings = builder.Configuration
    .GetSection(nameof(ClientServicesSettings))
    .Get<ClientServicesSettings>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Read, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope","catalog.fullaccess", "catalog.readaccess");
    });

    options.AddPolicy(Policies.Write, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope", "catalog.fullaccess", "catalog.writeaccess");
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Catalog.Service", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Catalog.Service v1"));

    app.UseCors(config => {
        config.WithOrigins(builder.Configuration[AllowedOriginSetting])
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();