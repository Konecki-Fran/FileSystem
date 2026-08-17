using F24.Models.Requests;
using F24.Models.Enums;
using F24.Models.DTOs;
using F24.Services;
using Microsoft.AspNetCore.Mvc;

namespace F24.Controllers;

[ApiController]
[Route("search")]
public sealed class SearchController(FileSystemService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SearchResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchRequest request,
        [FromQuery] Guid? folder,
        [FromQuery] SearchMode mode = SearchMode.PrefixAll,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.SearchAsync(request.Prefix, mode, folder, limit, cancellationToken));
    }
}
