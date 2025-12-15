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
        public static IServiceCollection AddMongoRepository<T>(this IServiceCollection serviceCollection, string collectionsName) where T: IEntity
        {
          
            serviceCollection.AddSingleton<IRepository<T>>(sp =>
            {
                var settings = sp.GetService<IMongoDatabase>();
                return new MongoRepository<T>(settings, collectionsName);
            });

            return serviceCollection;
        }


    }
}
