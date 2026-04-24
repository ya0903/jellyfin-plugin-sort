using Jellyfin.Data.Enums;
using Jellyfin.Plugin.RatingSort.Configuration;
using Jellyfin.Plugin.RatingSort.Persistence;
using Jellyfin.Plugin.RatingSort.Ratings;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RatingSort.Services;

public sealed class RatingSortService
{
    private readonly ILibraryManager _libraryManager;
    private readonly MdbListClient _mdbListClient;
    private readonly RatingBackupStore _backupStore;
    private readonly ILogger<RatingSortService> _logger;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public RatingSortService(
        ILibraryManager libraryManager,
        MdbListClient mdbListClient,
        RatingBackupStore backupStore,
        ILogger<RatingSortService> logger)
    {
        _libraryManager = libraryManager;
        _mdbListClient = mdbListClient;
        _backupStore = backupStore;
        _logger = logger;
    }

    public IReadOnlyList<LibraryInfo> GetLibraries()
    {
        return _libraryManager.GetVirtualFolders(true)
            .Where(v => string.Equals(v.CollectionType?.ToString(), "movies", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v.CollectionType?.ToString(), "tvshows", StringComparison.OrdinalIgnoreCase))
            .Select(v => new LibraryInfo
            {
                Id = v.ItemId ?? string.Empty,
                Name = v.Name ?? string.Empty,
                CollectionType = v.CollectionType?.ToString() ?? string.Empty
            })
            .Where(v => !string.IsNullOrWhiteSpace(v.Id))
            .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<RatingSortStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        return _backupStore.GetStatusAsync(cancellationToken);
    }

    public async Task<RatingRunResult> RefreshAsync(CancellationToken cancellationToken, IProgress<double>? progress = null)
    {
        var plugin = Plugin.Instance ?? throw new InvalidOperationException("Plugin instance is not available.");
        var config = plugin.Configuration;

        if (string.IsNullOrWhiteSpace(config.MdbListApiKey))
        {
            var missingKey = new RatingRunResult { Message = "MDBList API key is not configured." };
            await SaveCompletedStatusAsync(missingKey, "MDBList API key is not configured.", cancellationToken).ConfigureAwait(false);
            return missingKey;
        }

        await _runLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _backupStore.SaveStatusAsync(new RatingSortStatus
            {
                LastRunStartedUtc = DateTimeOffset.UtcNow,
                LastMessage = "Refresh running."
            }, cancellationToken).ConfigureAwait(false);

            var items = GetTargetItems(config);
            var total = items.Count;
            var result = new RatingRunResult();

            if (total == 0)
            {
                result.Message = "No movie or series items found.";
                progress?.Report(100);
                await SaveCompletedStatusAsync(result, result.Message, cancellationToken).ConfigureAwait(false);
                return result;
            }

            for (var i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = items[i];

                try
                {
                    var outcome = await RefreshItemAsync(item, config, cancellationToken).ConfigureAwait(false);
                    switch (outcome)
                    {
                        case ItemRefreshOutcome.Updated:
                            result.UpdatedCount++;
                            break;
                        case ItemRefreshOutcome.RateLimited:
                            result.RateLimited = true;
                            result.Message = "MDBList rate limit reached; refresh stopped early.";
                            progress?.Report(i * 100.0 / total);
                            await SaveCompletedStatusAsync(result, result.Message, cancellationToken).ConfigureAwait(false);
                            return result;
                        default:
                            result.SkippedCount++;
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    _logger.LogWarning(ex, "Failed to update ratings for {ItemName}", item.Name);
                }

                progress?.Report((i + 1) * 100.0 / total);
            }

            result.Message = $"Refresh completed. Updated {result.UpdatedCount}, skipped {result.SkippedCount}, errors {result.ErrorCount}.";
            progress?.Report(100);
            await SaveCompletedStatusAsync(result, result.Message, cancellationToken).ConfigureAwait(false);
            return result;
        }
        finally
        {
            _runLock.Release();
        }
    }

