using Microsoft.EntityFrameworkCore.Storage;
using SmartCoffeeBuilder.Repository.DBContext;

namespace SmartCoffeeBuilder.Repository.Interfaces;

public interface IUnitOfWork<out TContext> : IDisposable where TContext : SmartCafeBuilderContext
{
    TContext Context { get; }

    IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

    // Gói toàn bộ thao tác trong một transaction (tự SaveChanges + commit/rollback)
    Task<TOperation> ProcessInTransactionAsync<TOperation>(Func<Task<TOperation>> operation);
    Task ProcessInTransactionAsync(Func<Task> operation);

    // Quản lý transaction thủ công
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync(IDbContextTransaction transaction);
    Task RollbackTransactionAsync(IDbContextTransaction transaction);

    // Lưu thay đổi
    int Commit();
    Task<int> CommitAsync();
}
