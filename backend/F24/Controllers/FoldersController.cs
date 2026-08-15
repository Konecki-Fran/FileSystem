using F24.Models.DTOs;
using F24.Services;
using Microsoft.AspNetCore.Mvc;

namespace F24.Controllers;

[ApiController]
[Route("folders")]
public sealed class FoldersController(FileSystemService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FolderContentsDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var folder = await service.GetFolderAsync(id, cancellationToken);
        return folder is null ? NotFound() : Ok(folder);
    }
}
