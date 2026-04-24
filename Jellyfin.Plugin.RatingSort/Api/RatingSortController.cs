using System.Net.Mime;
using Jellyfin.Plugin.RatingSort.Persistence;
using Jellyfin.Plugin.RatingSort.Services;
using Jellyfin.Plugin.RatingSort.Web;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.RatingSort.Api;

/// <summary>
/// Rating Sort plugin API.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("RatingSort")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class RatingSortController : ControllerBase
{
    private readonly RatingSortService _ratingSortService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RatingSortController"/> class.
    /// </summary>
    /// <param name="ratingSortService">Rating sort service.</param>
    public RatingSortController(RatingSortService ratingSortService)
    {
        _ratingSortService = ratingSortService;
    }

    /// <summary>
    /// Refreshes ratings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refresh result.</returns>
    [HttpPost("Refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<RatingRunResult>> Refresh(CancellationToken cancellationToken)
    {
        return Ok(await _ratingSortService.RefreshAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Restores original rating values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Restore result.</returns>
    [HttpPost("Restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<RestoreResult>> Restore(CancellationToken cancellationToken)
    {
        return Ok(await _ratingSortService.RestoreAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Gets plugin status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status.</returns>
    [HttpGet("Status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<RatingSortStatus>> Status(CancellationToken cancellationToken)
    {
        return Ok(await _ratingSortService.GetStatusAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Gets movie/show libraries.
    /// </summary>
    /// <returns>Libraries.</returns>
    [HttpGet("Libraries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<LibraryInfo>> Libraries()
    {
        return Ok(_ratingSortService.GetLibraries());
    }

    /// <summary>
    /// Gets the fallback manual web script.
    /// </summary>
    /// <returns>JavaScript.</returns>
    [HttpGet("WebScript")]
    [Produces("application/javascript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ContentResult WebScript()
    {
        return Content(WebScriptBuilder.Build(), "application/javascript");
    }
}
