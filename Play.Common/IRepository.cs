using MongoDB.Driver;
using Play.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Play.Common.Repositories
{
    public interface IRepository<T> where T : IEntity
    {
        Task<T> GetAsync(Guid id);
        Task<IReadOnlyCollection<T>> GetAllAsync();
        Task RemoveAsync(Guid id);
        Task UpdateAsync(T entity);
        Task CreateAsync(T entity);
        Task<IReadOnlyCollection<T>> GetAllAsync(Expression<Func<T, bool>> filter);
        Task<T> GetAsync(Expression<Func<T, bool>> filter);
        Task<IReadOnlyCollection<T>> GetAllAsync(FilterDefinition<T> filter);

    }

}


