using Intranet.Entity;
using Intranet.Service;
using Intranet.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
var mode = builder.Configuration["Gestion:Mode"] ?? "Demo";
if (mode is not ("Demo" or "SqlServer")) throw new InvalidOperationException("Gestion:Mode debe ser Demo o SqlServer.");
if (!builder.Environment.IsDevelopment() && mode == "Demo") throw new InvalidOperationException("El modo Demo solo se permite en Development.");
var demo = builder.Environment.IsDevelopment() && mode == "Demo";
builder.Services.Configure<GestionOptions>(builder.Configuration.GetSection("Gestion"));
builder.Services.Configure<ApplicationLoginOptions>(builder.Configuration.GetSection("ApplicationLogin"));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "Hialpesa.Intranet.Session"; options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax; options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8); options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddScoped<Intranet.Api.Security.PortalAccessFilter>();
builder.Services.AddControllers(options=>options.Filters.AddService<Intranet.Api.Security.PortalAccessFilter>());
builder.Services.AddCors(options => options.AddPolicy("LocalAngular", policy => policy
    .WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ILoggerProvider>(services => new Intranet.Api.Logging.SharedErrorLogProvider(
    builder.Configuration["ErrorLogging:Directory"] ?? Path.Combine(builder.Environment.ContentRootPath, "logs"),
    Path.Combine(builder.Environment.ContentRootPath, "logs"),
    services.GetRequiredService<IHttpContextAccessor>()));
builder.Services.AddScoped<RequestActor>(services =>
{
    var context = services.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("No hay una solicitud activa.");
    return new RequestActor(context.User, context.Request.Headers["X-Intranet-Write"] == "1", context.Request.Headers["X-Intranet-Provider"] == "1", context.Request.Headers["X-Intranet-Agent"] == "1", context.Connection.RemoteIpAddress is { } address && System.Net.IPAddress.IsLoopback(address), context.TraceIdentifier);
});
builder.Services.AddIntranetData();
builder.Services.AddIntranetServices();
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var denied = exception is UnauthorizedAccessException;
    var conflict = exception is RegistrationConflictException;
    var business = exception is ArgumentException || exception is Microsoft.Data.SqlClient.SqlException { Number: 50000 };
    context.Response.StatusCode = denied ? 403 : conflict ? 409 : business ? 400 : 500;
    if (!denied) app.Logger.LogError(exception, "Error en la API. TraceId: {TraceId}", context.TraceIdentifier);
    await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = context.Response.StatusCode, Title = denied ? "Tu cuenta no tiene permiso para esta operación." : conflict || business ? exception!.Message : "No se pudo completar la operación. Revisa la conexión o contacta a soporte.", Extensions = { ["traceId"] = context.TraceIdentifier } });
}));
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
if (app.Environment.IsDevelopment()) app.UseCors("LocalAngular");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", mode, databaseWritesEnabled = builder.Configuration.GetValue<bool>("Gestion:EnableOrderRegistration") })).AllowAnonymous();
app.MapControllers();
app.Run();
public partial class Program { }


