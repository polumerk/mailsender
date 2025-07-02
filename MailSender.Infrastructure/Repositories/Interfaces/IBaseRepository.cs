using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MailSender.Infrastructure.Repositories.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task Update(T entity, CancellationToken cancellationToken);
        Task UpdateRange(IEnumerable<T> entities, CancellationToken cancellationToken);
        Task Add(T entity, CancellationToken cancellationToken);
        Task AddRange(IEnumerable<T> entities, CancellationToken cancellationToken);
        Task<T[]> Find(Expression<Func<T, bool>> expression, CancellationToken cancellationToken);
        Task<T[]> GetAll(CancellationToken cancellationToken);
        Task<T> GetById(Guid id, CancellationToken cancellationToken);
        Task Remove(T entity, CancellationToken cancellationToken);
        Task RemoveRange(IEnumerable<T> entities, CancellationToken cancellationToken);
    }
}