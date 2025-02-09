
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NBAJam.Data;

namespace NBAJam.Models
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private DbSet<T> _dbSet { get; set; }
        protected ApplicationDbContext _context { get; set; }

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            T entity = await _dbSet.FindAsync(id);
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(QueryOptions<T> options)
        {
            IQueryable<T> query = _dbSet;
            if (options.HasWhere)
            {
                query = query.Where(options.Where);
            }
            if (options.HasOrderBy)
            {
                query = query.OrderBy(options.OrderBy);
            }
            foreach (string include in options.GetIncludes())
            {
                query = query.Include(include);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public async Task<T> GetByIdAsync(int id, QueryOptions<T> options)
        {
            IQueryable<T> query = _dbSet;
            if (options.HasWhere)
            {
                query = query.Where(options.Where);
            }
            if (options.HasOrderBy)
            {
                query = query.OrderBy(options.OrderBy);
            }
            foreach (string include in options.GetIncludes())
            {
                query = query.Include(include);
            }
            var key = _context.Model.FindEntityType(typeof(T)).FindPrimaryKey().Properties.FirstOrDefault();
            string primaryKeyName = key?.Name;
            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, primaryKeyName) == id);
        }

        public async Task UpdateAsync(T entity)
        {
            _context.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> GetAllByIdAsync<Tkey>(Tkey id, string propertyName, QueryOptions<T> options)
        {
            IQueryable<T> query = _dbSet;
            if (options.HasWhere)
            {
                query = query.Where(options.Where);
            }
            if (options.HasOrderBy)
            {
                query = query.OrderBy(options.OrderBy);
            }
            foreach (string include in options.GetIncludes())
            {
                query = query.Include(include);
            }
            query = query.Where(e => EF.Property<Tkey>(e, propertyName).Equals(id));

            return await query.ToListAsync();
        }
    }
}
