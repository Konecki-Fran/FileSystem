using F24.Models.DTOs;
using F24.Models.Requests;
using F24.Services;
using Microsoft.AspNetCore.Mvc;

namespace F24.Controllers;

[ApiController]
[Route("folders")]
public sealed class FoldersController(FileSystemService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FolderContentsDto>> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.GetFolderAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}")]
    public async Task<ActionResult<EntryDto>> Create([FromRoute] Guid id, [FromBody] CreateEntryRequest request,
        CancellationToken cancellationToken)
    {
        return CreatedAtAction(nameof(Get), new { id }, await service.CreateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteFolderAsync(id, cancellationToken);
        return NoContent();
    }
}