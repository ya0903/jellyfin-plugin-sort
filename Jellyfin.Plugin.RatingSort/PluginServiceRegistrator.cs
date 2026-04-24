using Jellyfin.Plugin.RatingSort.Persistence;
using Jellyfin.Plugin.RatingSort.Ratings;
using Jellyfin.Plugin.RatingSort.Services;
using Jellyfin.Plugin.RatingSort.Web;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.RatingSort;

/// <summary>
/// Registers plugin services.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient();
        serviceCollection.AddSingleton<MdbListClient>();
        serviceCollection.AddSingleton<RatingBackupStore>();
        serviceCollection.AddSingleton<RatingSortService>();
        serviceCollection.AddHostedService<WebUiTransformationHostedService>();
    }
}
