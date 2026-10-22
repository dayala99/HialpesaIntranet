using Intranet.Entity;
namespace Intranet.Service;

public abstract class ApplicationService(RequestActor actor)
{
    protected RequestActor Actor { get; } = actor;
    protected System.Security.Claims.ClaimsPrincipal User => Actor.User;
    protected static ServiceResult Ok(object? value = null) => new(ResultKind.Success, value);
    protected static ServiceResult BadRequest(object? value = null) => new(ResultKind.BadRequest, value);
    protected static ServiceResult NotFound(object? value = null) => new(ResultKind.NotFound, value);
    protected static ServiceResult Conflict(object? value = null) => new(ResultKind.Conflict, value);
    protected static ServiceResult NoContent() => new(ResultKind.NoContent);
    protected static ServiceResult Created(string location, object value) => new(ResultKind.Created, value, Location: location);
    protected static ServiceResult File(byte[] content, string contentType, string? fileName = null) => new(ResultKind.Success, Content: content, ContentType: contentType, FileName: fileName);
    protected static ServiceResult StatusCode(int status, object value) => status == 410 ? new(ResultKind.Gone, value) : throw new ArgumentOutOfRangeException(nameof(status));
}
