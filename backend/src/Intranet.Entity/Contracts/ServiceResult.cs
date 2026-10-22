using System.Security.Claims;
namespace Intranet.Entity;

public enum ResultKind { Success, Created, NoContent, BadRequest, NotFound, Conflict, Gone }
public sealed record ServiceResult(ResultKind Kind, object? Value = null, byte[]? Content = null, string? ContentType = null, string? FileName = null, string? Location = null);
public sealed class BusinessProblem { public string? Title { get; set; } }
public sealed record RequestActor(ClaimsPrincipal User, bool WriteRequested, bool ProviderRequested, bool AgentRequested, bool IsLocal, string TraceIdentifier);
