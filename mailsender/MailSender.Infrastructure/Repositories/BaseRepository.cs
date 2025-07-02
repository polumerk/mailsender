using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MailSender.Infrastructure.Context;
using MailSender.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MailSender.Infrastructure.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly ApplicationContext _context;
        public BaseRepository(ApplicationContext context)
        {
            _context = context;
        }

        public Task Update(T entity, CancellationToken cancellationToken)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public Task UpdateRange(IEnumerable<T> entities, CancellationToken cancellationToken)
        {
            _context.Set<T>().UpdateRange(entities);
            return Task.CompletedTask;
        }

        public Task Add(T entity, CancellationToken cancellationToken)
        {
            _context.Set<T>().Add(entity);
            return Task.CompletedTask;
        }
        public Task AddRange(IEnumerable<T> entities, CancellationToken cancellationToken)
        {
            _context.Set<T>().AddRange(entities);
            return Task.CompletedTask;
        }
        public async Task<T[]> Find(Expression<Func<T, bool>> expression, CancellationToken cancellationToken)
        {
            return await _context.Set<T>().Where(expression).ToArrayAsync(cancellationToken);
        }
        public async Task<T[]> GetAll(CancellationToken cancellationToken)
        {
            return await _context.Set<T>().ToArrayAsync(cancellationToken);
        }
        public async Task<T> GetById(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Set<T>().FindAsync(id, cancellationToken);
        }
        public Task Remove(T entity, CancellationToken cancellationToken)
        {
            _context.Set<T>().Remove(entity);
            return Task.CompletedTask;
        }
        public Task RemoveRange(IEnumerable<T> entities, CancellationToken cancellationToken)
        {
            _context.Set<T>().RemoveRange(entities);
            return Task.CompletedTask;
        }
    }
}