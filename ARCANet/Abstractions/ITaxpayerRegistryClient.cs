using ARCANet.Taxpayers;

namespace ARCANet.Abstractions;

public interface ITaxpayerRegistryClient
{
    Task<TaxpayerProfile?> GetTaxpayerAsync(long taxpayerCuit, CancellationToken cancellationToken = default);
}
