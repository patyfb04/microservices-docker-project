using MassTransit;
using MassTransit.MongoDbIntegration.Saga;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.Identity;
using Play.Common.Repositories;
using Play.Common.Settings;
using Play.Trading.Service;
using Play.Trading.Service.Entities;
using Play.Trading.Service.Exceptions;
using Play.Trading.Service.StatesMachine;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(nameof(MongoDbSettings)));

builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection(nameof(ServiceSettings)));

builder.Services.Configure<MassTransitSettings>(
    builder.Configuration.GetSection(nameof(MassTransitSettings)));

builder.Services.AddMongoDb()
                .AddMongoRepository<CatalogItem>("catalogitems")
                .AddJwtBearerAuthentication();

AddMassTransit();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Read, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope", "catalog.fullaccess", "inventory.fullaccess");
    });

    options.AddPolicy(Policies.Write, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope", "catalog.fullaccess", "inventory.fullaccess");
    });

});
builder.Services.AddControllers(option =>
{
    option.SuppressAsyncSuffixInActionNames = false;
}).AddJsonOptions(options =>
   {
       options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
   });


//builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Trading.Service", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Trading.Service v1"));
}


app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
void AddMassTransit()
{
    builder.Services.AddMassTransit(configure =>
    {
        configure.AddConsumers(Assembly.GetEntryAssembly()); // all consumers found in the entry assembly are register with mass transit

        configure.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>()
        .MongoDbRepository(repo => {
            var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
            var mongoSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();

            repo.Connection = mongoSettings.ConnectionString;
            repo.DatabaseName = serviceSettings.ServiceName;
            repo.CollectionName = "purchaseStates";

            BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
            BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));

        });


        configure.UsingRabbitMq((context, cfg) =>
        {
            var rabbitSettings = builder.Configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();

            cfg.UseMessageRetry(config =>
            {
                config.Interval(3, TimeSpan.FromSeconds(5));
                config.Ignore(typeof(UnknownItemException));
            });

            cfg.Host(rabbitSettings.Host, h =>
            {
                h.Username(rabbitSettings.Username);
                h.Password(rabbitSettings.Password);
            });

            // This is critical: wires up saga consumers to queues
            cfg.ConfigureEndpoints(context);
        });

    });
} 

app.Run();