    public async Task<RestoreResult> RestoreAsync(CancellationToken cancellationToken)
    {
        await _runLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var backups = await _backupStore.GetBackupsAsync(cancellationToken).ConfigureAwait(false);
            var restoredIds = new List<Guid>();
            var errors = 0;

            foreach (var backup in backups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = _libraryManager.GetItemById(backup.ItemId);
                if (item is null)
                {
                    continue;
                }

                try
                {
                    item.CommunityRating = backup.OriginalCommunityRating;
                    item.CriticRating = backup.OriginalCriticRating;
                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                    restoredIds.Add(backup.ItemId);
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Failed to restore original ratings for {ItemName}", item.Name);
                }
            }

            await _backupStore.MarkRestoredAsync(restoredIds, cancellationToken).ConfigureAwait(false);
            var status = await _backupStore.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            status.LastRestoreCompletedUtc = DateTimeOffset.UtcNow;
            status.RestoredCount = restoredIds.Count;
            status.ErrorCount = errors;
            status.LastMessage = $"Restore completed. Restored {restoredIds.Count}, errors {errors}.";
            await _backupStore.SaveStatusAsync(status, cancellationToken).ConfigureAwait(false);

            return new RestoreResult(restoredIds.Count, errors, status.LastMessage);
        }
        finally
        {
            _runLock.Release();
        }
    }

    private IReadOnlyList<BaseItem> GetTargetItems(PluginConfiguration config)
    {
        var selectedIds = config.EnabledLibraryIds
            .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToArray();

        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            Recursive = true,
            IsVirtualItem = false,
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            TopParentIds = selectedIds.Length == 0 ? [] : selectedIds
        });
    }

    private async Task<ItemRefreshOutcome> RefreshItemAsync(BaseItem item, PluginConfiguration config, CancellationToken cancellationToken)
    {
        var contentType = item switch
        {
            Movie => "movie",
            Series => "show",
            _ => null
        };

        if (contentType is null)
        {
            return ItemRefreshOutcome.Skipped;
        }

        var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
        if (string.IsNullOrWhiteSpace(tmdbId))
        {
            return ItemRefreshOutcome.Skipped;
        }

        if (config.RequestDelayMs > 0)
        {
            await Task.Delay(config.RequestDelayMs, cancellationToken).ConfigureAwait(false);
        }

        var lookup = await _mdbListClient.GetByTmdbAsync(contentType, tmdbId, config.MdbListApiKey, cancellationToken).ConfigureAwait(false);
        if (lookup.IsRateLimited)
        {
            return ItemRefreshOutcome.RateLimited;
        }

        if (lookup.Data is null)
        {
            return ItemRefreshOutcome.Skipped;
        }

        var imdb = RatingNormalizer.GetCommunityRating0To10(lookup.Data, "imdb");
        var letterboxd = RatingNormalizer.GetCriticRating0To100(lookup.Data, "letterboxd");

        var newCommunity = imdb;
        var newCritic = letterboxd;
        var changed = !NullableEquals(item.CommunityRating, newCommunity) || !NullableEquals(item.CriticRating, newCritic);

        if (!changed)
        {
            return ItemRefreshOutcome.Skipped;
        }

        await _backupStore.EnsureBackupAsync(item, cancellationToken).ConfigureAwait(false);
        item.CommunityRating = newCommunity;
        item.CriticRating = newCritic;
        await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Updated Rating Sort values for {Name}: IMDb={ImdbRating}, Letterboxd={LetterboxdRating}",
            item.Name,
            newCommunity,
            newCritic);

        return ItemRefreshOutcome.Updated;
    }

    private async Task SaveCompletedStatusAsync(RatingRunResult result, string message, CancellationToken cancellationToken)
    {
        var previous = await _backupStore.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        previous.LastRunCompletedUtc = DateTimeOffset.UtcNow;
        previous.UpdatedCount = result.UpdatedCount;
        previous.SkippedCount = result.SkippedCount;
        previous.ErrorCount = result.ErrorCount;
        previous.LastMessage = message;
        await _backupStore.SaveStatusAsync(previous, cancellationToken).ConfigureAwait(false);
    }

    private static bool NullableEquals(float? left, float? right)
    {
        if (!left.HasValue && !right.HasValue)
        {
            return true;
        }

        return left.HasValue && right.HasValue && Math.Abs(left.Value - right.Value) < 0.01f;
    }

    private enum ItemRefreshOutcome
    {
        Skipped,
        Updated,
        RateLimited
    }
}

public sealed class LibraryInfo
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string CollectionType { get; set; } = string.Empty;
}

public sealed record RestoreResult(int RestoredCount, int ErrorCount, string Message);
