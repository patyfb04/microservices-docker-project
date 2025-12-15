using MassTransit;
using Microsoft.OpenApi;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.Repositories;
using Play.Common.Settings;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Polly;
using Polly.Timeout;
using Serilog;
using System;


var builder = WebApplication.CreateBuilder(args);
const string AllowedOriginSetting = "AllowedOrigin";

//BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.CSharpLegacy));
//BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.CSharpLegacy)));
BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(BsonType.String));
BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(BsonType.String)));

Log.Logger = new LoggerConfiguration()
             .WriteTo.Console()
             .WriteTo.File("logs/invent_log.txt")
             .MinimumLevel.Information()
             .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(nameof(MongoDbSettings)));

builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection(nameof(ServiceSettings)));

builder.Services.Configure<MassTransitSettings>(
    builder.Configuration.GetSection(nameof(MassTransitSettings)));

builder.Services.Configure<ClientServicesSettings>(
    builder.Configuration.GetSection("ClientServicesSettings"));

builder.Services.AddMongoDb()
    .AddMongoRepository<InventoryItem>("inventoryitems")
    .AddMongoRepository<CatalogItem>("catalogitems")
    .AddMassTransitWithRabbitMq()
    .AddJwtBearerAuthentication();

var clientServicesSettings = builder.Configuration
    .GetSection(nameof(ClientServicesSettings))
    .Get<ClientServicesSettings>();

var catalogService = clientServicesSettings.ClientServices
    .FirstOrDefault(s => s.ServiceName.Equals("CatalogService", StringComparison.OrdinalIgnoreCase));

AddCatalogClient(builder.Services, catalogService?.ServiceUrl);

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Inventory.Service", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Inventory.Service v1"));

    app.UseCors(config => {
        config.WithOrigins(builder.Configuration[AllowedOriginSetting])
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


 static void AddCatalogClient(IServiceCollection serviceCollection, string catalogUrl)
{
    Random jitterer = new Random();

    serviceCollection.AddHttpClient<CatalogClient>(client =>
    {
        client.BaseAddress = new Uri(catalogUrl);
    }).AddTransientHttpErrorPolicy(policy => policy.Or<TimeoutRejectedException>().WaitAndRetryAsync(
        5, // 5 attempts
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)), // exponentinal backoff
        onRetry: (outcome, timespan, retryAttemp) =>
        {
            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.GetService<ILogger<CatalogClient>>()?
                .LogWarning($"Delaying for  {timespan} seconds, then making retry {retryAttemp}");
        }))
      .AddTransientHttpErrorPolicy(policy => policy.Or<TimeoutRejectedException>().CircuitBreakerAsync(
              3,
              TimeSpan.FromSeconds(15),
              onBreak: (outcome, timespan) => {
                  var serviceProvider = serviceCollection.BuildServiceProvider();
                  serviceProvider.GetService<ILogger<CatalogClient>>()?
                      .LogWarning($"Opening the Circuit for  {timespan.TotalSeconds} seconds...");
              },
              onReset: () =>
              {
                  var serviceProvider = serviceCollection.BuildServiceProvider();
                  serviceProvider.GetService<ILogger<CatalogClient>>()?
                      .LogWarning($"Closing the Circuit...");
              }
          ))
      .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
}

app.Run();
