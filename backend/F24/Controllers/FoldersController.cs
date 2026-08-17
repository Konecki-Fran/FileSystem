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
    [ProducesResponseType<FolderContentsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderContentsDto>> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.GetFolderAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}")]
    [ProducesResponseType<EntryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EntryDto>> Create([FromRoute] Guid id, [FromBody] CreateEntryRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteFolderAsync(id, cancellationToken);
        return NoContent();
    }
}
