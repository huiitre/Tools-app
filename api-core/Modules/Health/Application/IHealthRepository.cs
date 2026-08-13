public interface IHealthRepository
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
