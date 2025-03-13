using System.Linq.Expressions;

namespace ToYouEMS.ToYouEMS.Core.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        // 查询方法
        Task<TEntity> GetByIdAsync(int id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

        // 添加方法
        Task AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);

        // 更新方法
        void Update(TEntity entity);
        void UpdateRange(IEnumerable<TEntity> entities);

        // 删除方法
        void Remove(TEntity entity);
        void RemoveRange(IEnumerable<TEntity> entities);
    }
}
