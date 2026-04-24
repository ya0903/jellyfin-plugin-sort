using Jellyfin.Plugin.RatingSort.Web;

namespace Jellyfin.Plugin.RatingSort.Tests;

public sealed class WebScriptBuilderTests
{
    [Fact]
    public void TransformIndexHtmlInlinesScript()
    {
        var html = "<html><body><main></main></body></html>";

        var transformed = WebScriptBuilder.TransformIndexHtml(html, Guid.NewGuid());

        Assert.Contains("window.__ratingSortLabelsInstalled", transformed);
        Assert.DoesNotContain("RatingSort/WebScript", transformed);
        Assert.Contains("</script></body>", transformed);
    }

    [Fact]
    public void TransformIndexHtmlDoesNotInjectTwice()
    {
        var html = WebScriptBuilder.TransformIndexHtml("<html><body></body></html>", Guid.NewGuid());

        var transformed = WebScriptBuilder.TransformIndexHtml(html, Guid.NewGuid());

        Assert.Equal(html, transformed);
    }
}
