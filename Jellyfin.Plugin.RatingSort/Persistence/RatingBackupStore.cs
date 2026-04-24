using System.Text.Json;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RatingSort.Persistence;

public sealed class RatingBackupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<RatingBackupStore> _logger;

    public RatingBackupStore(ILogger<RatingBackupStore> logger)
    {
        _logger = logger;
    }

    internal async Task EnsureBackupAsync(BaseItem item, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadBackupStateCoreAsync(cancellationToken).ConfigureAwait(false);
            if (state.Items.Any(e => e.ItemId == item.Id))
            {
                return;
            }

            state.Items.Add(new RatingBackupEntry
            {
                ItemId = item.Id,
                Name = item.Name,
                ItemType = item.GetBaseItemKind().ToString(),
                ImdbId = item.GetProviderId(MetadataProvider.Imdb),
                TmdbId = item.GetProviderId(MetadataProvider.Tmdb),
                OriginalCommunityRating = item.CommunityRating,
                OriginalCriticRating = item.CriticRating,
                BackedUpAtUtc = DateTimeOffset.UtcNow
            });

            await WriteBackupStateCoreAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    internal async Task<IReadOnlyList<RatingBackupEntry>> GetBackupsAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return (await ReadBackupStateCoreAsync(cancellationToken).ConfigureAwait(false)).Items;
        }
        finally
        {
            _lock.Release();
        }
    }

    internal async Task MarkRestoredAsync(IEnumerable<Guid> itemIds, CancellationToken cancellationToken)
    {
        var ids = itemIds.ToHashSet();
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadBackupStateCoreAsync(cancellationToken).ConfigureAwait(false);
            foreach (var entry in state.Items.Where(e => ids.Contains(e.ItemId)))
            {
                entry.RestoredAtUtc = DateTimeOffset.UtcNow;
            }

            await WriteBackupStateCoreAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    internal async Task<RatingSortStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadJsonAsync<RatingSortStatus>(StatusPath, cancellationToken).ConfigureAwait(false) ?? new RatingSortStatus();
        }
        finally
        {
            _lock.Release();
        }
    }

    internal async Task SaveStatusAsync(RatingSortStatus status, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteJsonAsync(StatusPath, status, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string DataFolder
    {
        get
        {
            var plugin = Plugin.Instance ?? throw new InvalidOperationException("Plugin instance is not available.");
            Directory.CreateDirectory(plugin.DataFolderPath);
            return plugin.DataFolderPath;
        }
    }

    private static string BackupPath => Path.Combine(DataFolder, "rating-sort-backups.json");

    private static string StatusPath => Path.Combine(DataFolder, "rating-sort-status.json");

    private async Task<RatingBackupState> ReadBackupStateCoreAsync(CancellationToken cancellationToken)
    {
        return await ReadJsonAsync<RatingBackupState>(BackupPath, cancellationToken).ConfigureAwait(false) ?? new RatingBackupState();
    }

    private async Task WriteBackupStateCoreAsync(RatingBackupState state, CancellationToken cancellationToken)
    {
        await WriteJsonAsync(BackupPath, state, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Rating Sort state file {Path}", path);
            return default;
        }
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, path, true);
    }
}
