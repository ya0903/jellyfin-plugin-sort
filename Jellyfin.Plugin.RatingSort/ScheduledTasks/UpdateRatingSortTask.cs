using Jellyfin.Plugin.RatingSort.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.RatingSort.ScheduledTasks;

/// <summary>
/// Scheduled task to refresh IMDb and Letterboxd sortable ratings.
/// </summary>
public sealed class UpdateRatingSortTask : IScheduledTask
{
    private readonly RatingSortService _ratingSortService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRatingSortTask"/> class.
    /// </summary>
    /// <param name="ratingSortService">Rating sort service.</param>
    public UpdateRatingSortTask(RatingSortService ratingSortService)
    {
        _ratingSortService = ratingSortService;
    }

    /// <inheritdoc />
    public string Name => "Update IMDb and Letterboxd sort ratings";

    /// <inheritdoc />
    public string Key => "RatingSortUpdate";

    /// <inheritdoc />
    public string Description => "Fetches MDBList ratings and writes IMDb/Letterboxd values into Jellyfin sortable rating fields.";

    /// <inheritdoc />
    public string Category => "Library";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        return _ratingSortService.RefreshAsync(cancellationToken, progress);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        var hours = Plugin.Instance?.Configuration.RefreshIntervalHours ?? 24;
        if (hours < 1)
        {
            hours = 24;
        }

        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromHours(hours).Ticks
            }
        ];
    }
}
