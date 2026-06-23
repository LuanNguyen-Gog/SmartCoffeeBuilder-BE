using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;

namespace SmartCoffeeBuilder.Repository.Utils;

/// <summary>
/// Repository tổng quát cho mọi entity. Không tự gọi SaveChanges —
/// việc lưu thay đổi do <see cref="SmartCoffeeBuilder.Repository.Interfaces.IUnitOfWork{TContext}"/> đảm nhiệm.
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly SmartCafeBuilderContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(SmartCafeBuilderContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public void Dispose() => GC.SuppressFinalize(this);

    #region Query

    public virtual async Task<T?> SingleOrDefaultAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
    {
        IQueryable<T> query = _dbSet;
        if (include != null) query = include(query);
        if (predicate != null) query = query.Where(predicate);
        if (orderBy != null) query = orderBy(query);

        return await query.AsNoTracking().FirstOrDefaultAsync();
    }

    public virtual async Task<TResult?> SingleOrDefaultAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
    {
        IQueryable<T> query = _dbSet;
        if (include != null) query = include(query);
        if (predicate != null) query = query.Where(predicate);
        if (orderBy != null) query = orderBy(query);

        return await query.AsNoTracking().Select(selector).FirstOrDefaultAsync();
    }

    public virtual async Task<ICollection<T>> GetListAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
    {
        IQueryable<T> query = _dbSet;
        if (include != null) query = include(query);
        if (predicate != null) query = query.Where(predicate);
        if (orderBy != null) query = orderBy(query);

        return await query.AsNoTracking().ToListAsync();
    }

    public virtual async Task<ICollection<TResult>> GetListAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
    {
        IQueryable<T> query = _dbSet;
        if (include != null) query = include(query);
        if (predicate != null) query = query.Where(predicate);
        if (orderBy != null) query = orderBy(query);

        return await query.AsNoTracking().Select(selector).ToListAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        => predicate != null
            ? await _dbSet.CountAsync(predicate)
            : await _dbSet.CountAsync();

    public IQueryable<T> GetQueryable(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
    {
        IQueryable<T> query = _dbSet.AsQueryable();
        if (include != null) query = include(query);
        if (predicate != null) query = query.Where(predicate);
        return query;
    }

    public virtual async Task<T?> GetByIdAsync(params object[] keyValues)
        => await _dbSet.FindAsync(keyValues);

    #endregion

    #region Mutation

    public async Task InsertAsync(T entity)
    {
        if (entity == null) return;
        await _dbSet.AddAsync(entity);
    }

    public async Task InsertRangeAsync(IEnumerable<T> entities)
        => await _dbSet.AddRangeAsync(entities);

    public void Update(T entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public void UpdateRange(IEnumerable<T> entities) => _dbSet.UpdateRange(entities);

    public void Delete(T entity) => _dbSet.Remove(entity);

    public void DeleteRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

    #endregion
}
