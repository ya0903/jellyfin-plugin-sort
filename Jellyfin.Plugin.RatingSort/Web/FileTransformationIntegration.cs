using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RatingSort.Web;

internal static class FileTransformationIntegration
{
    private const string FileTransformationAssemblyName = "Jellyfin.Plugin.FileTransformation";
    private const string FileTransformationPluginTypeName = "Jellyfin.Plugin.FileTransformation.FileTransformationPlugin";
    private const string WriteServiceTypeName = "Jellyfin.Plugin.FileTransformation.Library.IWebFileTransformationWriteService";
    private const string TransformDelegateTypeName = "Jellyfin.Plugin.FileTransformation.Library.TransformFile";

    public static bool TryRegisterIndexHtmlTransformation(Guid transformationId, IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            var assembly = FindFileTransformationAssembly();
            var writeServiceType = assembly?.GetType(WriteServiceTypeName, false, false) ?? GetTypeFromLoadedAssemblies(WriteServiceTypeName);
            var delegateType = assembly?.GetType(TransformDelegateTypeName, false, false) ?? GetTypeFromLoadedAssemblies(TransformDelegateTypeName);

            if (writeServiceType is null || delegateType is null)
            {
                logger.LogInformation("Rating Sort: File Transformation plugin not found; web label injection will use the manual script fallback.");
                return false;
            }

            var writeService = serviceProvider.GetService(writeServiceType);
            if (writeService is null)
            {
                logger.LogWarning("Rating Sort: File Transformation service type was found but not available from DI.");
                return false;
            }

            var method = typeof(WebUiStreamTransformer).GetMethod(nameof(WebUiStreamTransformer.TransformIndexHtmlStream), BindingFlags.Public | BindingFlags.Static);
            if (method is null)
            {
                return false;
            }

            var transformDelegate = Delegate.CreateDelegate(delegateType, method);
            var update = writeServiceType.GetMethod("UpdateTransformation", BindingFlags.Public | BindingFlags.Instance);
            if (update is not null)
            {
                update.Invoke(writeService, [transformationId, "index.html", transformDelegate]);
            }
            else
            {
                writeServiceType.GetMethod("AddTransformation", BindingFlags.Public | BindingFlags.Instance)
                    ?.Invoke(writeService, [transformationId, "index.html", transformDelegate]);
            }

            logger.LogInformation("Rating Sort: registered web label injection through File Transformation.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rating Sort: failed to register File Transformation web label injection.");
            return false;
        }
    }

    public static void TryUnregisterIndexHtmlTransformation(Guid transformationId, IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            var writeServiceType = GetTypeFromLoadedAssemblies(WriteServiceTypeName);
            var writeService = writeServiceType is null ? null : serviceProvider.GetService(writeServiceType);
            writeServiceType?.GetMethod("RemoveTransformation", BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(writeService, [transformationId]);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Rating Sort: failed to unregister File Transformation hook.");
        }
    }

    private static Type? GetTypeFromLoadedAssemblies(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => string.Equals(a.GetName().Name, FileTransformationAssemblyName, StringComparison.OrdinalIgnoreCase))
            .Select(a => a.GetType(fullName, false, false))
            .FirstOrDefault(t => t is not null);
    }

    private static Assembly? FindFileTransformationAssembly()
    {
        foreach (var context in AssemblyLoadContext.All)
        {
            foreach (var assembly in context.Assemblies)
            {
                if (!string.Equals(assembly.GetName().Name, FileTransformationAssemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var pluginType = assembly.GetType(FileTransformationPluginTypeName, false, false);
                var instance = pluginType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (instance is not null)
                {
                    return assembly;
                }
            }
        }

        return AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, FileTransformationAssemblyName, StringComparison.OrdinalIgnoreCase));
    }
}
