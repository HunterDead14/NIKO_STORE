namespace Application.Common.Interfaces;

public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
