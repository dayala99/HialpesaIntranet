using Intranet.Entity;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api;

public static class ServiceResultExtensions
{
    public static ActionResult ToActionResult(this ServiceResult result)
    {
        if (result.Content is { } bytes) return new FileContentResult(bytes, result.ContentType ?? "application/octet-stream") { FileDownloadName = result.FileName ?? "" };
        object? value = result.Value is BusinessProblem problem ? new ProblemDetails { Title = problem.Title } : result.Value;
        return result.Kind switch
        {
            ResultKind.Success => new OkObjectResult(value),
            ResultKind.Created => new CreatedResult(result.Location, value),
            ResultKind.NoContent => new NoContentResult(),
            ResultKind.BadRequest => value is null ? new BadRequestResult() : new BadRequestObjectResult(value),
            ResultKind.NotFound => value is null ? new NotFoundResult() : new NotFoundObjectResult(value),
            ResultKind.Conflict => new ConflictObjectResult(value),
            ResultKind.Gone => new ObjectResult(value) { StatusCode = 410 },
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };
    }
}
