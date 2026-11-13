using System.Linq;
using System.Linq.Expressions;
using SmartTask.Domain.Entities;

namespace SmartTask.Infrastructure.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T?> FindAsync(Guid id);
    Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task RemoveAsync(T entity);
    Task RemoveAsync(Guid id);
    Task<int> SaveChangesAsync();
    IQueryable<T> AsQueryable();
}
