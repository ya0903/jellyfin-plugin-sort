namespace Jellyfin.Plugin.RatingSort.Persistence;

internal sealed class RatingBackupState
{
    public List<RatingBackupEntry> Items { get; set; } = [];
}

internal sealed class RatingBackupEntry
{
    public Guid ItemId { get; set; }

    public string? Name { get; set; }

    public string? ItemType { get; set; }

    public string? ImdbId { get; set; }

    public string? TmdbId { get; set; }

    public float? OriginalCommunityRating { get; set; }

    public float? OriginalCriticRating { get; set; }

    public DateTimeOffset BackedUpAtUtc { get; set; }

    public DateTimeOffset? RestoredAtUtc { get; set; }
}

public sealed class RatingSortStatus
{
    public DateTimeOffset? LastRunStartedUtc { get; set; }

    public DateTimeOffset? LastRunCompletedUtc { get; set; }

    public DateTimeOffset? LastRestoreCompletedUtc { get; set; }

    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public int ErrorCount { get; set; }

    public int RestoredCount { get; set; }

    public string? LastMessage { get; set; }
}

public sealed class RatingRunResult
{
    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public int ErrorCount { get; set; }

    public bool RateLimited { get; set; }

    public string Message { get; set; } = string.Empty;
}
