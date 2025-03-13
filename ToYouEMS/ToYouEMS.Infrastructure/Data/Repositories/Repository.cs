using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using ToYouEMS.ToYouEMS.Core.Interfaces;

namespace ToYouEMS.ToYouEMS.Infrastructure.Data.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DbContext Context;
        protected readonly DbSet<TEntity> DbSet;

        public Repository(DbContext context)
        {
            Context = context;
            DbSet = context.Set<TEntity>();
        }

        public async Task<TEntity> GetByIdAsync(int id)
        {
            return await DbSet.FindAsync(id);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return DbSet.Where(predicate);
        }

        public async Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet.SingleOrDefaultAsync(predicate);
        }

        public async Task AddAsync(TEntity entity)
        {
            await DbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await DbSet.AddRangeAsync(entities);
        }

        public void Update(TEntity entity)
        {
            DbSet.Update(entity);
        }

        public void UpdateRange(IEnumerable<TEntity> entities)
        {
            DbSet.UpdateRange(entities);
        }

        public void Remove(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            DbSet.RemoveRange(entities);
        }
    }
}
