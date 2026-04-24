using Jellyfin.Plugin.RatingSort.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RatingSort.Web;

internal sealed class WebUiTransformationHostedService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebUiTransformationHostedService> _logger;
    private bool _registered;

    public WebUiTransformationHostedService(IServiceProvider serviceProvider, ILogger<WebUiTransformationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;
        if (plugin is null || plugin.Configuration.UiInjectionMode != UiInjectionMode.Auto)
        {
            return Task.CompletedTask;
        }

        _registered = FileTransformationIntegration.TryRegisterIndexHtmlTransformation(plugin.Id, _serviceProvider, _logger);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Unregister();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Unregister();
    }

    private void Unregister()
    {
        if (!_registered || Plugin.Instance is null)
        {
            return;
        }

        FileTransformationIntegration.TryUnregisterIndexHtmlTransformation(Plugin.Instance.Id, _serviceProvider, _logger);
        _registered = false;
    }
}
