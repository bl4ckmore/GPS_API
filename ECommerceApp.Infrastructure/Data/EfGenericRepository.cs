using ECommerceApp.Core.Entities;     
using ECommerceApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;



namespace ECommerceApp.Infrastructure.Data

{

    public sealed class EfGenericRepository<T> : IGenericRepository<T> where T : BaseEntity

    {

        private readonly ApplicationDbContext _db;

        private readonly DbSet<T> _set;



        public EfGenericRepository(ApplicationDbContext db)

        {

            _db = db;

            _set = _db.Set<T>();

        }



        public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)

            => await _set.FindAsync([id], ct);



        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)

            => await _set.AsNoTracking().ToListAsync(ct);



        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)

            => await _set.AsNoTracking().Where(predicate).ToListAsync(ct);



        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)

            => await _set.AsNoTracking().FirstOrDefaultAsync(predicate, ct);



        public async Task<T> AddAsync(T entity, CancellationToken ct = default)

        {

            await _set.AddAsync(entity, ct);

            await _db.SaveChangesAsync(ct);

            return entity;

        }



        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)

        {

            await _set.AddRangeAsync(entities, ct);

            await _db.SaveChangesAsync(ct);

            return entities;

        }



        public async Task UpdateAsync(T entity, CancellationToken ct = default)

        {

            _set.Update(entity);

            await _db.SaveChangesAsync(ct);

        }



        public async Task DeleteAsync(T entity, CancellationToken ct = default)

        {

            _set.Remove(entity);

            await _db.SaveChangesAsync(ct);

        }



        public async Task DeleteAsync(Guid id, CancellationToken ct = default)

        {

            var e = await _set.FindAsync([id], ct);

            if (e is null) return;

            _set.Remove(e);

            await _db.SaveChangesAsync(ct);

        }



        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)

            => await _set.FindAsync([id], ct) is not null;



        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)

            => predicate is null ? await _set.CountAsync(ct) : await _set.CountAsync(predicate, ct);

    }

}