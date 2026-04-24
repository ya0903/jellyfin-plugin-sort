using System.Text;

namespace Jellyfin.Plugin.RatingSort.Web;

internal static class WebUiStreamTransformer
{
    public static async Task TransformIndexHtmlStream(string path, Stream contents)
    {
        ArgumentNullException.ThrowIfNull(contents);

        if (!IsIndexHtml(path))
        {
            if (contents.CanSeek)
            {
                contents.Seek(0, SeekOrigin.Begin);
            }

            return;
        }

        string html;
        contents.Seek(0, SeekOrigin.Begin);
        using (var reader = new StreamReader(contents, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
        {
            html = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        var pluginId = Plugin.Instance?.Id ?? Guid.Parse("0ddf0a90-7a30-4e32-b291-9d761c277b1a");
        var transformed = WebScriptBuilder.TransformIndexHtml(html, pluginId);

        if (!contents.CanWrite)
        {
            return;
        }

        contents.Seek(0, SeekOrigin.Begin);
        try
        {
            contents.SetLength(0);
        }
        catch (NotSupportedException)
        {
            // Best effort for streams that cannot be truncated.
        }

        using var writer = new StreamWriter(contents, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteAsync(transformed).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
        contents.Seek(0, SeekOrigin.Begin);
    }

    private static bool IsIndexHtml(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var clean = path.Split('?', '#')[0].Replace('\\', '/');
        var file = clean[(clean.LastIndexOf('/') + 1)..];
        return string.Equals(file, "index.html", StringComparison.OrdinalIgnoreCase);
    }
}
