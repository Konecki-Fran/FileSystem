using F24.Services;
using Microsoft.AspNetCore.Mvc;

namespace F24.Controllers;

[ApiController]
[Route("files")]
public sealed class FilesController(FileSystemService service) : ControllerBase
{
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteFileAsync(id, cancellationToken);
        return NoContent();
    }
}