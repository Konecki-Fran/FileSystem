using F24.Models.Requests;
using F24.Services;
using Microsoft.AspNetCore.Mvc;

namespace F24.Controllers;

[ApiController]
[Route("search")]
public sealed class SearchController(FileSystemService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] SearchRequest request,
        [FromQuery] Guid? folder,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.SearchAsync(request.Prefix, folder, limit, cancellationToken));
    }
}