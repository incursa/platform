namespace Incursa.Platform.Dns.Internal;

internal static class DnsAsyncEnumerable
{
    public static async Task<List<T>> ToListAsync<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        List<T> items = [];

        await foreach (var item in source.ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Add(item);
        }

        return items;
    }
}
