using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;

namespace SmartCoffeeBuilder.Repository.Implementations;

public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : SmartCafeBuilderContext
{
    public TContext Context { get; }
    private Dictionary<Type, object>? _repositories;

    public UnitOfWork(TContext context)
    {
        Context = context;
    }

    #region Repository Management
    public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        _repositories ??= new Dictionary<Type, object>();
        if (_repositories.TryGetValue(typeof(TEntity), out var repository))
        {
            return (IGenericRepository<TEntity>)repository;
        }

        repository = new GenericRepository<TEntity>(Context);
        _repositories.Add(typeof(TEntity), repository);
        return (IGenericRepository<TEntity>)repository;
    }
    #endregion

    #region Packed Transaction Management
    public async Task<TOperation> ProcessInTransactionAsync<TOperation>(Func<Task<TOperation>> operation)
    {
        var executionStrategy = Context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await Context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    // Overload cho thao tác không trả về giá trị
    public async Task ProcessInTransactionAsync(Func<Task> operation)
    {
        await ProcessInTransactionAsync(async () =>
        {
            await operation();
            return true;
        });
    }
    #endregion

    #region Transaction Management
    public async Task<IDbContextTransaction> BeginTransactionAsync()
        => await Context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
        => await transaction.CommitAsync();

    public async Task RollbackTransactionAsync(IDbContextTransaction transaction)
        => await transaction.RollbackAsync();
    #endregion

    #region Save Changes
    public int Commit()
    {
        TrackChanges();
        return Context.SaveChanges();
    }

    public async Task<int> CommitAsync()
    {
        TrackChanges();
        return await Context.SaveChangesAsync();
    }
    #endregion

    #region Validation
    private void TrackChanges()
    {
        var validationErrors = Context.ChangeTracker.Entries<IValidatableObject>()
            .SelectMany(e => e.Entity.Validate(new ValidationContext(e.Entity)))
            .Where(r => r != ValidationResult.Success)
            .ToArray();

        if (validationErrors.Length != 0)
        {
            var message = string.Join(Environment.NewLine,
                validationErrors.Select(error =>
                    $"Properties {string.Join(", ", error.MemberNames)} Error: {error.ErrorMessage}"));
            throw new ValidationException(message);
        }
    }
    #endregion

    #region IDisposable Implementation
    public void Dispose()
    {
        Context?.Dispose();
        GC.SuppressFinalize(this);
    }
    #endregion
}
