using System.Linq.Expressions;

namespace NBAJam.Models
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(QueryOptions<T> options);
        Task<T> GetByIdAsync(int id, QueryOptions<T> options);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task<IEnumerable<T>> GetAllByIdAsync<Tkey>(Tkey id, string propertyName, QueryOptions<T> options);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    }
}
