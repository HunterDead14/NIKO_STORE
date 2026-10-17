using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        return new EfTransactionScope(transaction);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => _context.SaveChangesAsync(ct);

    // Обгортка над IDbContextTransaction, щоб EF-тип не протікав в Application.
    // Якщо scope звільнено без CommitAsync, EF відкочує транзакцію сам.
    private sealed class EfTransactionScope : ITransactionScope
    {
        private readonly IDbContextTransaction _transaction;

        public EfTransactionScope(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken ct)
            => _transaction.CommitAsync(ct);

        public Task RollbackAsync(CancellationToken ct)
            => _transaction.RollbackAsync(ct);

        public ValueTask DisposeAsync()
            => _transaction.DisposeAsync();
    }
}
