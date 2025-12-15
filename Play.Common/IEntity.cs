using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Play.Common.Entities
{
    public interface IEntity
    {
        [BsonId]
        public Guid Id { get; set; }
    }

}
