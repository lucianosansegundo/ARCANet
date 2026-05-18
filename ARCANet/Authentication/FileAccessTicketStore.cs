using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using ARCANet.Abstractions;

namespace ARCANet.Authentication;

public sealed class FileAccessTicketStore : IAccessTicketStore, IAccessTicketStoreSynchronization
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyGates = new(StringComparer.Ordinal);

    private readonly string _rootDirectory;

    public FileAccessTicketStore(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    public async Task<StoredAccessTicket?> GetAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var path = GetTicketPath(key);
        var gate = GetFileLock(path);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<StoredAccessTicket>(json, JsonOptions);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SaveAsync(
        AccessTicketStoreKey key,
        StoredAccessTicket ticket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(ticket);

        Directory.CreateDirectory(_rootDirectory);

        var path = GetTicketPath(key);
        var gate = GetFileLock(path);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
            var json = JsonSerializer.Serialize(ticket, JsonOptions);

            await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DeleteAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var path = GetTicketPath(key);
        var gate = GetFileLock(path);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<T> ExecuteSerializedAsync<T>(
        AccessTicketStoreKey key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(action);

        var keyMaterial = BuildKeyMaterial(key);
        var gate = KeyGates.GetOrAdd(keyMaterial, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_rootDirectory);
            await using var lockStream = await AcquireKeyLockAsync(keyMaterial, cancellationToken).ConfigureAwait(false);
            return await action(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private string GetTicketPath(AccessTicketStoreKey key)
    {
        var keyMaterial = BuildKeyMaterial(key);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial)));
        return Path.Combine(_rootDirectory, $"{hash}.json");
    }

    private static SemaphoreSlim GetFileLock(string path) =>
        FileLocks.GetOrAdd(path, static _ => new SemaphoreSlim(1, 1));

    private static string BuildKeyMaterial(AccessTicketStoreKey key) =>
        $"{key.Environment}|{key.Service}|{key.RepresentedCuit}|{key.CertificateIdentifier}";

    private string GetLockPath(string keyMaterial)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial)));
        return Path.Combine(_rootDirectory, $"{hash}.lock");
    }

    private async Task<FileStream> AcquireKeyLockAsync(string keyMaterial, CancellationToken cancellationToken)
    {
        var lockPath = GetLockPath(keyMaterial);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(25, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
