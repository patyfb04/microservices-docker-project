using MassTransit;
using Microsoft.OpenApi;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.MassTransit;
using Play.Common.Settings;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Exceptions;
using Play.Identity.Service.HostedServices;
using Play.Identity.Service.Settings;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/identity_log.txt")
    .MinimumLevel.Information()
    .CreateLogger();

builder.Host.UseSerilog();

// MongoDB Guid serialization (Standard)
BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));

// Settings
const string AllowedOriginsSettings = "AllowedOrigins";
var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
var mongoDBSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
var identityServerSettings = builder.Configuration.GetSection(nameof(IdentityServerSettings)).Get<IdentityServerSettings>();
var identitySettings = builder.Configuration.GetSection(nameof(IdentitySettings));

// Identity + Mongo stores
builder.Services
    .Configure<IdentitySettings>(identitySettings)
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        // Ensure ASP.NET Core Identity uses "role" for role claims
        options.ClaimsIdentity.RoleClaimType = "role";
    })
    .AddRoles<ApplicationRole>()
    .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>(
        mongoDBSettings.ConnectionString,
        serviceSettings.ServiceName
    );


builder.Services.AddMassTransitWithRabbitMq(retryConfigurator =>
{
    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
    retryConfigurator.Ignore(typeof(UnknownUserException));
    retryConfigurator.Ignore(typeof(InsufficientFundsException));

});

// IdentityServer
builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseSuccessEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseErrorEvents = true;
})
    .AddAspNetIdentity<ApplicationUser>() // use built-in profile service
    .AddInMemoryApiScopes(identityServerSettings.ApiScopes)
    .AddInMemoryApiResources(identityServerSettings.ApiResources)
    .AddInMemoryClients(identityServerSettings.Clients)
    .AddInMemoryIdentityResources(identityServerSettings.IdentityResources) // use local variable to guarantee "roles"
    .AddDeveloperSigningCredential();

// Local API auth
builder.Services.AddLocalApiAuthentication();

// MVC + Razor Pages
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// Seed users/roles
builder.Services.AddHostedService<IdentitySeedHostedService>();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Identity.Service", Version = "v1" });
});

// Cookie settings for SPA cross-site redirects
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection(AllowedOriginsSettings)?.Value)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Identity.Service v1"));
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseIdentityServer();

app.MapControllers();
app.MapRazorPages();

app.Run();