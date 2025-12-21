using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Play.Common.Entities;
using Play.Common.Settings;
using System;

namespace Play.Common.Repositories
{
    public static class Extensions
    {
        public static IServiceCollection AddMongoDb(this IServiceCollection serviceCollection)
        {
            BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));
           
            serviceCollection.AddSingleton<MongoClient>(sp =>
            {
                var dbsettings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                return new MongoClient(dbsettings.ConnectionString);
            });

            serviceCollection.AddSingleton<IMongoDatabase>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<ServiceSettings>>().Value;
                var client = sp.GetRequiredService<MongoClient>();
                return client.GetDatabase(settings?.ServiceName);
            });

            return serviceCollection;
        }
        public static IServiceCollection AddMongoRepository<T>(this IServiceCollection serviceCollection, string collectionName, string databaseName = null) where T : IEntity
        {
            serviceCollection.AddSingleton<IRepository<T>>(sp =>
            {
                var client = sp.GetRequiredService<MongoClient>();

                // If a database name is provided, use it.
                // Otherwise fall back to the default IMongoDatabase.
                var database = databaseName != null
                    ? client.GetDatabase(databaseName)
                    : sp.GetRequiredService<IMongoDatabase>();

                Console.WriteLine($"[Repository Init] {typeof(T).Name} → DB: " +
                    $"  {database.DatabaseNamespace.DatabaseName}, Collection: {collectionName}");

                return new MongoRepository<T>(database, collectionName);
            });

            return serviceCollection;
        }


    }
}
