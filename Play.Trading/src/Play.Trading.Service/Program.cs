using MassTransit;
//using MassTransit.MongoDbIntegration.Saga;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Play.Common.Identity;
using Play.Common.Repositories;
using Play.Common.Settings;
using Play.Identity.Contracts;
using Play.Inventory.Contracts;
using Play.Trading.Service;
using Play.Trading.Service.Contracts;
using Play.Trading.Service.Entities;
using Play.Trading.Service.Exceptions;
using Play.Trading.Service.Repository;
using Play.Trading.Service.Settings;
using Play.Trading.Service.SignalR;
using Play.Trading.Service.StatesMachine;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));

const string AllowedOriginSetting = "AllowedOrigin";

Log.Logger = new LoggerConfiguration()
             .WriteTo.Console()
             //.WriteTo.File("logs/invent_log.txt")
             .MinimumLevel.Information()
             .CreateLogger();

// Add services to the container.

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(nameof(MongoDbSettings)));

builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection(nameof(ServiceSettings)));

builder.Services.Configure<MassTransitSettings>(
    builder.Configuration.GetSection(nameof(MassTransitSettings)));

builder.Services.Configure<QueueSettings>(
    builder.Configuration.GetSection(nameof(QueueSettings)));

builder.Services.AddSingleton<ITradingUserRepository>(sp =>
{
    var client = sp.GetRequiredService<MongoClient>();
    var database = client.GetDatabase("Identity");
    return new TradingUserRepository(database, "Users");
});

builder.Services.AddMongoDb()
                .AddMongoRepository<CatalogItem>("catalogitems")
                .AddMongoRepository<InventoryItem>("inventoryitems")
                .AddMongoRepository<ApplicationUser>("users")
                .AddJwtBearerAuthentication();

AddMassTransit();

builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>()
                .AddSingleton<MessageHub>()
                .AddSignalR();

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

    app.UseCors(config => {
        config.WithOrigins(builder.Configuration[AllowedOriginSetting])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<MessageHub>("/messagehub");
});


void AddMassTransit()
{
    builder.Services.AddMassTransit(configure =>
    {
        configure.AddConsumers(Assembly.GetEntryAssembly()); // all consumers found in the entry assembly are register with mass transit

        configure.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>()
        .InMemoryRepository();
        //.MongoDbRepository(repo => {
        //    var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
        //    var mongoSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();

        //    repo.Connection = mongoSettings.ConnectionString;
        //    repo.DatabaseName = serviceSettings.ServiceName;
        //    repo.CollectionName = "purchaseStates";

        //});

        var queueSettings = builder.Configuration.GetSection(nameof(QueueSettings)).Get<QueueSettings>();

        configure.UsingRabbitMq((context, cfg) =>
        {
            var rabbitSettings = builder.Configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
            
            cfg.UseInMemoryOutbox(context);

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


            cfg.ReceiveEndpoint("purchase-requested-faults", e =>
            {
                e.Handler<Fault<PurchaseRequested>>(context =>
                {
                    var exception = context.Message.Exceptions.FirstOrDefault();
                    Console.WriteLine($"Fault captured: {exception?.Message}");
                    return Task.CompletedTask;
                });

                e.Handler<Fault<GilDebited>>(context =>
                {
                    var exception = context.Message.Exceptions.FirstOrDefault();
                    Console.WriteLine($"Fault captured: {exception?.Message}");
                    return Task.CompletedTask;
                });

                e.Handler<Fault<InventoryItemsGranted>>(context =>
                {
                    var exception = context.Message.Exceptions.FirstOrDefault();
                    Console.WriteLine($"Fault captured: {exception?.Message}");
                    return Task.CompletedTask;
                });

                e.Handler<Fault<InventoryItemsSubtracted>>(context =>
                {
                    var exception = context.Message.Exceptions.FirstOrDefault();
                    Console.WriteLine($"Fault captured: {exception?.Message}");
                    return Task.CompletedTask;
                });

            });

            cfg.ConfigureEndpoints(context);
        });

        EndpointConvention.Map<GrantItems>(new Uri(queueSettings.GrantItemsQueueAddress));
        EndpointConvention.Map<DebitGil>(new Uri(queueSettings.DebitGilQueueAddress));
        EndpointConvention.Map<SubtractItems>(new Uri(queueSettings.SubtractItemsQueueAddress));
    });
} 

app.Run();
