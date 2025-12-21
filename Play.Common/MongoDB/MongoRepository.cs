using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Play.Common.Entities;
using Serilog;
using SharpCompress.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;


namespace Play.Common.Repositories
{
    public class MongoRepository<T> : IRepository<T> where T : IEntity
    {
        private readonly IMongoCollection<T> dbCollection;
        private readonly ILogger _logger;
        private readonly FilterDefinitionBuilder<T> filterBuilder = Builders<T>.Filter;

        public MongoRepository(IMongoDatabase database, string collectionName) {
       
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs/mongo_repository.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog();

            _logger = loggerFactory.CreateLogger(typeof(T).Name);

            dbCollection = database.GetCollection<T>(collectionName);
        }

        public async Task<IReadOnlyCollection<T>> GetAllAsync()
        {
            try
            {
                return await dbCollection.Find(filterBuilder.Empty).ToListAsync();
            }
            catch (MongoServerException ex) {
                _logger.LogError(ex.ToJson());
                throw;
            }
        }

        public async Task<T> GetAsync(Guid id)
        {
            try
            {
                var filter = Builders<T>.Filter.Eq("_id", id);
                return await dbCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (MongoServerException ex)
            {
                _logger.LogError(ex.ToJson());
                throw;
            }
        }

        public async Task CreateAsync(T entity) {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            try
            {
                await dbCollection.InsertOneAsync(entity);
            }
            catch (MongoServerException ex)
            {
                _logger.LogError(ex.ToJson());
                throw;
            }
        }

        public async Task UpdateAsync(T entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                FilterDefinition<T> filter = filterBuilder.Eq(entity => entity.Id, entity.Id);
                await dbCollection.ReplaceOneAsync(filter, entity);
            }
            catch (MongoServerException ex)
            {
                _logger.LogError(ex.ToJson());
                throw;
            }
        }

        public async Task RemoveAsync(Guid id)
        {
            try
            {
                FilterDefinition<T> filter = filterBuilder.Eq(entity => entity.Id, id);
                await dbCollection.DeleteOneAsync(filter);
            }
            catch (MongoServerException ex)
            {
                _logger.LogError(ex.ToJson());
                throw;
            }
        }

        public async Task<IReadOnlyCollection<T>> GetAllAsync(Expression<Func<T, bool>> filter)
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            try
            {
                return await dbCollection.Find(filter).ToListAsync();
            }
            catch (MongoServerException ex)
            {
                _logger.LogError(ex.ToJson());
                throw;
            }
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>> filter)
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            try
            {
                return await dbCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (MongoServerException ex)
            {
                _logger.LogError(ex.ToJson());
                throw;
            }
        }

        public async Task<IReadOnlyCollection<T>> GetAllAsync(FilterDefinition<T> filter)
        {
            return await dbCollection.Find<T>(filter).ToListAsync();
        }

    }
}
