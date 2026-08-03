using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;

namespace SmartCoffeeBuilder.Repository.Implementations;

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

    /// <summary>
    /// Đánh dấu entity cần UPDATE. Mọi truy vấn đọc ở trên đều <c>AsNoTracking</c> nên entity
    /// truyền vào thường là detached — hai truy vấn khác nhau trong cùng request sẽ cho hai
    /// instance khác nhau của cùng một dòng. Vì vậy:
    /// - KHÔNG dùng <c>Attach</c> (Attach duyệt cả graph navigation đã nạp, kéo theo cả các
    ///   entity con và ném "another instance with the same key value is already being tracked");
    /// - Nếu change tracker đã giữ một instance khác cùng khoá thì chép giá trị sang instance đó.
    /// </summary>
    public void Update(T entity)
    {
        if (entity == null) return;

        var tracked = FindTrackedByKey(entity);
        if (tracked != null)
        {
            if (!ReferenceEquals(tracked.Entity, entity))
                tracked.CurrentValues.SetValues(entity);

            // Entity đang chờ INSERT thì giữ nguyên Added — ép Modified sẽ mất lệnh insert.
            if (tracked.State != EntityState.Added)
                tracked.State = EntityState.Modified;
            return;
        }

        _context.Entry(entity).State = EntityState.Modified;
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities) Update(entity);
    }

    /// <summary>
    /// Tìm entry đang được track có cùng khoá chính với <paramref name="entity"/>
    /// (kể cả khi là một instance khác). Trả null nếu chưa track hoặc khoá chưa có giá trị.
    /// </summary>
    private EntityEntry<T>? FindTrackedByKey(T entity)
    {
        var primaryKey = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey();
        if (primaryKey == null) return null;

        var properties = primaryKey.Properties;
        var keyValues = new object?[properties.Count];
        for (var i = 0; i < properties.Count; i++)
        {
            var propertyInfo = properties[i].PropertyInfo;
            if (propertyInfo == null) return null;

            keyValues[i] = propertyInfo.GetValue(entity);
            if (keyValues[i] == null) return null;
        }

        return _context.ChangeTracker.Entries<T>().FirstOrDefault(entry =>
        {
            for (var i = 0; i < properties.Count; i++)
            {
                if (!Equals(properties[i].PropertyInfo!.GetValue(entry.Entity), keyValues[i]))
                    return false;
            }
            return true;
        });
    }

    public void Delete(T entity) => _dbSet.Remove(entity);

    public void DeleteRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

    #endregion
}
