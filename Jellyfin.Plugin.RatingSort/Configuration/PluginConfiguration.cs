using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.RatingSort.Configuration;

/// <summary>
/// Web UI injection mode.
/// </summary>
public enum UiInjectionMode
{
    /// <summary>
    /// Register with File Transformation when available.
    /// </summary>
    Auto,

    /// <summary>
    /// Do not attempt web UI injection.
    /// </summary>
    Disabled,

    /// <summary>
    /// Only expose the script endpoint for manual injector use.
    /// </summary>
    ManualScript
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the MDBList API key.
    /// </summary>
    public string MdbListApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets enabled top-level Jellyfin library ids. Empty means all movie and show libraries.
    /// </summary>
    public string[] EnabledLibraryIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the refresh interval, in hours, used by the scheduled task.
    /// </summary>
    public int RefreshIntervalHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the delay between MDBList requests.
    /// </summary>
    public int RequestDelayMs { get; set; } = 250;

    /// <summary>
    /// Gets or sets the UI injection mode.
    /// </summary>
    public UiInjectionMode UiInjectionMode { get; set; } = UiInjectionMode.Auto;
}
