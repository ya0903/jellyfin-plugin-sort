namespace Jellyfin.Plugin.RatingSort.Web;

internal static class WebScriptBuilder
{
    public static string Build()
    {
        return """
(() => {
  if (window.__ratingSortLabelsInstalled) return;
  window.__ratingSortLabelsInstalled = true;

  const replacements = new Map([
    ['Community Rating', 'IMDb Rating'],
    ['Critic Rating', 'Letterboxd Rating'],
    ['Community rating', 'IMDb rating'],
    ['Critic rating', 'Letterboxd rating']
  ]);

  function rewriteNode(node) {
    if (!node) return;

    if (node.nodeType === Node.TEXT_NODE) {
      const next = replacements.get(node.nodeValue.trim());
      if (next) node.nodeValue = node.nodeValue.replace(node.nodeValue.trim(), next);
      return;
    }

    if (node.nodeType !== Node.ELEMENT_NODE) return;

    for (const attr of ['title', 'aria-label', 'data-title', 'label']) {
      const value = node.getAttribute && node.getAttribute(attr);
      const next = value && replacements.get(value.trim());
      if (next) node.setAttribute(attr, next);
    }

    if (node.tagName === 'OPTION') {
      const next = replacements.get((node.textContent || '').trim());
      if (next) node.textContent = next;
    }

    for (const child of node.childNodes || []) rewriteNode(child);
  }

  function rewrite() {
    rewriteNode(document.body);
  }

  const observer = new MutationObserver((mutations) => {
    for (const mutation of mutations) {
      for (const node of mutation.addedNodes) rewriteNode(node);
      if (mutation.type === 'characterData') rewriteNode(mutation.target);
    }
  });

  function start() {
    rewrite();
    observer.observe(document.body, { childList: true, subtree: true, characterData: true });
  }

  if (document.body) start();
  else document.addEventListener('DOMContentLoaded', start, { once: true });
})();
""";
    }

    public static string TransformIndexHtml(string html, Guid pluginId)
    {
        if (html.Contains("RatingSort/WebScript", StringComparison.OrdinalIgnoreCase))
        {
            return html;
        }

        var tag = $"<script defer src=\"/RatingSort/WebScript?plugin={pluginId:N}\"></script>";
        var bodyIndex = html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return bodyIndex >= 0
            ? html.Insert(bodyIndex, tag)
            : html + tag;
    }
}
